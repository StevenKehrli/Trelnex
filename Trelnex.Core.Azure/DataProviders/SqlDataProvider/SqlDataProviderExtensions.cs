using System.Configuration;
using Azure.Core;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
using Trelnex.Core.Identity;
using Trelnex.Core.Api.Encryption;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Extension methods for configuring SQL Server data providers with dependency injection.
/// </summary>
public static class SqlDataProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers SQL Server data providers with the service collection using configuration.
    /// </summary>
    /// <param name="services">Service collection to register providers with.</param>
    /// <param name="configuration">Application configuration containing SQL Server settings.</param>
    /// <param name="bootstrapLogger">Logger for recording registration activities.</param>
    /// <param name="configureDataProviders">Delegate to configure which providers to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required configuration sections are missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown when ServiceConfiguration is not registered or duplicate providers are registered.</exception>
    /// <exception cref="ArgumentException">Thrown when a type name has no associated table configuration.</exception>
    public static IServiceCollection AddSqlDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Get Azure credentials from registered credential provider
        var credentialProvider = services.GetCredentialProvider<TokenCredential>();

        // Extract SQL Server configuration from application settings
        var dataSource = configuration.GetSection("Azure.SqlDataProviders:DataSource").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.SqlDataProviders configuration is not valid.");

        var initialCatalog = configuration.GetSection("Azure.SqlDataProviders:InitialCatalog").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.SqlDataProviders configuration is not valid.");

        var tables = configuration.GetSection("Azure.SqlDataProviders:Tables").GetChildren();
        var tableConfigurations = tables
            .Select(section =>
            {
                var itemTableName = section.GetValue<string>("ItemTableName")
                    ?? throw new ConfigurationErrorsException("The Azure.SqlDataProviders configuration is not valid.");

                // Default the event table name to {itemTableName}-events
                // If the EventPolicy is Disabled, we do not use it
                var eventTableName = section.GetValue<string>("EventTableName", $"{itemTableName}-events");

                var eventPolicy = section.GetValue("EventPolicy", EventPolicy.AllChanges);
                var eventTimeToLive = section.GetValue<int?>("EventTimeToLive");

                var blockCipherService = section.CreateBlockCipherService();

                return new TableConfiguration(
                    TypeName: section.Key,
                    ItemTableName: itemTableName,
                    EventTableName: eventTableName,
                    EventPolicy: eventPolicy,
                    EventTimeToLive: eventTimeToLive,
                    BlockCipherService: blockCipherService);
            })
            .ToArray();

        // Get service configuration from DI container
        var serviceDescriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // Build provider options from configuration
        var providerOptions = SqlDataProviderOptions.Parse(
            dataSource: dataSource,
            initialCatalog: initialCatalog,
            tableConfigurations: tableConfigurations);

        // Configure SQL Server client with credentials and connection details
        var sqlClientOptions = GetSqlClientOptions(credentialProvider, providerOptions);

        var providerFactory = SqlDataProviderFactory
            .Create(serviceConfiguration, sqlClientOptions)
            .GetAwaiter()
            .GetResult();

        // Register factory with DI container
        services.AddDataProviderFactory(providerFactory);

        // Configure individual data providers using factory
        var dataProviderOptions = new DataProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory,
            providerOptions: providerOptions);

        configureDataProviders(dataProviderOptions);

        return services;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates SQL Server client options with Azure credentials and authentication scope.
    /// </summary>
    /// <param name="credentialProvider">Provider for Azure credentials.</param>
    /// <param name="providerOptions">SQL Server configuration options.</param>
    /// <returns>Configured SQL Server client options.</returns>
    private static SqlClientOptions GetSqlClientOptions(
        ICredentialProvider<TokenCredential> credentialProvider,
        SqlDataProviderOptions providerOptions)
    {
        // Get Azure credentials and configure authentication
        var tokenCredential = credentialProvider.GetCredential();

        // Standard Azure SQL Database authentication scope
        var scope = "https://database.windows.net/.default";

        var tokenRequestContext = new TokenRequestContext(
            scopes: [scope]);

        // Pre-authenticate to verify credentials
        tokenCredential.GetToken(tokenRequestContext, default);

        return new SqlClientOptions(
            TokenCredential: tokenCredential,
            Scope: scope,
            DataSource: providerOptions.DataSource,
            InitialCatalog: providerOptions.InitialCatalog,
            TableNames: providerOptions.GetTableNames()
        );
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Handles registration of SQL Server data providers with type-to-table mapping.
    /// </summary>
    private class DataProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        SqlDataProviderFactory providerFactory,
        SqlDataProviderOptions providerOptions)
        : IDataProviderOptions
    {
        #region Public Methods

        /// <summary>
        /// Registers a SQL Server data provider for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier that maps to a SQL Server table.</param>
        /// <param name="itemValidator">Optional validator for entity validation.</param>
        /// <param name="commandOperations">Optional CRUD operations to enable.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when no table is configured for the type name.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a provider for this type is already registered.</exception>
        public IDataProviderOptions Add<TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TItem : BaseItem, new()
        {
            // Look up table configuration for the specified type
            var tableConfiguration = providerOptions.GetTableConfiguration(typeName);

            if (tableConfiguration is null)
            {
                throw new ArgumentException(
                    $"The Table for TypeName '{typeName}' is not found.",
                    nameof(typeName));
            }

            if (services.Any(sd => sd.ServiceType == typeof(IDataProvider<TItem>)))
            {
                throw new InvalidOperationException(
                    $"The DataProvider<{typeof(TItem).Name}> is already registered.");
            }

            // Create data provider instance using factory and table configuration
            var dataProvider = providerFactory.Create(
                typeName: typeName,
                itemTableName: tableConfiguration.ItemTableName,
                eventTableName: tableConfiguration.EventTableName,
                itemValidator: itemValidator,
                commandOperations: commandOperations,
                eventPolicy: tableConfiguration.EventPolicy,
                eventTimeToLive: tableConfiguration.EventTimeToLive,
                blockCipherService: tableConfiguration.BlockCipherService,
                logger: bootstrapLogger);

            services.AddSingleton(dataProvider);

            object[] args =
            [
                typeof(TItem), // TItem,
                providerOptions.DataSource, // server
                providerOptions.InitialCatalog, // database,
                tableConfiguration.ItemTableName, // item table
                tableConfiguration.EventTableName, // event table
            ];

            // Log successful provider registration
            bootstrapLogger.LogInformation(
                message: "Added SqlDataProvider<{TItem:l}>: dataSource = '{dataSource:l}', initialCatalog = '{initialCatalog:l}', itemTableName = '{itemTableName:l}', eventTableName = '{eventTableName:l}'.",
                args: args);

            return this;
        }

        #endregion
    }

    #endregion

    #region Configuration Records

    /// <summary>
    /// Configuration mapping a type name to its SQL Server table and settings.
    /// </summary>
    /// <param name="TypeName">The logical type name identifier.</param>
    /// <param name="ItemTableName">The physical SQL Server table name for items.</param>
    /// <param name="EventTableName">The physical SQL Server table name for events.</param>
    /// <param name="EventPolicy">The event policy for the table.</param>
    /// <param name="EventTimeToLive">Optional TTL for events in seconds.</param>
    /// <param name="BlockCipherService">Optional encryption service for the table.</param>
    private record TableConfiguration(
        string TypeName,
        string ItemTableName,
        string EventTableName,
        EventPolicy EventPolicy,
        int? EventTimeToLive,
        IBlockCipherService? BlockCipherService);

    #endregion

    #region Provider Options

    /// <summary>
    /// Configuration options for SQL Server data providers with type-to-table mappings.
    /// </summary>
    /// <param name="dataSource">SQL Server instance name or network address.</param>
    /// <param name="initialCatalog">SQL Server database name.</param>
    private class SqlDataProviderOptions(
        string dataSource,
        string initialCatalog)
    {
        #region Private Fields

        // Dictionary mapping type names to their table configurations
        private readonly Dictionary<string, TableConfiguration> _tableConfigurationsByTypeName = [];

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the SQL Server data source name.
        /// </summary>
        public string DataSource => dataSource;

        /// <summary>
        /// Gets the SQL Server database name.
        /// </summary>
        public string InitialCatalog => initialCatalog;

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Parses configuration arrays into validated provider options.
        /// </summary>
        /// <param name="dataSource">SQL Server data source name.</param>
        /// <param name="initialCatalog">SQL Server database name.</param>
        /// <param name="tableConfigurations">Array of table configurations to validate.</param>
        /// <returns>Validated provider options with type-to-table mappings.</returns>
        /// <exception cref="AggregateException">Thrown when configuration contains duplicate type mappings.</exception>
        public static SqlDataProviderOptions Parse(
            string dataSource,
            string initialCatalog,
            TableConfiguration[] tableConfigurations)
        {
            // Create options instance with connection details
            var providerOptions = new SqlDataProviderOptions(
                dataSource: dataSource,
                initialCatalog: initialCatalog);

            // Group configurations by type name to detect duplicates
            var groups = tableConfigurations
                .GroupBy(tableConfiguration => tableConfiguration.TypeName)
                .ToArray();

            // Validate configuration for duplicate type mappings
            var exs = new List<ConfigurationErrorsException>();

            Array.ForEach(groups, group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A Table for TypeName '{group.Key} is specified more than once."));
            });

            // Throw aggregate exception if validation errors found
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // Build type-to-table mapping dictionary
            Array.ForEach(groups, group =>
            {
                providerOptions._tableConfigurationsByTypeName[group.Key] = group.Single();
            });

            return providerOptions;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves table configuration for a specified type name.
        /// </summary>
        /// <param name="typeName">Type name to look up.</param>
        /// <returns>Table configuration if found, null otherwise.</returns>
        public TableConfiguration? GetTableConfiguration(
            string typeName)
        {
            return _tableConfigurationsByTypeName.TryGetValue(typeName, out var tableConfiguration)
                ? tableConfiguration
                : null;
        }

        /// <summary>
        /// Gets all configured SQL Server table names.
        /// </summary>
        /// <returns>Sorted array of unique table names.</returns>
        public string[] GetTableNames()
        {
            var tableNames = new HashSet<string>();

            foreach (var tableConfiguration in _tableConfigurationsByTypeName.Values)
            {
                tableNames.Add(tableConfiguration.ItemTableName);

                if (tableConfiguration.EventPolicy != EventPolicy.Disabled)
                {
                    tableNames.Add(tableConfiguration.EventTableName);
                }
            }

            return tableNames
                .OrderBy(tableName => tableName)
                .ToArray();
        }

        #endregion
    }

    #endregion
}
