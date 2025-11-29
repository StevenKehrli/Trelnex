using System.Collections;
using System.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.Core;
using FluentValidation;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Encryption;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Extension methods for configuring Cosmos DB data providers with dependency injection.
/// </summary>
public static class CosmosDataProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers Cosmos DB data providers with the service collection using configuration.
    /// </summary>
    /// <param name="services">Service collection to register providers with.</param>
    /// <param name="configuration">Application configuration containing Cosmos DB settings.</param>
    /// <param name="bootstrapLogger">Logger for recording registration activities.</param>
    /// <param name="configureDataProviders">Delegate to configure which providers to register.</param>
    /// <returns>A task that completes when all providers are registered.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required configuration sections are missing.</exception>
    public static async Task<IServiceCollection> AddCosmosDataProvidersAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Get Azure credentials from registered credential provider
        var credentialProvider = services.GetCredentialProvider<TokenCredential>();
        var tokenCredential = credentialProvider.GetCredential();

        // Extract Cosmos DB configuration from application settings
        var endpointUri = configuration.GetSection("Azure.CosmosDataProviders:EndpointUri").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.CosmosDataProviders configuration is not valid.");

        var databaseId = configuration.GetSection("Azure.CosmosDataProviders:DatabaseId").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.CosmosDataProviders configuration is not valid.");

        // Get container IDs for initialization
        var containers = configuration.GetSection("Azure.CosmosDataProviders:Containers").GetChildren();
        var databaseAndContainers = containers
            .Select(section => section.GetValue<string>("ContainerId")
                ?? throw new ConfigurationErrorsException("The Azure.CosmosDataProviders configuration is not valid."))
            .Distinct()
            .Select(containerId => (databaseId, containerId))
            .ToList();

        // Create and initialize Cosmos DB client with containers
        var cosmosClient = await new CosmosClientBuilder(endpointUri, tokenCredential)
            .WithCustomSerializer(new SystemTextJsonSerializer())
            .WithHttpClientFactory(() => new HttpClient(new SocketsHttpHandler(), disposeHandler: false))
            .BuildAndInitializeAsync(databaseAndContainers, CancellationToken.None);

        // Build container configurations with loaded containers
        var containerConfigurations = containers
            .Select(section =>
            {
                var containerId = section.GetValue<string>("ContainerId")
                    ?? throw new ConfigurationErrorsException("The Azure.CosmosDataProviders configuration is not valid.");

                // Get container reference from initialized client
                var container = cosmosClient.GetContainer(databaseId, containerId);

                var eventPolicy = section.GetValue("EventPolicy", EventPolicy.AllChanges);
                var eventTimeToLive = section.GetValue<int?>("EventTimeToLive");
                var blockCipherService = section.CreateBlockCipherService();

                return new ContainerConfiguration
                {
                    TypeName = section.Key,
                    Container = container,
                    EventPolicy = eventPolicy,
                    EventTimeToLive = eventTimeToLive,
                    BlockCipherService = blockCipherService
                };
            })
            .ToArray();

        // Create options to capture registrations
        var options = new DataProviderOptions(containerConfigurations);

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

    #region ContainerConfiguration

    /// <summary>
    /// Configuration mapping a type name to its Cosmos DB container and settings.
    /// </summary>
    private record ContainerConfiguration
    {
        /// <summary>
        /// The logical type name identifier.
        /// </summary>
        public required string TypeName { get; init; }

        /// <summary>
        /// The loaded Cosmos DB container.
        /// </summary>
        public required Container Container { get; init; }

        /// <summary>
        /// Event policy for change tracking.
        /// </summary>
        public required EventPolicy EventPolicy { get; init; }

        /// <summary>
        /// Optional TTL for events in seconds.
        /// </summary>
        public int? EventTimeToLive { get; init; } = null;

        /// <summary>
        /// Optional encryption service for the container.
        /// </summary>
        public IBlockCipherService? BlockCipherService { get; init; } = null;
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Captures registration configurations for Cosmos DB data providers.
    /// </summary>
    private class DataProviderOptions(
        ContainerConfiguration[] containerConfigurations)
        : IDataProviderOptions, IEnumerable<IDataProviderRegistration>
    {
        #region Private Fields

        /// <summary>
        /// Dictionary mapping type names to their container configurations.
        /// </summary>
        private readonly Dictionary<string, ContainerConfiguration> _containerConfigurationsByTypeName =
            containerConfigurations.ToDictionary(cc => cc.TypeName);

        /// <summary>
        /// Collection of provider registrations to process.
        /// </summary>
        private readonly List<IDataProviderRegistration> _registrations = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Captures a Cosmos DB data provider registration for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier that maps to a Cosmos DB container.</param>
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
                ContainerConfiguration = _containerConfigurationsByTypeName[typeName]
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
    /// Captures registration data for a single Cosmos DB data provider.
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
        /// Container configuration for this provider.
        /// </summary>
        public required ContainerConfiguration ContainerConfiguration { get; init; }

        /// <summary>
        /// Creates and registers the Cosmos DB data provider with the service collection.
        /// </summary>
        /// <param name="services">Service collection for provider registration.</param>
        /// <param name="logger">Logger for recording registration activities.</param>
        /// <returns>A task that completes when the provider is registered.</returns>
        public Task CreateAndRegisterAsync(
            IServiceCollection services,
            ILogger logger)
        {
            // Create data provider instance using constructor
            var dataProvider = new CosmosDataProvider<TItem>(
                typeName: TypeName,
                container: ContainerConfiguration.Container,
                itemValidator: ItemValidator,
                commandOperations: CommandOperations,
                eventPolicy: ContainerConfiguration.EventPolicy,
                eventTimeToLive: ContainerConfiguration.EventTimeToLive,
                blockCipherService: ContainerConfiguration.BlockCipherService,
                logger: logger);

            // Register provider as singleton in DI container
            services.AddSingleton<IDataProvider<TItem>>(dataProvider);

            // Log successful registration
            logger.LogInformation(
                "Added CosmosDataProvider<{TItem:l}>: typeName = '{typeName:l}', containerId = '{containerId:l}', commandOperations = '{commandOperations}'.",
                typeof(TItem),
                TypeName,
                ContainerConfiguration.Container.Id,
                CommandOperations);

            return Task.CompletedTask;
        }
    }

    #endregion
}
