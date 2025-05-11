using System.Configuration;
using Azure.Core;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// Extension methods for configuring and registering SQL Server command providers.
/// </summary>
/// <remarks>Provides dependency injection integration for SQL Server command providers.</remarks>
public static class SqlCommandProvidersExtensions
{
    /// <summary>
    /// Adds SQL Server command providers to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add providers to.</param>
    /// <param name="configuration">Application configuration containing provider settings.</param>
    /// <param name="bootstrapLogger">Logger for recording provider initialization.</param>
    /// <param name="configureCommandProviders">Action to configure specific providers.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">When the SqlCommandProviders section is missing from configuration.</exception>
    /// <exception cref="InvalidOperationException">When the ServiceConfiguration is not registered or when attempting to register the same command provider interface twice.</exception>
    /// <exception cref="ArgumentException">When a requested type name has no associated table.</exception>
    /// <remarks>Registers command providers for specific types.</remarks>
    public static IServiceCollection AddSqlCommandProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<ICommandProviderOptions> configureCommandProviders)
    {
        // Get the token credential provider.
        var credentialProvider = services.GetCredentialProvider<TokenCredential>();

        // Load and validate configuration.
        var providerConfiguration = configuration
            .GetSection("SqlCommandProviders")
            .Get<SqlCommandProviderConfiguration>()
            ?? throw new ConfigurationErrorsException("The SqlCommandProviders configuration is not found.");

        // Get the service configuration.
        var serviceDescriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        // Cast the service descriptor.
        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // Convert the raw configuration into a validated options object.
        var providerOptions = SqlCommandProviderOptions.Parse(providerConfiguration);

        // Set up the SQL client options for AAD authentication.
        var sqlClientOptions = GetSqlClientOptions(credentialProvider, providerOptions);

        // Create and initialize the SQL command provider factory.
        var providerFactory = SqlCommandProviderFactory.Create(
            serviceConfiguration,
            sqlClientOptions);

        // Register the factory in the DI container as an implementation of ICommandProviderFactory.
        services.AddCommandProviderFactory(providerFactory);

        // Create a configuration object that will be used by the consumer to register specific providers.
        var commandProviderOptions = new CommandProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory,
            providerOptions: providerOptions);

        // Execute the consumer-supplied configuration action to register specific entity types.
        configureCommandProviders(commandProviderOptions);

        // Return the service collection to allow for method chaining in the calling code.
        return services;
    }

    /// <summary>
    /// Creates SQL Server client options with properly configured authentication.
    /// </summary>
    /// <param name="credentialProvider">Provider for token-based authentication.</param>
    /// <param name="providerOptions">Configuration options for SQL Server.</param>
    /// <returns>Fully configured SQL client options.</returns>
    /// <remarks>Initializes an access token with proper scope for SQL Server.</remarks>
    private static SqlClientOptions GetSqlClientOptions(
        ICredentialProvider<TokenCredential> credentialProvider,
        SqlCommandProviderOptions providerOptions)
    {
        // Retrieve the token credential from the provider that was registered during application startup.
        var tokenCredential = credentialProvider.GetCredential();

        // Define the standard scope for Azure SQL Database access.
        var scope = "https://database.windows.net/.default";

        // Create a token request context with the SQL Database scope.
        var tokenRequestContext = new TokenRequestContext(
            scopes: [scope]);

        // Request a token now to "warm up" the credential provider and avoid cold start latency.
        tokenCredential.GetToken(tokenRequestContext, default);

        // Construct and return the SQL client options with all necessary connection parameters.
        return new SqlClientOptions(
            TokenCredential: tokenCredential,
            Scope: scope,
            DataSource: providerOptions.DataSource,
            InitialCatalog: providerOptions.InitialCatalog,
            TableNames: providerOptions.GetTableNames()
        );
    }

    /// <summary>
    /// Implementation of <see cref="ICommandProviderOptions"/> for configuring SQL Server providers.
    /// </summary>
    /// <remarks>Provides type-to-table mapping and command provider registration.</remarks>
    private class CommandProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        SqlCommandProviderFactory providerFactory,
        SqlCommandProviderOptions providerOptions)
        : ICommandProviderOptions
    {
        /// <summary>
        /// Registers a command provider for a specific item type with table mapping.
        /// </summary>
        /// <typeparam name="TInterface">Interface type for the items.</typeparam>
        /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
        /// <param name="typeName">Type name to map to a table.</param>
        /// <param name="itemValidator">Optional validator for items.</param>
        /// <param name="commandOperations">Operations allowed for this provider.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="ArgumentException">When no table is configured for the specified type name.</exception>
        /// <exception cref="InvalidOperationException">When a command provider for the interface is already registered.</exception>
        /// <remarks>Maps a logical entity type with its physical storage location.</remarks>
        public ICommandProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // Look up the table name from the configured mappings using the provided type name.
            var tableName = providerOptions.GetTableName(typeName);

            // If no table mapping exists for this type name, we cannot continue.
            if (tableName is null)
            {
                throw new ArgumentException(
                    $"The Table for TypeName '{typeName}' is not found.",
                    nameof(typeName));
            }

            // Check if a command provider for this interface has already been registered.
            if (services.Any(sd => sd.ServiceType == typeof(ICommandProvider<TInterface>)))
            {
                throw new InvalidOperationException(
                    $"The CommandProvider<{typeof(TInterface).Name}> is already registered.");
            }

            // Create a new command provider instance for this entity type via the factory.
            var commandProvider = providerFactory.Create<TInterface, TItem>(
                tableName: tableName,
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations);

            // Register the provider in the DI container as a singleton.
            services.AddSingleton(commandProvider);

            // Prepare logging parameters to record the registration.
            object[] args =
            [
                typeof(TInterface), // TInterface,
                typeof(TItem), // TItem,
                providerOptions.DataSource, // server
                providerOptions.InitialCatalog, // database,
                tableName, // table
            ];

            // Log the successful registration with connection details.
            bootstrapLogger.LogInformation(
                message: "Added SqlCommandProvider<{TInterface:l}, {TItem:l}>: dataSource = '{dataSource:l}', initialCatalog = '{initialCatalog:l}', tableName = '{tableName:l}'.",
                args: args);

            // Return this instance to enable method chaining for fluent configuration.
            return this;
        }
    }

    /// <summary>
    /// Table configuration mapping type names to table names.
    /// </summary>
    /// <param name="TypeName">The type name used for filtering items.</param>
    /// <param name="TableName">The table name in SQL Server.</param>
    /// <remarks>Defines the mapping between logical type names and physical tables.</remarks>
    private record TableConfiguration(
        string TypeName,
        string TableName);

    /// <summary>
    /// Configuration properties for SQL Server command providers.
    /// </summary>
    /// <remarks>Reads from the "SqlCommandProviders" section in application configuration.</remarks>
    private record SqlCommandProviderConfiguration
    {
        /// <summary>
        /// The SQL Server name or network address.
        /// </summary>
        /// <remarks>Used to establish connection to the database server.</remarks>
        public required string DataSource { get; init; }

        /// <summary>
        /// The database name to use.
        /// </summary>
        /// <remarks>All tables must be within this database.</remarks>
        public required string InitialCatalog { get; init; }

        /// <summary>
        /// The collection of table mappings by item type.
        /// </summary>
        /// <remarks>Maps logical type names to physical table names.</remarks>
        public required TableConfiguration[] Tables { get; init; }
    }

    /// <summary>
    /// Runtime options for SQL Server command providers.
    /// </summary>
    /// <param name="dataSource">The data source or server name for the SQL server.</param>
    /// <param name="initialCatalog">The initial catalog or database name for the SQL server.</param>
    /// <remarks>Provides validated, parsed configuration with table-to-type mappings.</remarks>
    private class SqlCommandProviderOptions(
        string dataSource,
        string initialCatalog)
    {
        /// <summary>
        /// The mappings from type names to table names.
        /// </summary>
        private readonly Dictionary<string, string> _tableNamesByTypeName = [];

        /// <summary>
        /// Parses configuration into a validated options object.
        /// </summary>
        /// <param name="providerConfiguration">Raw configuration data.</param>
        /// <returns>Validated options with type-to-table mappings.</returns>
        /// <exception cref="AggregateException">When configuration contains duplicate type mappings.</exception>
        /// <remarks>Validates that no type name is mapped to multiple tables.</remarks>
        public static SqlCommandProviderOptions Parse(
            SqlCommandProviderConfiguration providerConfiguration)
        {
            // Create a new options instance with the connection information from configuration.
            var options = new SqlCommandProviderOptions(
                dataSource: providerConfiguration.DataSource,
                initialCatalog: providerConfiguration.InitialCatalog);

            // Group the table configurations by type name to detect duplicates.
            var groups = providerConfiguration
                .Tables
                .GroupBy(o => o.TypeName)
                .ToArray();

            // Prepare to collect any configuration errors for comprehensive reporting.
            var exs = new List<ConfigurationErrorsException>();

            // Check each group to ensure no type name is mapped to multiple tables.
            Array.ForEach(groups, group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A Table for TypeName '{group.Key} is specified more than once."));
            });

            // If any configuration errors were found, throw them as an aggregate exception.
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // With validation complete, build the type-to-table mapping dictionary.
            Array.ForEach(groups, group =>
            {
                options._tableNamesByTypeName[group.Key] = group.Single().TableName;
            });

            // Return the fully configured and validated options object.
            return options;
        }

        /// <summary>
        /// Gets the SQL Server name or network address.
        /// </summary>
        public string DataSource => dataSource;

        /// <summary>
        /// Gets the database name.
        /// </summary>
        public string InitialCatalog => initialCatalog;

        /// <summary>
        /// Gets the table name for a specified type name.
        /// </summary>
        /// <param name="typeName">The type name to look up.</param>
        /// <returns>The table name if found, or <see langword="null"/> if no mapping exists.</returns>
        public string? GetTableName(
            string typeName)
        {
            // Try to retrieve the table name associated with the given type name.
            return _tableNamesByTypeName.TryGetValue(typeName, out var tableName)
                ? tableName
                : null;
        }

        /// <summary>
        /// Gets all configured table names.
        /// </summary>
        /// <returns>Array of distinct table names sorted alphabetically.</returns>
        public string[] GetTableNames()
        {
            // Extract all the table names from the mapping dictionary.
            return _tableNamesByTypeName
                .Values
                .OrderBy(tn => tn)
                .ToArray();
        }
    }
}
