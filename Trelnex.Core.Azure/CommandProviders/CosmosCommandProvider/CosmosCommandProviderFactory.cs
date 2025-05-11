using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Security.KeyVault.Keys.Cryptography;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Encryption;
using Microsoft.Azure.Cosmos.Fluent;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// Factory for creating CosmosDB command providers.
/// </summary>
/// <remarks>Manages CosmosDB client initialization, connection, encryption setup, and provider creation.</remarks>
internal class CosmosCommandProviderFactory : ICommandProviderFactory
{
    #region Private Fields

    /// <summary>
    /// The configured Cosmos DB client.
    /// </summary>
    private readonly CosmosClient _cosmosClient;

    /// <summary>
    /// The database ID to use for all command providers.
    /// </summary>
    private readonly string _databaseId;

    /// <summary>
    /// Function to retrieve the current operational status.
    /// </summary>
    private readonly Func<CommandProviderFactoryStatus> _getStatus;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosCommandProviderFactory"/> class.
    /// </summary>
    /// <param name="cosmosClient">The configured CosmosDB client.</param>
    /// <param name="databaseId">The database ID to use.</param>
    /// <param name="getStatus">Function that provides operational status information.</param>
    private CosmosCommandProviderFactory(
        CosmosClient cosmosClient,
        string databaseId,
        Func<CommandProviderFactoryStatus> getStatus)
    {
        _cosmosClient = cosmosClient;
        _databaseId = databaseId;
        _getStatus = getStatus;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="CosmosCommandProviderFactory"/>.
    /// </summary>
    /// <param name="cosmosClientOptions">Options for CosmosDB client configuration.</param>
    /// <param name="keyResolverOptions">Options for Key Vault encryption key resolution.</param>
    /// <returns>A fully initialized <see cref="CosmosCommandProviderFactory"/> instance.</returns>
    /// <exception cref="CommandException">When the CosmosDB connection cannot be established or required containers are missing.</exception>
    /// <remarks>Configures JSON serialization, initializes the CosmosDB client, sets up encryption, and validates database health.</remarks>
    public static async Task<CosmosCommandProviderFactory> Create(
        CosmosClientOptions cosmosClientOptions,
        KeyResolverOptions keyResolverOptions)
    {
        // Configure JSON serialization settings
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Build the list of ( database, container ) tuples
        var containers = cosmosClientOptions.ContainerIds
            .Select(container => (cosmosClientOptions.DatabaseId, container))
            .ToList()
            .AsReadOnly();

        // Create the cosmos client
        var cosmosClient = await
            new CosmosClientBuilder(
                cosmosClientOptions.AccountEndpoint,
                cosmosClientOptions.TokenCredential)
            .WithCustomSerializer(new SystemTextJsonSerializer(jsonSerializerOptions))
            .WithHttpClientFactory(() => new HttpClient(new SocketsHttpHandler(), disposeHandler: false))
            .BuildAndInitializeAsync(
                containers,
                CancellationToken.None
            );

        // Add encryption
        cosmosClient = cosmosClient.WithEncryption(
            new KeyResolver(keyResolverOptions.TokenCredential),
            KeyEncryptionKeyResolverName.AzureKeyVault);

        // Function to retrieve the current operational status.
        CommandProviderFactoryStatus getStatus()
        {
            var data = new Dictionary<string, object>
            {
                { "accountEndpoint", cosmosClientOptions.AccountEndpoint },
                { "databaseId", cosmosClientOptions.DatabaseId },
                { "containerIds", cosmosClientOptions.ContainerIds },
            };

            try
            {
                // Get the database
                var database = cosmosClient.GetDatabase(cosmosClientOptions.DatabaseId);

                // Get the containers
                ContainerProperties[] getContainers()
                {
                    // Initialize a collection to hold all container properties from the database.
                    var containers = new List<ContainerProperties>();

                    // Create a query iterator to retrieve all containers in the database.
                    var feedIterator = database.GetContainerQueryIterator<ContainerProperties>();

                    // Process all pages of results until we've exhausted the feed.
                    while (feedIterator.HasMoreResults)
                    {
                        // Synchronously read the next page of results.
                        var feedResponse = feedIterator.ReadNextAsync().GetAwaiter().GetResult();

                        // Add each container from this page to our collection.
                        foreach (var container in feedResponse)
                        {
                            containers.Add(container);
                        }
                    }

                    // Return an array for immutability and to match the expected return type.
                    return containers.ToArray();
                }

                var containers = getContainers();

                // Compare the requested container IDs against the actual containers in the database.
                var missingContainerIds = new List<string>();

                // Sort the container IDs to ensure consistent ordering in error messages.
                foreach (var containerId in cosmosClientOptions.ContainerIds.OrderBy(containerId => containerId))
                {
                    // Check if this container ID exists in the retrieved containers.
                    if (containers.Any(containerProperties => containerProperties.Id == containerId) is false)
                    {
                        // Track any missing containers that will need to be created.
                        missingContainerIds.Add(containerId);
                    }
                }

                if (0 != missingContainerIds.Count)
                {
                    data["error"] = $"Missing ContainerIds: {string.Join(", ", missingContainerIds)}";
                }

                return new CommandProviderFactoryStatus(
                    IsHealthy: 0 == missingContainerIds.Count,
                    Data: data);
            }
            catch (Exception exception)
            {
                data["error"] = exception.Message;

                return new CommandProviderFactoryStatus(
                    IsHealthy: false,
                    Data: data);
            }
        }

        var factoryStatus = getStatus();
        if (factoryStatus.IsHealthy is false)
        {
            throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                factoryStatus.Data["error"] as string);
        }

        return new CosmosCommandProviderFactory(
            cosmosClient,
            cosmosClientOptions.DatabaseId,
            getStatus);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a command provider for a specific item type.
    /// </summary>
    /// <typeparam name="TInterface">Interface type for the items.</typeparam>
    /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
    /// <param name="containerId">ID of the CosmosDB container to use.</param>
    /// <param name="typeName">Type name to filter items by.</param>
    /// <param name="validator">Optional validator for items.</param>
    /// <param name="commandOperations">Operations allowed for this provider.</param>
    /// <returns>A configured <see cref="ICommandProvider{TInterface}"/> instance.</returns>
    /// <remarks>Uses the specified container for all operations on the given type.</remarks>
    public ICommandProvider<TInterface> Create<TInterface, TItem>(
        string containerId,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // Retrieve the specific Cosmos DB container instance.
        var container = _cosmosClient.GetContainer(
            _databaseId,
            containerId);

        // Instantiate a new command provider.
        return new CosmosCommandProvider<TInterface, TItem>(
            container,
            typeName,
            validator,
            commandOperations);
    }

    /// <summary>
    /// Gets the current operational status of the factory.
    /// </summary>
    /// <returns>Status information including connectivity and container availability.</returns>
    public CommandProviderFactoryStatus GetStatus() => _getStatus();

    #endregion
}
