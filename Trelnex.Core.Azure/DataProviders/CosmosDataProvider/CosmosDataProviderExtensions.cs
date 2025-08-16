using System.Configuration;
using Azure.Core;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Encryption;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
using Trelnex.Core.Identity;

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
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required configuration sections are missing.</exception>
    /// <exception cref="ArgumentException">Thrown when a type name has no associated container configuration.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to register duplicate providers.</exception>
    public static IServiceCollection AddCosmosDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Get Azure credentials from registered credential provider
        var credentialProvider = services.GetCredentialProvider<TokenCredential>();

        // Extract Cosmos DB configuration from application settings
        var endpointUri = configuration.GetSection("Azure.CosmosDataProviders:EndpointUri").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.CosmosDataProviders configuration is not found.");

        var databaseId = configuration.GetSection("Azure.CosmosDataProviders:DatabaseId").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.CosmosDataProviders configuration is not found.");

        var containers = configuration.GetSection("Azure.CosmosDataProviders:Containers").GetChildren();
        var containerConfigurations = containers
            .Select(section =>
            {
                var containerId = section.GetValue<string>("ContainerId")
                    ?? throw new ConfigurationErrorsException("The Azure.CosmosDataProviders configuration is not found.");

                var eventTimeToLive = section.GetValue<int?>("EventTimeToLive");

                var blockCipherService = section.CreateBlockCipherService();

                return new ContainerConfiguration(
                    TypeName: section.Key,
                    ContainerId: containerId,
                    EventTimeToLive: eventTimeToLive,
                    BlockCipherService: blockCipherService);
            })
            .ToArray();

        // Build provider options from configuration
        var providerOptions = CosmosDataProviderOptions.Parse(
            endpointUri: endpointUri,
            databaseId: databaseId,
            containerConfigurations: containerConfigurations);

        // Configure Cosmos DB client with credentials and connection details
        var cosmosClientOptions = GetCosmosClientOptions(credentialProvider, providerOptions);

        var providerFactory = CosmosDataProviderFactory
            .Create(cosmosClientOptions)
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
    /// Creates Cosmos DB client options with Azure credentials and authentication scope.
    /// </summary>
    /// <param name="credentialProvider">Provider for Azure credentials.</param>
    /// <param name="providerOptions">Cosmos DB configuration options.</param>
    /// <returns>Configured Cosmos DB client options.</returns>
    private static CosmosClientOptions GetCosmosClientOptions(
        ICredentialProvider<TokenCredential> credentialProvider,
        CosmosDataProviderOptions providerOptions)
    {
        // Get Azure credentials and configure authentication scope
        var tokenCredential = credentialProvider.GetCredential();

        // Build authentication scope from endpoint URI
        var uri = new Uri(providerOptions.EndpointUri);

        var scope = new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: ".default",
            extraValue: uri.Query).Uri.ToString();

        var tokenRequestContext = new TokenRequestContext(
            scopes: [scope]);

        // Pre-authenticate to verify credentials
        tokenCredential.GetToken(tokenRequestContext, default);

        return new CosmosClientOptions(
            AccountEndpoint: providerOptions.EndpointUri,
            TokenCredential: tokenCredential,
            DatabaseId: providerOptions.DatabaseId,
            ContainerIds: providerOptions.GetContainerIds());
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Handles registration of Cosmos DB data providers with type-to-container mapping.
    /// </summary>
    private class DataProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        CosmosDataProviderFactory providerFactory,
        CosmosDataProviderOptions providerOptions)
        : IDataProviderOptions
    {
        #region Public Methods

        /// <summary>
        /// Registers a Cosmos DB data provider for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier that maps to a Cosmos DB container.</param>
        /// <param name="itemValidator">Optional validator for entity validation.</param>
        /// <param name="commandOperations">Optional CRUD operations to enable.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when no container is configured for the type name.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a provider for this type is already registered.</exception>
        public IDataProviderOptions Add<TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TItem : BaseItem, new()
        {
            // Look up container configuration for the specified type
            var containerConfiguration = providerOptions.GetContainerConfiguration(typeName);

            if (containerConfiguration is null)
            {
                throw new ArgumentException(
                    $"The Container for TypeName '{typeName}' is not found.",
                    nameof(typeName));
            }

            if (services.Any(sd => sd.ServiceType == typeof(IDataProvider<TItem>)))
            {
                throw new InvalidOperationException(
                    $"The DataProvider<{typeof(TItem).Name}> is already registered.");
            }

            // Create data provider instance using factory and container configuration
            var dataProvider = providerFactory.Create(
                typeName: typeName,
                containerId: containerConfiguration.ContainerId,
                itemValidator: itemValidator,
                commandOperations: commandOperations,
                eventTimeToLive: containerConfiguration.EventTimeToLive,
                blockCipherService: containerConfiguration.BlockCipherService,
                logger: bootstrapLogger);

            services.AddSingleton(dataProvider);

            object[] args =
            [
                typeof(TItem), // TItem,
                providerOptions.EndpointUri, // account
                providerOptions.DatabaseId, // database,
                containerConfiguration.ContainerId, // container
            ];

            // Log successful provider registration
            bootstrapLogger.LogInformation(
                message: "Added CosmosDataProvider<{TItem:l}>: endpointUri = '{endpointUri:l}', databaseId = '{databaseId:l}', containerId = '{containerId:l}'.",
                args: args);

            return this;
        }

        #endregion
    }

    #endregion

    #region Configuration Records

    /// <summary>
    /// Configuration mapping a type name to its Cosmos DB container and settings.
    /// </summary>
    /// <param name="TypeName">The logical type name identifier.</param>
    /// <param name="ContainerId">The physical Cosmos DB container identifier.</param>
    /// <param name="EventTimeToLive">Optional TTL for events in seconds.</param>
    /// <param name="BlockCipherService">Optional encryption service for the container.</param>
    private record ContainerConfiguration(
        string TypeName,
        string ContainerId,
        int? EventTimeToLive,
        IBlockCipherService? BlockCipherService);

    #endregion

    #region Provider Options

    /// <summary>
    /// Configuration options for Cosmos DB data providers with type-to-container mappings.
    /// </summary>
    /// <param name="endpointUri">Cosmos DB account endpoint URI.</param>
    /// <param name="databaseId">Cosmos DB database identifier.</param>
    private class CosmosDataProviderOptions(
        string endpointUri,
        string databaseId)
    {
        #region Private Fields

        // Dictionary mapping type names to their container configurations
        private readonly Dictionary<string, ContainerConfiguration> _containerConfigurationsByTypeName = [];

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Parses configuration arrays into validated provider options.
        /// </summary>
        /// <param name="endpointUri">Cosmos DB account endpoint URI.</param>
        /// <param name="databaseId">Cosmos DB database identifier.</param>
        /// <param name="containerConfigurations">Array of container configurations to validate.</param>
        /// <returns>Validated provider options with type-to-container mappings.</returns>
        /// <exception cref="AggregateException">Thrown when configuration contains duplicate type mappings.</exception>
        public static CosmosDataProviderOptions Parse(
            string endpointUri,
            string databaseId,
            ContainerConfiguration[] containerConfigurations)
        {
            var providerOptions = new CosmosDataProviderOptions(
                endpointUri: endpointUri,
                databaseId: databaseId);

            // Group configurations by type name to detect duplicates
            var groups = containerConfigurations
                .GroupBy(o => o.TypeName)
                .ToArray();

            // Validate configuration for duplicate type mappings
            var exs = new List<ConfigurationErrorsException>();

            Array.ForEach(groups, group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A Container for TypeName '{group.Key} is specified more than once."));
            });

            // Throw aggregate exception if validation errors found
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // Build type-to-container mapping dictionary
            Array.ForEach(groups, group =>
            {
                providerOptions._containerConfigurationsByTypeName[group.Key] = group.Single();
            });

            return providerOptions;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the Cosmos DB database identifier.
        /// </summary>
        public string DatabaseId => databaseId;

        /// <summary>
        /// Gets the Cosmos DB account endpoint URI.
        /// </summary>
        public string EndpointUri => endpointUri;

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves container configuration for a specified type name.
        /// </summary>
        /// <param name="typeName">Type name to look up.</param>
        /// <returns>Container configuration if found, null otherwise.</returns>
        public ContainerConfiguration? GetContainerConfiguration(
            string typeName)
        {
            return _containerConfigurationsByTypeName.TryGetValue(typeName, out var containerConfiguration)
                ? containerConfiguration
                : null;
        }

        /// <summary>
        /// Gets all configured Cosmos DB container identifiers.
        /// </summary>
        /// <returns>Sorted array of unique container identifiers.</returns>
        public string[] GetContainerIds()
        {
            return _containerConfigurationsByTypeName
                .Select(c => c.Value.ContainerId)
                .OrderBy(containerId => containerId)
                .ToArray();
        }

        #endregion
    }

    #endregion
}
