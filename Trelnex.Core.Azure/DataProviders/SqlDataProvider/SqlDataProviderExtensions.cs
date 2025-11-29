using System.Collections;
using System.Configuration;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.Core;
using FluentValidation;
using LinqToDB;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
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
    /// <returns>A task that completes when all providers are registered.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required configuration sections are missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown when ServiceConfiguration is not registered.</exception>
    public static async Task<IServiceCollection> AddSqlDataProvidersAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Get Azure credentials from registered credential provider
        var credentialProvider = services.GetCredentialProvider<TokenCredential>();
        var tokenCredential = credentialProvider.GetCredential();

        // Extract SQL Server configuration from application settings
        var dataSource = configuration.GetSection("Azure.SqlDataProviders:DataSource").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.SqlDataProviders configuration is not valid.");

        var initialCatalog = configuration.GetSection("Azure.SqlDataProviders:InitialCatalog").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.SqlDataProviders configuration is not valid.");

        // Get service configuration from DI container
        var serviceDescriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // Standard Azure SQL Database authentication scope
        var scope = "https://database.windows.net/.default";

        // Create BeforeConnectionOpened callback for Azure AD authentication
        void beforeConnectionOpened(DbConnection dbConnection)
        {
            // Only process SqlConnection
            if (dbConnection is not SqlConnection connection) return;

            // Generate Azure AD authentication token
            var tokenRequestContext = new TokenRequestContext(scopes: [scope]);
            var accessToken = tokenCredential.GetToken(tokenRequestContext, default);

            connection.AccessToken = accessToken.Token;
        }

        // Create base DataOptions with SQL Server connection string
        var connectionStringBuilder = new SqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            DataSource = dataSource,
            InitialCatalog = initialCatalog,
            Encrypt = true
        };

        var baseDataOptions = new DataOptions().UseSqlServer(connectionStringBuilder.ConnectionString);

        // Load table configurations
        var tables = configuration.GetSection("Azure.SqlDataProviders:Tables").GetChildren();
        var tableConfigurations = tables
            .Select(section =>
            {
                var itemTableName = section.GetValue<string>("ItemTableName")
                    ?? throw new ConfigurationErrorsException("The Azure.SqlDataProviders configuration is not valid.");

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
    /// Configuration mapping a type name to its SQL Server table and settings.
    /// </summary>
    private record TableConfiguration
    {
        /// <summary>
        /// The logical type name identifier.
        /// </summary>
        public required string TypeName { get; init; }

        /// <summary>
        /// Callback to configure connection before opening (Azure AD token authentication).
        /// </summary>
        public required Action<DbConnection> BeforeConnectionOpened { get; init; }

        /// <summary>
        /// The physical SQL Server table name for items.
        /// </summary>
        public required string ItemTableName { get; init; }

        /// <summary>
        /// The physical SQL Server table name for events, or null if EventPolicy is Disabled.
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
    /// Captures registration configurations for SQL Server data providers.
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
        /// Captures a SQL Server data provider registration for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier that maps to a SQL Server table.</param>
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
        /// <param name="baseDataOptions">Base DataOptions with SQL Server connection configuration.</param>
        /// <param name="logger">Logger for recording registration activities.</param>
        /// <returns>A task that completes when the provider is registered.</returns>
        Task CreateAndRegisterAsync(
            IServiceCollection services,
            DataOptions baseDataOptions,
            ILogger logger);
    }

    /// <summary>
    /// Captures registration data for a single SQL Server data provider.
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
        /// Creates and registers the SQL Server data provider with the service collection.
        /// </summary>
        /// <param name="services">Service collection for provider registration.</param>
        /// <param name="baseDataOptions">Base DataOptions with SQL Server connection configuration.</param>
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
            var dataProvider = new SqlDataProvider<TItem>(
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
                "Added SqlDataProvider<{TItem:l}>: typeName = '{typeName:l}', itemTableName = '{itemTableName:l}', eventTableName = '{eventTableName:l}', commandOperations = '{commandOperations}'.",
                typeof(TItem),
                TypeName,
                TableConfiguration.ItemTableName,
                TableConfiguration.EventTableName,
                CommandOperations);

            return Task.CompletedTask;
        }
    }

    #endregion
}
