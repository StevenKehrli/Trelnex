using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Factory for creating DynamoDB data provider instances with table validation.
/// </summary>
internal class DynamoDataProviderFactory : IDataProviderFactory
{
    #region Private Fields

    // DynamoDB client for AWS operations
    private readonly AmazonDynamoDBClient _dynamoClient;

    // Configuration options for the DynamoDB client
    private readonly DynamoClientOptions _dynamoClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new DynamoDB data provider factory with client and options.
    /// </summary>
    /// <param name="dynamoClient">Configured DynamoDB client.</param>
    /// <param name="dynamoClientOptions">Client configuration options.</param>
    private DynamoDataProviderFactory(
        AmazonDynamoDBClient dynamoClient,
        DynamoClientOptions dynamoClientOptions)
    {
        _dynamoClient = dynamoClient;
        _dynamoClientOptions = dynamoClientOptions;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates and validates a new DynamoDB data provider factory instance.
    /// </summary>
    /// <param name="dynamoClientOptions">DynamoDB client configuration options.</param>
    /// <returns>Validated factory instance ready for use.</returns>
    /// <exception cref="CommandException">Thrown when DynamoDB connection fails or tables are missing.</exception>
    public static async Task<DynamoDataProviderFactory> Create(
        DynamoClientOptions dynamoClientOptions)
    {
        // Initialize DynamoDB client with AWS credentials and region
        var dynamoClient = new AmazonDynamoDBClient(
            dynamoClientOptions.AWSCredentials,
            RegionEndpoint.GetBySystemName(dynamoClientOptions.Region));

        // Create factory instance
        var factory = new DynamoDataProviderFactory(
            dynamoClient,
            dynamoClientOptions);

        // Verify factory health and table availability
        var status = await factory.GetStatusAsync();

        // Return factory if healthy, otherwise throw exception with error details
        return (status.IsHealthy is true)
            ? factory
            : throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                status.Data["error"] as string);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a DynamoDB data provider for the specified item type and table.
    /// </summary>
    /// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
    /// <param name="typeName">Type name identifier for filtering items.</param>
    /// <param name="tableName">DynamoDB table name to operate on.</param>
    /// <param name="itemValidator">Optional validator for items.</param>
    /// <param name="commandOperations">Allowed CRUD operations for this provider.</param>
    /// <param name="eventTimeToLive">Optional TTL for events in seconds.</param>
    /// <param name="blockCipherService">Optional encryption service for sensitive data.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>Configured DynamoDB data provider instance.</returns>
    public IDataProvider<TItem> Create<TItem>(
        string typeName,
        string tableName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        EventPolicy? eventPolicy = null,
        int? eventTimeToLive = null,
        IBlockCipherService? blockCipherService = null,
        ILogger? logger = null)
        where TItem : BaseItem, new()
    {
        // Get DynamoDB table instance with standard key schema
        var table = _dynamoClient.GetTable(tableName);

        // Create and return configured data provider
        return new DynamoDataProvider<TItem>(
            typeName: typeName,
            table: table,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            eventTimeToLive: eventTimeToLive,
            blockCipherService: blockCipherService,
            logger: logger);
    }

    /// <summary>
    /// Retrieves the current operational status of the factory and DynamoDB connectivity.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the status check operation.</param>
    /// <returns>Status information including health, connectivity, and table availability.</returns>
    public async Task<DataProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        // Initialize status data with basic configuration
        var data = new Dictionary<string, object>
        {
            { "region", _dynamoClientOptions.Region },
            { "tableNames", _dynamoClientOptions.TableNames },
        };

        try
        {
            // Get list of existing tables from DynamoDB
            var tableNames = await GetTableNames(
                _dynamoClient,
                cancellationToken);

            // Check for missing required tables
            var missingTableNames = new List<string>();
            foreach (var tableName in _dynamoClientOptions.TableNames.OrderBy(tableName => tableName))
            {
                // Verify table exists in DynamoDB
                if (tableNames.Any(tn => tn == tableName) is false)
                {
                    missingTableNames.Add(tableName);
                }
            }

            // Add error information if tables are missing
            if (0 != missingTableNames.Count)
            {
                data["error"] = $"Missing Tables: {string.Join(", ", missingTableNames)}";
            }

            // Return status based on table availability
            return new DataProviderFactoryStatus(
                IsHealthy: 0 == missingTableNames.Count,
                Data: data);
        }
        catch (Exception ex)
        {
            // Add exception details to status data
            data["error"] = ex.Message;

            // Return unhealthy status with error information
            return new DataProviderFactoryStatus(
                IsHealthy: false,
                Data: data);
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Retrieves all table names from DynamoDB using paginated requests.
    /// </summary>
    /// <param name="dynamoClient">DynamoDB client for AWS operations.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Array of all table names in the DynamoDB account.</returns>
    private static async Task<string[]> GetTableNames(
        AmazonDynamoDBClient dynamoClient,
        CancellationToken cancellationToken)
    {
        // Collect table names across all pages
        var tableNames = new List<string>();

        // Track pagination state
        string? lastEvaluatedTableName = null;

        // Process all pages of table listings
        do
        {
            // Request next page of table names
            var request = new ListTablesRequest
            {
                ExclusiveStartTableName = lastEvaluatedTableName
            };

            var response = await dynamoClient.ListTablesAsync(request);

            // Add table names from current page
            tableNames.AddRange(response.TableNames);

            // Update pagination marker for next iteration
            lastEvaluatedTableName = response.LastEvaluatedTableName;
        } while (lastEvaluatedTableName is not null);

        return tableNames.ToArray();
    }

    #endregion
}
