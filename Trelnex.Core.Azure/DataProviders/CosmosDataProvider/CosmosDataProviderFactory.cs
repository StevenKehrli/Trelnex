using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Factory for creating Cosmos DB data providers.
/// </summary>
/// <remarks>
/// Manages Cosmos DB client initialization, connection, and provider creation.
/// </remarks>
internal class CosmosDataProviderFactory : IDataProviderFactory
{
    #region Private Fields

    /// <summary>
    /// The configured Cosmos DB client instance.
    /// </summary>
    private readonly CosmosClient _cosmosClient;

    /// <summary>
    /// The options used to configure the Cosmos DB client.
    /// </summary>
    private readonly CosmosClientOptions _cosmosClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDataProviderFactory"/> class.
    /// </summary>
    /// <param name="cosmosClient">The configured Cosmos DB client.</param>
    /// <param name="cosmosClientOptions">The options used to configure the Cosmos DB client.</param>
    private CosmosDataProviderFactory(
        CosmosClient cosmosClient,
        CosmosClientOptions cosmosClientOptions)
    {
        _cosmosClient = cosmosClient;
        _cosmosClientOptions = cosmosClientOptions;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="CosmosDataProviderFactory"/>.
    /// </summary>
    /// <param name="cosmosClientOptions">Options for configuring the Cosmos DB client.</param>
    /// <returns>A fully initialized <see cref="CosmosDataProviderFactory"/> instance.</returns>
    /// <exception cref="CommandException">
    /// Thrown when the Cosmos DB connection cannot be established or required containers are missing.
    /// </exception>
    /// <remarks>
    /// Configures JSON serialization, initializes the Cosmos DB client, and validates database health.
    /// </remarks>
    public static async Task<CosmosDataProviderFactory> Create(
        CosmosClientOptions cosmosClientOptions)
    {
        // Configure JSON serialization settings to ignore null values and use relaxed JSON escaping.
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Build the list of (database, container) tuples from the provided container IDs.
        var containers = cosmosClientOptions.ContainerIds
            .Select(container => (cosmosClientOptions.DatabaseId, container))
            .ToList()
            .AsReadOnly();

        // Create and initialize the Cosmos DB client.
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

        // Create a new instance of the CosmosDataProviderFactory.
        var factory = new CosmosDataProviderFactory(
            cosmosClient,
            cosmosClientOptions);

        // Get the operational status of the factory.
        var factoryStatus = await factory.GetStatusAsync();

        // Return the factory if it is healthy; otherwise, throw an exception.
        return factoryStatus.IsHealthy
            ? factory
            : throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                factoryStatus.Data["error"] as string);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a data provider for a specific item type.
    /// </summary>
    /// <typeparam name="TInterface">The interface type for the items.</typeparam>
    /// <typeparam name="TItem">The concrete implementation type for the items.</typeparam>
    /// <param name="containerId">The ID of the Cosmos DB container to use.</param>
    /// <param name="typeName">The type name to filter items by.</param>
    /// <param name="validator">An optional validator for items.</param>
    /// <param name="commandOperations">The operations allowed for this provider.</param>
    /// <param name="blockCipherService">Optional block cipher service for encrypting sensitive data.</param>
    /// <returns>A configured <see cref="IDataProvider{TInterface}"/> instance.</returns>
    /// <remarks>Uses the specified container for all operations on the given type.</remarks>
    public IDataProvider<TInterface> Create<TInterface, TItem>(
        string containerId,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null,
        IBlockCipherService? blockCipherService = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // Retrieve the specific Cosmos DB container instance.
        var container = _cosmosClient.GetContainer(
            _cosmosClientOptions.DatabaseId,
            containerId);

        // Instantiate a new data provider.
        return new CosmosDataProvider<TInterface, TItem>(
            container,
            typeName,
            validator,
            commandOperations,
            blockCipherService);
    }

    /// <summary>
    /// Asynchronously gets the current operational status of the factory.
    /// </summary>
    /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
    /// <returns>Status information including connectivity and container availability.</returns>
    public async Task<DataProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        // Initialize a dictionary to hold status data.
        var data = new Dictionary<string, object>
        {
            { "accountEndpoint", _cosmosClientOptions.AccountEndpoint },
            { "databaseId", _cosmosClientOptions.DatabaseId },
            { "containerIds", _cosmosClientOptions.ContainerIds },
        };

        try
        {
            // Retrieve the container properties from the Cosmos DB.
            var containers = await GetContainers(
                _cosmosClient,
                _cosmosClientOptions.DatabaseId,
                cancellationToken);

            // Compare the requested container IDs against the actual containers in the database.
            var missingContainerIds = new List<string>();

            // Sort the container IDs to ensure consistent ordering in error messages.
            foreach (var containerId in _cosmosClientOptions.ContainerIds.OrderBy(containerId => containerId))
            {
                // Check if this container ID exists in the retrieved containers.
                if (containers.Any(cp => cp.Id == containerId) is false)
                {
                    // Track any missing containers.
                    missingContainerIds.Add(containerId);
                }
            }

            // If there are any missing container IDs, add an error message to the status data.
            if (0 != missingContainerIds.Count)
            {
                data["error"] = $"Missing ContainerIds: {string.Join(", ", missingContainerIds)}";
            }

            // Return a healthy status if there are no missing container IDs.
            return new DataProviderFactoryStatus(
                IsHealthy: 0 == missingContainerIds.Count,
                Data: data);
        }
        catch (Exception exception)
        {
            // If an exception occurs, add the error message to the status data.
            data["error"] = exception.Message;

            // Return an unhealthy status with the error data.
            return new DataProviderFactoryStatus(
                IsHealthy: false,
                Data: data);
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Retrieves an array of container properties from the Cosmos DB.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    /// <param name="databaseId">The ID of the database.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
    /// <returns>An array of container properties.</returns>
    private static async Task<ContainerProperties[]> GetContainers(
        CosmosClient cosmosClient,
        string databaseId,
        CancellationToken cancellationToken)
    {
        // Get the database
        var database = cosmosClient.GetDatabase(databaseId);

        // Initialize a collection to hold all container properties from the database.
        var containers = new List<ContainerProperties>();

        // Create a query iterator to retrieve all containers in the database.
        var feedIterator = database.GetContainerQueryIterator<ContainerProperties>();

        // Process all pages of results until we've exhausted the feed.
        while (feedIterator.HasMoreResults)
        {
            // Synchronously read the next page of results.
            var feedResponse = await feedIterator.ReadNextAsync(cancellationToken);

            // Add each container from this page to our collection.
            foreach (var container in feedResponse)
            {
                containers.Add(container);
            }
        }

        // Return an array for immutability and to match the expected return type.
        return containers.ToArray();
    }

    #endregion
}
