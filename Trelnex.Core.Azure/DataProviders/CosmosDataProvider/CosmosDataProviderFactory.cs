using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Factory for creating Cosmos DB data provider instances with container validation.
/// </summary>
internal class CosmosDataProviderFactory : IDataProviderFactory
{
    #region Private Fields

    // Configured Cosmos DB client for database operations
    private readonly CosmosClient _cosmosClient;

    // Configuration options for the Cosmos DB client
    private readonly CosmosClientOptions _cosmosClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new Cosmos DB data provider factory with client and options.
    /// </summary>
    /// <param name="cosmosClient">Configured Cosmos DB client.</param>
    /// <param name="cosmosClientOptions">Client configuration options.</param>
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
    /// Creates and validates a new Cosmos DB data provider factory instance.
    /// </summary>
    /// <param name="cosmosClientOptions">Cosmos DB client configuration options.</param>
    /// <returns>Validated factory instance ready for use.</returns>
    /// <exception cref="CommandException">Thrown when Cosmos DB connection fails or containers are missing.</exception>
    public static async Task<CosmosDataProviderFactory> Create(
        CosmosClientOptions cosmosClientOptions)
    {
        // Configure JSON serialization for Cosmos DB operations
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Build list of (database, container) tuples for initialization
        var containers = cosmosClientOptions.ContainerIds
            .Select(container => (cosmosClientOptions.DatabaseId, container))
            .ToList()
            .AsReadOnly();

        // Initialize Cosmos DB client with custom serialization and HTTP configuration
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

        // Create factory instance
        var factory = new CosmosDataProviderFactory(
            cosmosClient,
            cosmosClientOptions);

        // Verify factory health and container availability
        var factoryStatus = await factory.GetStatusAsync();

        // Return factory if healthy, otherwise throw exception with error details
        return factoryStatus.IsHealthy
            ? factory
            : throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                factoryStatus.Data["error"] as string);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a Cosmos DB data provider for the specified item type and container.
    /// </summary>
    /// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
    /// <param name="typeName">Type name identifier for filtering items.</param>
    /// <param name="containerId">Cosmos DB container identifier to operate on.</param>
    /// <param name="itemValidator">Optional validator for items.</param>
    /// <param name="commandOperations">Allowed CRUD operations for this provider.</param>
    /// <param name="eventTimeToLive">Optional TTL for events in seconds.</param>
    /// <param name="blockCipherService">Optional encryption service for sensitive data.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>Configured Cosmos DB data provider instance.</returns>
    public IDataProvider<TItem> Create<TItem>(
        string typeName,
        string containerId,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        EventPolicy? eventPolicy = null,
        int? eventTimeToLive = null,
        IBlockCipherService? blockCipherService = null,
        ILogger? logger = null)
        where TItem : BaseItem, new()
    {
        // Get Cosmos DB container instance
        var container = _cosmosClient.GetContainer(
            _cosmosClientOptions.DatabaseId,
            containerId);

        // Create and return configured data provider
        return new CosmosDataProvider<TItem>(
            typeName: typeName,
            container: container,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            eventTimeToLive: eventTimeToLive,
            blockCipherService: blockCipherService,
            logger: logger);
    }

    /// <summary>
    /// Retrieves the current operational status of the factory and Cosmos DB connectivity.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the status check operation.</param>
    /// <returns>Status information including health, connectivity, and container availability.</returns>
    public async Task<DataProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        // Initialize status data with basic configuration
        var data = new Dictionary<string, object>
        {
            { "accountEndpoint", _cosmosClientOptions.AccountEndpoint },
            { "databaseId", _cosmosClientOptions.DatabaseId },
            { "containerIds", _cosmosClientOptions.ContainerIds },
        };

        try
        {
            // Get list of existing containers from Cosmos DB
            var containers = await GetContainers(
                _cosmosClient,
                _cosmosClientOptions.DatabaseId,
                cancellationToken);

            // Check for missing required containers
            var missingContainerIds = new List<string>();

            foreach (var containerId in _cosmosClientOptions.ContainerIds.OrderBy(containerId => containerId))
            {
                // Verify container exists in Cosmos DB
                if (containers.Any(cp => cp.Id == containerId) is false)
                {
                    missingContainerIds.Add(containerId);
                }
            }

            // Add error information if containers are missing
            if (0 != missingContainerIds.Count)
            {
                data["error"] = $"Missing ContainerIds: {string.Join(", ", missingContainerIds)}";
            }

            // Return status based on container availability
            return new DataProviderFactoryStatus(
                IsHealthy: 0 == missingContainerIds.Count,
                Data: data);
        }
        catch (Exception exception)
        {
            // Add exception details to status data
            data["error"] = exception.Message;

            // Return unhealthy status with error information
            return new DataProviderFactoryStatus(
                IsHealthy: false,
                Data: data);
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Retrieves all container properties from the specified Cosmos DB database.
    /// </summary>
    /// <param name="cosmosClient">Cosmos DB client for database operations.</param>
    /// <param name="databaseId">Database identifier to query containers from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Array of all container properties in the database.</returns>
    private static async Task<ContainerProperties[]> GetContainers(
        CosmosClient cosmosClient,
        string databaseId,
        CancellationToken cancellationToken)
    {
        // Get database instance
        var database = cosmosClient.GetDatabase(databaseId);

        // Collect container properties across all pages
        var containers = new List<ContainerProperties>();

        // Query all containers using feed iterator
        var feedIterator = database.GetContainerQueryIterator<ContainerProperties>();

        // Process all pages of container results
        while (feedIterator.HasMoreResults)
        {
            var feedResponse = await feedIterator.ReadNextAsync(cancellationToken);

            // Add containers from current page
            foreach (var container in feedResponse)
            {
                containers.Add(container);
            }
        }

        return containers.ToArray();
    }

    #endregion
}
