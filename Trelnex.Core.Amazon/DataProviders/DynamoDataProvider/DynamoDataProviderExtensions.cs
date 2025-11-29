using System.Collections;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using FluentValidation;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Encryption;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Extension methods for configuring DynamoDB data providers with dependency injection.
/// </summary>
public static class DynamoDataProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers DynamoDB data providers with the service collection using configuration.
    /// </summary>
    /// <param name="services">Service collection to register providers with.</param>
    /// <param name="configuration">Application configuration containing DynamoDB settings.</param>
    /// <param name="bootstrapLogger">Logger for recording registration activities.</param>
    /// <param name="configureDataProviders">Delegate to configure which providers to register.</param>
    /// <returns>A task that completes when all providers are registered.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required configuration sections are missing.</exception>
    /// <exception cref="ArgumentException">Thrown when a type name has no associated table configuration.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to register duplicate providers.</exception>
    public static async Task<IServiceCollection> AddDynamoDataProvidersAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Get AWS credentials from registered credential provider
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();
        var credentials = credentialProvider.GetCredential();

        // Extract DynamoDB configuration from application settings
        var region = configuration.GetSection("Amazon.DynamoDataProviders:Region").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.DynamoDataProviders configuration is not valid.");

        // Create DynamoDB client
        var dynamoClient = new AmazonDynamoDBClient(
            credentials,
            RegionEndpoint.GetBySystemName(region));

        // Load table configurations asynchronously
        var tables = configuration.GetSection("Amazon.DynamoDataProviders:Tables").GetChildren();
        var tableConfigurations = await Task.WhenAll(
            tables.Select(async section =>
            {
                var itemTableName = section.GetValue<string>("ItemTableName")
                    ?? throw new ConfigurationErrorsException("The Amazon.DynamoDataProviders configuration is not valid.");

                // Load item table asynchronously
                var itemTable = await dynamoClient.LoadTableAsync(
                    bootstrapLogger,
                    itemTableName);

                var eventPolicy = section.GetValue("EventPolicy", EventPolicy.AllChanges);
                var blockCipherService = section.CreateBlockCipherService();

                // If EventPolicy is Disabled, return configuration without event table
                if (eventPolicy == EventPolicy.Disabled)
                {
                    return new TableConfiguration
                    {
                        TypeName = section.Key,
                        Region = region,
                        ItemTable = itemTable,
                        EventTable = null,
                        EventPolicy = eventPolicy,
                        EventTimeToLive = null,
                        BlockCipherService = blockCipherService
                    };
                }

                // Load event table and configuration
                var eventTableName = section.GetValue<string>("EventTableName", $"{itemTableName}-events");
                var eventTable = await dynamoClient.LoadTableAsync(bootstrapLogger, eventTableName);
                var eventTimeToLive = section.GetValue<int?>("EventTimeToLive");

                return new TableConfiguration
                {
                    TypeName = section.Key,
                    Region = region,
                    ItemTable = itemTable,
                    EventTable = eventTable,
                    EventPolicy = eventPolicy,
                    EventTimeToLive = eventTimeToLive,
                    BlockCipherService = blockCipherService
                };
            }));

        // Create options to capture registrations
        var options = new DataProviderOptions(tableConfigurations);

        // Execute user configuration to capture registrations
        configureDataProviders(options);

        // Create and register each provider
        foreach (var registration in options)
        {
            await registration.CreateAndRegisterAsync(services, bootstrapLogger);
        }

        return services;
    }

    #endregion

    #region TableConfiguration

    /// <summary>
    /// Configuration mapping a type name to its DynamoDB table and settings.
    /// </summary>
    private record TableConfiguration
    {
        /// <summary>
        /// The logical type name identifier.
        /// </summary>
        public required string TypeName { get; init; }

        /// <summary>
        /// AWS region where DynamoDB tables are located.
        /// </summary>
        public required string Region { get; init; }

        /// <summary>
        /// The loaded DynamoDB table for items.
        /// </summary>
        public required Table ItemTable { get; init; }

        /// <summary>
        /// The loaded DynamoDB table for events, or null if EventPolicy is Disabled.
        /// </summary>
        public Table? EventTable { get; init; } = null;

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
    /// Captures registration configurations for DynamoDB data providers.
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
        /// Captures a DynamoDB data provider registration for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier that maps to a DynamoDB table.</param>
        /// <param name="itemValidator">Optional validator for entity validation.</param>
        /// <param name="commandOperations">Optional CRUD operations to enable.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when no table is configured for the type name.</exception>
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
        /// <param name="logger">Logger for recording registration activities.</param>
        /// <returns>A task that completes when the provider is registered.</returns>
        Task CreateAndRegisterAsync(
            IServiceCollection services,
            ILogger logger);
    }

    /// <summary>
    /// Captures registration data for a single DynamoDB data provider.
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
        /// Creates and registers the DynamoDB data provider with the service collection.
        /// </summary>
        /// <param name="services">Service collection for provider registration.</param>
        /// <param name="logger">Logger for recording registration activities.</param>
        /// <returns>A task that completes when the provider is registered.</returns>
        public Task CreateAndRegisterAsync(
            IServiceCollection services,
            ILogger logger)
        {
            // Create data provider instance using constructor with Table objects
            var dataProvider = new DynamoDataProvider<TItem>(
                typeName: TypeName,
                itemTable: TableConfiguration.ItemTable,
                eventTable: TableConfiguration.EventTable,
                itemValidator: ItemValidator,
                commandOperations: CommandOperations,
                eventPolicy: TableConfiguration.EventPolicy,
                eventTimeToLive: TableConfiguration.EventTimeToLive,
                blockCipherService: TableConfiguration.BlockCipherService,
                logger: logger);

            // Register provider as singleton in DI container
            services.AddSingleton<IDataProvider<TItem>>(dataProvider);

            // Log successful registration
            object?[] args =
            [
                typeof(TItem),                            // TItem
                TypeName,                                 // typeName
                TableConfiguration.Region,                // region
                TableConfiguration.ItemTable.TableName,   // itemTableName
                TableConfiguration.EventTable?.TableName, // eventTableName
                CommandOperations                         // commandOperations
            ];

            logger.LogInformation(
                message: "Added DynamoDataProvider<{TItem:l}>: typeName = '{typeName:l}', region = '{region:l}', itemTableName = '{itemTableName:l}', eventTableName = '{eventTableName:l}', commandOperations = '{commandOperations}'.",
                args: args);

            return Task.CompletedTask;
        }
    }

    #endregion
}
