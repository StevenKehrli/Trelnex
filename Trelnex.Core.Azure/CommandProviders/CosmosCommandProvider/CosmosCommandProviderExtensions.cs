using System.Configuration;
using Azure.Core;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// Extension methods for configuring and registering Cosmos DB command providers.
/// </summary>
/// <remarks>Provides dependency injection integration for Cosmos DB command providers.</remarks>
public static class CosmosCommandProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds Cosmos DB command providers to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add providers to.</param>
    /// <param name="configuration">Application configuration containing provider settings.</param>
    /// <param name="bootstrapLogger">Logger for recording provider initialization.</param>
    /// <param name="configureCommandProviders">Action to configure specific providers.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">When the CosmosCommandProviders section is missing from configuration.</exception>
    /// <exception cref="ArgumentException">When a requested type name has no associated container.</exception>
    /// <exception cref="InvalidOperationException">When attempting to register the same command provider interface twice.</exception>
    /// <remarks>Registers command providers for specific types.</remarks>
    public static IServiceCollection AddCosmosCommandProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<ICommandProviderOptions> configureCommandProviders)
    {
        // Retrieve the token credential provider.
        var credentialProvider = services.GetCredentialProvider<TokenCredential>();

        // Get the database and container configurations from the configuration
        var endpointUri = configuration.GetSection("Azure.CosmosCommandProviders:EndpointUri").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.CosmosCommandProviders configuration is not found.");

        var databaseId = configuration.GetSection("Azure.CosmosCommandProviders:DatabaseId").Get<string>()
            ?? throw new ConfigurationErrorsException("The Azure.CosmosCommandProviders configuration is not found.");

        var containers = configuration.GetSection("Azure.CosmosCommandProviders:Containers").GetChildren();
        var containerConfigurations = containers
            .Select(c =>
            {
                var containerId = c.GetValue<string>("ContainerId")
                    ?? throw new ConfigurationErrorsException("The Azure.CosmosCommandProviders configuration is not found.");

                var encryptionSecret = c.GetValue<string?>("EncryptionSecret");

                return new ContainerConfiguration(
                    TypeName: c.Key,
                    ContainerId: containerId,
                    EncryptionSecret: encryptionSecret);
            })
            .ToArray();

        // Parse the cosmos options
        var providerOptions = CosmosCommandProviderOptions.Parse(
            endpointUri: endpointUri,
            databaseId: databaseId,
            containerConfigurations: containerConfigurations);

        // Create our factory
        var cosmosClientOptions = GetCosmosClientOptions(credentialProvider, providerOptions);

        var providerFactory = CosmosCommandProviderFactory
            .Create(cosmosClientOptions)
            .GetAwaiter()
            .GetResult();

        // Inject the factory as the status interface
        services.AddCommandProviderFactory(providerFactory);

        // Create the command providers and inject
        var commandProviderOptions = new CommandProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory,
            providerOptions: providerOptions);

        // Execute the configuration
        configureCommandProviders(commandProviderOptions);

        return services;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates Cosmos DB client options with properly configured authentication.
    /// </summary>
    /// <param name="credentialProvider">Provider for token-based authentication.</param>
    /// <param name="providerOptions">Configuration options for Cosmos DB.</param>
    /// <returns>Fully configured Cosmos DB client options.</returns>
    /// <remarks>Initializes an access token with proper scope formatting for Cosmos DB.</remarks>
    private static CosmosClientOptions GetCosmosClientOptions(
        ICredentialProvider<TokenCredential> credentialProvider,
        CosmosCommandProviderOptions providerOptions)
    {
        // Get the token credential and initialize
        var tokenCredential = credentialProvider.GetCredential();

        // Format the scope
        var uri = new Uri(providerOptions.EndpointUri);

        var scope = new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: ".default",
            extraValue: uri.Query).Uri.ToString();

        var tokenRequestContext = new TokenRequestContext(
            scopes: [scope]);

        // Warm-up this token
        tokenCredential.GetToken(tokenRequestContext, default);

        return new CosmosClientOptions(
            AccountEndpoint: providerOptions.EndpointUri,
            TokenCredential: tokenCredential,
            DatabaseId: providerOptions.DatabaseId,
            ContainerIds: providerOptions.GetContainerIds());
    }

    #endregion

    #region CommandProviderOptions

    /// <summary>
    /// Implementation of <see cref="ICommandProviderOptions"/> for configuring Cosmos DB providers.
    /// </summary>
    /// <remarks>Provides type-to-container mapping and command provider registration.</remarks>
    private class CommandProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        CosmosCommandProviderFactory providerFactory,
        CosmosCommandProviderOptions providerOptions)
        : ICommandProviderOptions
    {
        #region Public Methods

        /// <summary>
        /// Registers a command provider for a specific item type with container mapping.
        /// </summary>
        /// <typeparam name="TInterface">Interface type for the items.</typeparam>
        /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
        /// <param name="typeName">Type name to map to a container.</param>
        /// <param name="itemValidator">Optional validator for items.</param>
        /// <param name="commandOperations">Operations allowed for this provider.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="ArgumentException">When no container is configured for the specified type name.</exception>
        /// <exception cref="InvalidOperationException">When a command provider for the interface is already registered.</exception>
        /// <remarks>Maps a logical entity type with its physical storage location.</remarks>
        public ICommandProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // Get the container for the specified item type
            var containerConfiguration = providerOptions.GetContainerConfiguration(typeName);

            // If the container is not found, then throw an exception
            if (containerConfiguration is null)
            {
                throw new ArgumentException(
                    $"The Container for TypeName '{typeName}' is not found.",
                    nameof(typeName));
            }

            // If the command provider is already registered, then throw an exception
            if (services.Any(sd => sd.ServiceType == typeof(ICommandProvider<TInterface>)))
            {
                throw new InvalidOperationException(
                    $"The CommandProvider<{typeof(TInterface).Name}> is already registered.");
            }

            // Create the command provider and inject
            var commandProvider = providerFactory.Create<TInterface, TItem>(
                containerId: containerConfiguration.ContainerId,
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations);

            // Register the command provider
            services.AddSingleton(commandProvider);

            // Prepare logging parameters to record the registration.
            object[] args =
            [
                typeof(TInterface), // TInterface,
                typeof(TItem), // TItem,
                providerOptions.EndpointUri, // account
                providerOptions.DatabaseId, // database,
                containerConfiguration.ContainerId, // container
            ];

            // Log - the :l format parameter (l = literal) to avoid the quotes
            bootstrapLogger.LogInformation(
                message: "Added CosmosCommandProvider<{TInterface:l}, {TItem:l}>: endpointUri = '{endpointUri:l}', databaseId = '{databaseId:l}', containerId = '{containerId:l}'.",
                args: args);

            return this;
        }

        #endregion
    }

    #endregion

    #region Configuration Records

    /// <summary>
    /// Container configuration mapping type names to container IDs.
    /// </summary>
    /// <param name="TypeName">The type name used for filtering items.</param>
    /// <param name="ContainerId">The container ID in Cosmos DB.</param>
    /// <param name="EncryptionSecret">Optional secret for encryption.</param>
    /// <remarks>Defines the mapping between logical type names and physical containers.</remarks>
    private record ContainerConfiguration(
        string TypeName,
        string ContainerId,
        string? EncryptionSecret);

    #endregion

    #region Provider Options

    /// <summary>
    /// Runtime options for Cosmos DB command providers.
    /// </summary>
    /// <param name="endpointUri">The Cosmos DB account endpoint URI.</param>
    /// <param name="databaseId">The database ID.</param>
    /// <remarks>Provides validated, parsed configuration with container-to-type mappings.</remarks>
    private class CosmosCommandProviderOptions(
        string endpointUri,
        string databaseId)
    {
        #region Private Fields

        /// <summary>
        /// The mappings from type names to container configurations.
        /// </summary>
        private readonly Dictionary<string, ContainerConfiguration> _containerConfigurationsByTypeName = [];

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Parses configuration into a validated options object.
        /// </summary>
        /// <param name="endpointUri">The Cosmos DB account endpoint URI.</param>
        /// <param name="databaseId">The database ID.</param>
        /// <param name="containerConfigurations">Array of container configurations.</param>
        /// <returns>Validated options with type-to-container mappings.</returns>
        /// <exception cref="AggregateException">When configuration contains duplicate type mappings.</exception>
        /// <remarks>Validates that no type name is mapped to multiple containers.</remarks>
        public static CosmosCommandProviderOptions Parse(
            string endpointUri,
            string databaseId,
            ContainerConfiguration[] containerConfigurations)
        {
            // Get the tenant, endpoint, and database
            var providerOptions = new CosmosCommandProviderOptions(
                endpointUri: endpointUri,
                databaseId: databaseId);

            // Group the containers by item type
            var groups = containerConfigurations
                .GroupBy(o => o.TypeName)
                .ToArray();

            // Any exceptions
            var exs = new List<ConfigurationErrorsException>();

            // Enumerate each group - should be one
            Array.ForEach(groups, group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A Container for TypeName '{group.Key} is specified more than once."));
            });

            // If there are any exceptions, then throw an aggregate exception of all exceptions
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // After validating that no type name is mapped to multiple containers,
            // populate the dictionary that maps each type name to its corresponding container configuration.
            Array.ForEach(groups, group =>
            {
                // Extract the single container configuration for this type name and add it to the lookup dictionary.
                providerOptions._containerConfigurationsByTypeName[group.Key] = group.Single();
            });

            // Return the fully initialized and validated options object for use in creating providers.
            return providerOptions;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the database ID.
        /// </summary>
        public string DatabaseId => databaseId;

        /// <summary>
        /// Gets the Cosmos DB account endpoint URI.
        /// </summary>
        public string EndpointUri => endpointUri;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the container configuration for a specified type name.
        /// </summary>
        /// <param name="typeName">The type name to look up.</param>
        /// <returns>The container configuration if found, or <see langword="null"/> if no mapping exists.</returns>
        public ContainerConfiguration? GetContainerConfiguration(
            string typeName)
        {
            // Try to find the container ID corresponding to the provided type name in our lookup dictionary.
            return _containerConfigurationsByTypeName.TryGetValue(typeName, out var containerConfiguration)
                ? containerConfiguration
                : null;
        }

        /// <summary>
        /// Gets all configured container IDs.
        /// </summary>
        /// <returns>Array of distinct container IDs sorted alphabetically.</returns>
        public string[] GetContainerIds()
        {
            // Extract all container IDs from the dictionary's values, ensuring no duplicates.
            return _containerConfigurationsByTypeName
                .Select(c => c.Value.ContainerId)
                .OrderBy(containerId => containerId)
                .ToArray();
        }

        #endregion
    }

    #endregion
}
