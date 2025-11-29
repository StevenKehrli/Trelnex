using System.Collections;
using System.Configuration;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using FluentValidation;
using LinqToDB;
using Npgsql;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
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
    /// <returns>A task that completes when all providers are registered.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required configuration sections are missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown when ServiceConfiguration is not registered or duplicate providers are registered.</exception>
    /// <exception cref="ArgumentException">Thrown when a type name has no associated table configuration.</exception>
    public static async Task<IServiceCollection> AddPostgresDataProvidersAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Get AWS credentials from registered credential provider
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();
        var credentials = credentialProvider.GetCredential();

        // Extract PostgreSQL configuration from application settings
        var host = configuration.GetSection("Amazon.PostgresDataProviders:Host").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not valid.");

        var port = configuration.GetSection("Amazon.PostgresDataProviders:Port").Get<int?>()
            ?? 5432;

        var database = configuration.GetSection("Amazon.PostgresDataProviders:Database").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not valid.");

        var dbUser = configuration.GetSection("Amazon.PostgresDataProviders:DbUser").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not valid.");

        // Extract region from AWS RDS hostname format
        var match = HostRegex().Match(host);
        if (match.Success is false)
        {
            throw new ConfigurationErrorsException($"The Host '{host}' is not valid. It should be in the format '<instanceName>.<uniqueId>.<region>.rds.amazonaws.com'.");
        }

        var regionSystemName = match.Groups["region"].Value;
        var region = RegionEndpoint.GetBySystemName(regionSystemName)
            ?? throw new ConfigurationErrorsException($"The Host '{host}' is not valid. It should be in the format '<instanceName>.<uniqueId>.<region>.rds.amazonaws.com'.");

        // Get service configuration from DI container
        var serviceDescriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // Create BeforeConnectionOpened callback for AWS IAM authentication
        void beforeConnectionOpened(DbConnection dbConnection)
        {
            // Only process Npgsql connections
            if (dbConnection is not Npgsql.NpgsqlConnection connection) return;

            // Generate AWS IAM authentication token for PostgreSQL
            var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
                credentials: credentials,
                region: region,
                hostname: host,
                port: port,
                dbUser: dbUser);

            // Update connection string with generated authentication token
            var csb = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
            {
                Password = pwd,
                SslMode = SslMode.Require
            };

            connection.ConnectionString = csb.ConnectionString;
        }

        // Create base DataOptions with PostgreSQL connection string
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            Host = host,
            Port = port,
            Database = database,
            Username = dbUser,
            SslMode = SslMode.Require
        };

        var baseDataOptions = new DataOptions().UsePostgreSQL(connectionStringBuilder.ConnectionString);

        // Load table configurations
        var tables = configuration.GetSection("Amazon.PostgresDataProviders:Tables").GetChildren();
        var tableConfigurations = tables
            .Select(section =>
            {
                var itemTableName = section.GetValue<string>("ItemTableName")
                    ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not valid.");

                var eventPolicy = section.GetValue("EventPolicy", EventPolicy.AllChanges);
                var blockCipherService = section.CreateBlockCipherService();

                // If EventPolicy is Disabled, return configuration without event table
                if (eventPolicy == EventPolicy.Disabled)
                {
                    return new TableConfiguration
                    {
                        TypeName = section.Key,
                        BeforeConnectionOpened = beforeConnectionOpened,
                        ItemTableName = itemTableName,
                        EventPolicy = eventPolicy,
                        BlockCipherService = blockCipherService
                    };
                }

                // Load event table configuration
                var eventTableName = section.GetValue<string>("EventTableName", $"{itemTableName}-events");
                var eventTimeToLive = section.GetValue<int?>("EventTimeToLive");

                return new TableConfiguration
                {
                    TypeName = section.Key,
                    BeforeConnectionOpened = beforeConnectionOpened,
                    ItemTableName = itemTableName,
                    EventTableName = eventTableName,
                    EventPolicy = eventPolicy,
                    EventTimeToLive = eventTimeToLive,
                    BlockCipherService = blockCipherService
                };
            })
            .ToArray();

        // Create options to capture registrations
        var options = new DataProviderOptions(tableConfigurations);

        // Execute user configuration to capture registrations
        configureDataProviders(options);

        // Create and register each provider
        foreach (var registration in options)
        {
            await registration.CreateAndRegisterAsync(services, baseDataOptions, bootstrapLogger);
        }

        return services;
    }

    #endregion

    #region TableConfiguration

    /// <summary>
    /// Configuration mapping a type name to its PostgreSQL table and settings.
    /// </summary>
    private record TableConfiguration
    {
        /// <summary>
        /// The logical type name identifier.
        /// </summary>
        public required string TypeName { get; init; }

        /// <summary>
        /// Callback to configure connection before opening (AWS IAM auth token generation).
        /// </summary>
        public required Action<DbConnection> BeforeConnectionOpened { get; init; }

        /// <summary>
        /// The physical PostgreSQL table name for items.
        /// </summary>
        public required string ItemTableName { get; init; }

        /// <summary>
        /// The physical PostgreSQL table name for events, or null if EventPolicy is Disabled.
        /// </summary>
        public string? EventTableName { get; init; } = null;

        /// <summary>
        /// Event policy for change tracking.
        /// </summary>
        public required EventPolicy EventPolicy { get; init; }

        /// <summary>
        /// Optional TTL for events in seconds.
        /// </summary>
        public int? EventTimeToLive { get; init; } = null;

        /// <summary>
        /// Optional encryption service for the table.
        /// </summary>
        public IBlockCipherService? BlockCipherService { get; init; } = null;
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Captures registration configurations for PostgreSQL data providers.
    /// </summary>
    private class DataProviderOptions(
        TableConfiguration[] tableConfigurations)
        : IDataProviderOptions, IEnumerable<IDataProviderRegistration>
    {
        #region Private Fields

        /// <summary>
        /// Dictionary mapping type names to their table configurations.
        /// </summary>
        private readonly Dictionary<string, TableConfiguration> _tableConfigurationsByTypeName =
            tableConfigurations.ToDictionary(tc => tc.TypeName);

        /// <summary>
        /// Collection of provider registrations to process.
        /// </summary>
        private readonly List<IDataProviderRegistration> _registrations = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Captures a PostgreSQL data provider registration for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier that maps to a PostgreSQL table.</param>
        /// <param name="itemValidator">Optional validator for entity validation.</param>
        /// <param name="commandOperations">Optional CRUD operations to enable.</param>
        /// <returns>The options instance for method chaining.</returns>
        public IDataProviderOptions Add<TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TItem : BaseItem, new()
        {
            // Capture the registration data
            var registration = new DataProviderRegistration<TItem>
            {
                TypeName = typeName,
                ItemValidator = itemValidator,
                CommandOperations = commandOperations,
                TableConfiguration = _tableConfigurationsByTypeName[typeName]
            };

            _registrations.Add(registration);

            return this;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the registrations.
        /// </summary>
        /// <returns>An enumerator for the registrations.</returns>
        public IEnumerator<IDataProviderRegistration> GetEnumerator() => _registrations.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the registrations.
        /// </summary>
        /// <returns>An enumerator for the registrations.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }

    #endregion

    #region DataProviderRegistration

    /// <summary>
    /// Interface for type-erased provider registration storage.
    /// </summary>
    private interface IDataProviderRegistration
    {
        /// <summary>
        /// Creates and registers the data provider with the service collection.
        /// </summary>
        /// <param name="services">Service collection for provider registration.</param>
        /// <param name="baseDataOptions">Base DataOptions with PostgreSQL connection configuration.</param>
        /// <param name="logger">Logger for recording registration activities.</param>
        /// <returns>A task that completes when the provider is registered.</returns>
        Task CreateAndRegisterAsync(
            IServiceCollection services,
            DataOptions baseDataOptions,
            ILogger logger);
    }

    /// <summary>
    /// Captures registration data for a single PostgreSQL data provider.
    /// </summary>
    /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
    private record DataProviderRegistration<TItem>
        : IDataProviderRegistration
        where TItem : BaseItem, new()
    {
        /// <summary>
        /// Type name identifier for the entity in storage.
        /// </summary>
        public required string TypeName { get; init; }

        /// <summary>
        /// Optional validator for entity validation.
        /// </summary>
        public IValidator<TItem>? ItemValidator { get; init; } = null;

        /// <summary>
        /// Optional CRUD operations to enable.
        /// </summary>
        public CommandOperations? CommandOperations { get; init; } = null;

        /// <summary>
        /// Table configuration for this provider.
        /// </summary>
        public required TableConfiguration TableConfiguration { get; init; }

        /// <summary>
        /// Creates and registers the PostgreSQL data provider with the service collection.
        /// </summary>
        /// <param name="services">Service collection for provider registration.</param>
        /// <param name="baseDataOptions">Base DataOptions with PostgreSQL connection configuration.</param>
        /// <param name="logger">Logger for recording registration activities.</param>
        /// <returns>A task that completes when the provider is registered.</returns>
        public Task CreateAndRegisterAsync(
            IServiceCollection services,
            DataOptions baseDataOptions,
            ILogger logger)
        {
            // Build configured DataOptions with mapping schema
            var dataOptions = DataOptionsBuilder.Build<TItem>(
                baseDataOptions: baseDataOptions,
                beforeConnectionOpened: TableConfiguration.BeforeConnectionOpened,
                itemTableName: TableConfiguration.ItemTableName,
                eventTableName: TableConfiguration.EventTableName,
                blockCipherService: TableConfiguration.BlockCipherService);

            // Create data provider instance using constructor
            var dataProvider = new PostgresDataProvider<TItem>(
                typeName: TypeName,
                dataOptions: dataOptions,
                itemValidator: ItemValidator,
                commandOperations: CommandOperations,
                eventPolicy: TableConfiguration.EventPolicy,
                eventTimeToLive: TableConfiguration.EventTimeToLive,
                blockCipherService: TableConfiguration.BlockCipherService,
                logger: logger);

            // Register provider as singleton in DI container
            services.AddSingleton<IDataProvider<TItem>>(dataProvider);

            // Log successful registration
            logger.LogInformation(
                "Added PostgresDataProvider<{TItem:l}>: typeName = '{typeName:l}', itemTableName = '{itemTableName:l}', eventTableName = '{eventTableName:l}', commandOperations = '{commandOperations}'.",
                typeof(TItem),
                TypeName,
                TableConfiguration.ItemTableName,
                TableConfiguration.EventTableName,
                CommandOperations);

            return Task.CompletedTask;
        }
    }

    #endregion

    #region Regex Methods

    /// <summary>
    /// Gets regex pattern for parsing AWS RDS PostgreSQL hostnames.
    /// </summary>
    /// <returns>Compiled regex for extracting region from RDS hostname format.</returns>
    [GeneratedRegex(@"^(?<instanceName>[^.]+)\.(?<uniqueId>[^.]+)\.(?<region>[a-z]{2}-[a-z]+-\d)\.rds\.amazonaws\.com$")]
    private static partial Regex HostRegex();

    #endregion
}
