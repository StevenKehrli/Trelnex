using System.Configuration;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.Runtime;
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

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Extension methods for configuring PostgreSQL data providers with dependency injection.
/// </summary>
public static partial class PostgresDataProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers PostgreSQL data providers with the service collection using configuration.
    /// </summary>
    /// <param name="services">Service collection to register providers with.</param>
    /// <param name="configuration">Application configuration containing PostgreSQL settings.</param>
    /// <param name="bootstrapLogger">Logger for recording registration activities.</param>
    /// <param name="configureDataProviders">Delegate to configure which providers to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required configuration sections are missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown when ServiceConfiguration is not registered or duplicate providers are registered.</exception>
    /// <exception cref="ArgumentException">Thrown when a type name has no associated table configuration.</exception>
    public static IServiceCollection AddPostgresDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Get AWS credentials from registered credential provider
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();

        // Extract PostgreSQL configuration from application settings
        var host = configuration.GetSection("Amazon.PostgresDataProviders:Host").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not valid.");

        var port = configuration.GetSection("Amazon.PostgresDataProviders:Port").Get<int?>()
            ?? 5432;

        var database = configuration.GetSection("Amazon.PostgresDataProviders:Database").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not valid.");

        var dbUser = configuration.GetSection("Amazon.PostgresDataProviders:DbUser").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not valid.");

        var tables = configuration.GetSection("Amazon.PostgresDataProviders:Tables").GetChildren();
        var tableConfigurations = tables
            .Select(section =>
            {
                var itemTableName = section.GetValue<string>("ItemTableName")
                    ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not valid.");

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
        var providerOptions = PostgresDataProviderOptions.Parse(
            host: host,
            port: port,
            database: database,
            dbUser: dbUser,
            tableConfigurations: tableConfigurations);

        // Configure PostgreSQL client with credentials and connection details
        var postgresClientOptions = GetPostgresClientOptions(credentialProvider, providerOptions);

        var providerFactory = PostgresDataProviderFactory
            .Create(serviceConfiguration, postgresClientOptions)
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
    /// Creates PostgreSQL client options with AWS credentials and connection settings.
    /// </summary>
    /// <param name="credentialProvider">Provider for AWS credentials.</param>
    /// <param name="providerOptions">PostgreSQL configuration options.</param>
    /// <returns>Configured PostgreSQL client options.</returns>
    private static PostgresClientOptions GetPostgresClientOptions(
        ICredentialProvider<AWSCredentials> credentialProvider,
        PostgresDataProviderOptions providerOptions)
    {
        // Get AWS credentials and build client configuration
        var awsCredentials = credentialProvider.GetCredential();

        return new PostgresClientOptions(
            AWSCredentials: awsCredentials,
            Region: providerOptions.Region,
            Host: providerOptions.Host,
            Port: providerOptions.Port,
            Database: providerOptions.Database,
            DbUser: providerOptions.DbUser,
            TableNames: providerOptions.GetTableNames()
        );
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Handles registration of PostgreSQL data providers with type-to-table mapping.
    /// </summary>
    private class DataProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        PostgresDataProviderFactory providerFactory,
        PostgresDataProviderOptions providerOptions)
        : IDataProviderOptions
    {
        /// <summary>
        /// Registers a PostgreSQL data provider for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier that maps to a PostgreSQL table.</param>
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
                blockCipherService: tableConfiguration.BlockCipherService);

            services.AddSingleton(dataProvider);

            object[] args =
            [
                typeof(TItem), // TItem,
                providerOptions.Region, // region
                providerOptions.Host, // host
                providerOptions.Port, // port
                providerOptions.Database, // database
                providerOptions.DbUser, // dbUser
                tableConfiguration.ItemTableName, // item table
                tableConfiguration.EventTableName, // event table
            ];

            // Log successful provider registration
            bootstrapLogger.LogInformation(
                message: "Added PostgresDataProvider<{TItem:l}>: region = '{region:l}', host = '{host:l}', port = '{port:l}', database = '{database:l}', dbUser = '{dbUser:l}', itemTableName = '{itemTableName:l}', eventTableName = '{eventTableName:l}'.",
                args: args);

            return this;
        }
    }

    #endregion

    #region Configuration Records

    /// <summary>
    /// Configuration mapping a type name to its PostgreSQL table and settings.
    /// </summary>
    /// <param name="TypeName">The logical type name identifier.</param>
    /// <param name="ItemTableName">The physical PostgreSQL table name for items.</param>
    /// <param name="EventTableName">The physical PostgreSQL table name for events.</param>
    /// <param name="EventPolicy">The event policy for change tracking.</param>
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
    /// Configuration options for PostgreSQL data providers with type-to-table mappings.
    /// </summary>
    private partial class PostgresDataProviderOptions(
        RegionEndpoint region,
        string host,
        int port,
        string database,
        string dbUser)
    {
        #region Public Properties

        /// <summary>
        /// Gets the PostgreSQL database name.
        /// </summary>
        public string Database => database;

        /// <summary>
        /// Gets the PostgreSQL database username.
        /// </summary>
        public string DbUser => dbUser;

        /// <summary>
        /// Gets the PostgreSQL server hostname.
        /// </summary>
        public string Host => host;

        /// <summary>
        /// Gets the PostgreSQL server port number.
        /// </summary>
        public int Port => port;

        /// <summary>
        /// Gets the AWS region for the PostgreSQL server.
        /// </summary>
        public RegionEndpoint Region => region;

        #endregion

        #region Private Fields

        // Dictionary mapping type names to their table configurations
        private readonly Dictionary<string, TableConfiguration> _tableConfigurationsByTypeName = [];

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
        /// Gets all configured PostgreSQL table names.
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

        #region Internal Static Methods

        /// <summary>
        /// Parses configuration settings into validated provider options.
        /// </summary>
        /// <param name="host">PostgreSQL server hostname in AWS RDS format.</param>
        /// <param name="port">PostgreSQL server port number.</param>
        /// <param name="database">PostgreSQL database name.</param>
        /// <param name="dbUser">PostgreSQL database username.</param>
        /// <param name="tableConfigurations">Array of table configurations to validate.</param>
        /// <returns>Validated provider options with type-to-table mappings.</returns>
        /// <exception cref="ConfigurationErrorsException">Thrown when host format is invalid.</exception>
        /// <exception cref="AggregateException">Thrown when configuration contains duplicate type mappings.</exception>
        internal static PostgresDataProviderOptions Parse(
            string host,
            int port,
            string database,
            string dbUser,
            TableConfiguration[] tableConfigurations)
        {
            // Extract region from AWS RDS hostname format
            var match = HostRegex().Match(host);
            if (match.Success is false)
            {
                throw new ConfigurationErrorsException($"The Host '{host}' is not valid. It should be in the format '<instanceName>.<uniqueId>.<region>.rds.amazonaws.com'.");
            }

            // Parse region from hostname
            var regionSystemName = match.Groups["region"].Value;
            var region = RegionEndpoint.GetBySystemName(regionSystemName)
                ?? throw new ConfigurationErrorsException($"The Host '{host}' is not valid. It should be in the format '<instanceName>.<uniqueId>.<region>.rds.amazonaws.com'.");

            // Create options instance with connection details
            var options = new PostgresDataProviderOptions(
                region: region,
                host: host,
                port: port,
                database: database,
                dbUser: dbUser);

            // Group configurations by type name to detect duplicates
            var groups = tableConfigurations
                .GroupBy(o => o.TypeName)
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
                options._tableConfigurationsByTypeName[group.Key] = group.Single();
            });

            return options;
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Gets regex pattern for parsing AWS RDS PostgreSQL hostnames.
        /// </summary>
        /// <returns>Compiled regex for extracting region from RDS hostname format.</returns>
        [GeneratedRegex(@"^(?<instanceName>[^.]+)\.(?<uniqueId>[^.]+)\.(?<region>[a-z]{2}-[a-z]+-\d)\.rds\.amazonaws\.com$")]
        private static partial Regex HostRegex();

        #endregion
    }

    #endregion
}
