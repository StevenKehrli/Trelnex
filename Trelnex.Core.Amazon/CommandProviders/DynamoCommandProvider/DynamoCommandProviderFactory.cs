using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentValidation;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Encryption;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// Factory for creating DynamoDB command providers.
/// </summary>
/// <remarks>
/// Manages DynamoDB client initialization, table validation, and provider creation.
/// </remarks>
internal class DynamoCommandProviderFactory : ICommandProviderFactory
{
    #region Private Fields

    /// <summary>
    /// The DynamoDB client.
    /// </summary>
    private readonly AmazonDynamoDBClient _dynamoClient;

    /// <summary>
    /// The options used to configure the DynamoDB client.
    /// </summary>
    private readonly DynamoClientOptions _dynamoClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoCommandProviderFactory"/> class.
    /// </summary>
    /// <param name="dynamoClient">The configured DynamoDB client.</param>
    /// <param name="dynamoClientOptions">The options used to configure the DynamoDB client.</param>
    private DynamoCommandProviderFactory(
        AmazonDynamoDBClient dynamoClient,
        DynamoClientOptions dynamoClientOptions)
    {
        _dynamoClient = dynamoClient;
        _dynamoClientOptions = dynamoClientOptions;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="DynamoCommandProviderFactory"/>.
    /// </summary>
    /// <param name="dynamoClientOptions">Options for DynamoDB client configuration.</param>
    /// <returns>A fully initialized <see cref="DynamoCommandProviderFactory"/> instance.</returns>
    /// <exception cref="CommandException">Thrown when the DynamoDB connection cannot be established or tables are missing.</exception>
    /// <remarks>
    /// Initializes the DynamoDB client and validates that all required tables exist.
    /// </remarks>
    public static async Task<DynamoCommandProviderFactory> Create(
        DynamoClientOptions dynamoClientOptions)
    {
        // Create the DynamoDB client using the provided AWS credentials and region.
        var dynamoClient = new AmazonDynamoDBClient(
            dynamoClientOptions.AWSCredentials,
            RegionEndpoint.GetBySystemName(dynamoClientOptions.Region));

        // Build and return the factory instance.
        var factory = new DynamoCommandProviderFactory(
            dynamoClient,
            dynamoClientOptions);

        // Get the operational status of the factory.
        var status = await factory.GetStatusAsync();

        // Return the factory if it is healthy; otherwise, throw an exception.
        return (status.IsHealthy is true)
            ? factory
            : throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                status.Data["error"] as string);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a command provider for a specific item type.
    /// </summary>
    /// <typeparam name="TInterface">Interface type for the items.</typeparam>
    /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
    /// <param name="tableName">Name of the DynamoDB table to use.</param>
    /// <param name="typeName">Type name to filter items by.</param>
    /// <param name="validator">Optional validator for items.</param>
    /// <param name="commandOperations">Operations allowed for this provider.</param>
    /// <param name="encryptionService">Optional encryption service for encrypting sensitive data.</param>
    /// <returns>A configured <see cref="ICommandProvider{TInterface}"/> instance.</returns>
    /// <remarks>
    /// Creates a <see cref="DynamoCommandProvider{TInterface, TItem}"/> that operates on the specified DynamoDB table.
    /// </remarks>
    public ICommandProvider<TInterface> Create<TInterface, TItem>(
        string tableName,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null,
        IEncryptionService? encryptionService = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // Get a Table object with the standard key schema for the specified table name.
        var table = _dynamoClient.GetTable(tableName);

        // Create and return the command provider instance.
        return new DynamoCommandProvider<TInterface, TItem>(
            table,
            typeName,
            validator,
            commandOperations,
            encryptionService);
    }

    /// <summary>
    /// Gets the current operational status of the factory.
    /// </summary>
    /// <returns>Status information including connectivity and table availability.</returns>
    public CommandProviderFactoryStatus GetStatus()
    {
        return GetStatusAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously gets the current operational status of the factory.
    /// </summary>
    /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
    /// <returns>Status information including connectivity and table availability.</returns>
    public async Task<CommandProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        // Initialize a dictionary to hold status data.
        var data = new Dictionary<string, object>
        {
            { "region", _dynamoClientOptions.Region },
            { "tableNames", _dynamoClientOptions.TableNames },
        };

        try
        {
            // Retrieve the table names from DynamoDB.
            var tableNames = await GetTableNames(
                _dynamoClient,
                cancellationToken);

            // Identify any required tables that are missing.
            var missingTableNames = new List<string>();
            foreach (var tableName in _dynamoClientOptions.TableNames.OrderBy(tableName => tableName))
            {
                // Check if the table exists in DynamoDB.
                if (tableNames.Any(tn => tn == tableName) is false)
                {
                    // Add the missing table name to the list.
                    missingTableNames.Add(tableName);
                }
            }

            // If there are any missing table names, add an error message to the status data.
            if (0 != missingTableNames.Count)
            {
                data["error"] = $"Missing Tables: {string.Join(", ", missingTableNames)}";
            }

            // Return a healthy status if there are no missing table names.
            return new CommandProviderFactoryStatus(
                IsHealthy: 0 == missingTableNames.Count,
                Data: data);
        }
        catch (Exception ex)
        {
            // If an exception occurs, add the error message to the status data.
            data["error"] = ex.Message;

            // Return an unhealthy status with the error data.
            return new CommandProviderFactoryStatus(
                IsHealthy: false,
                Data: data);
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Retrieves an array of table names from DynamoDB.
    /// </summary>
    /// <param name="dynamoClient">The DynamoDB client.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
    /// <returns>An array of table names.</returns>
    private static async Task<string[]> GetTableNames(
        AmazonDynamoDBClient dynamoClient,
        CancellationToken cancellationToken)
    {
        // Initialize a list to hold the table names.
        var tableNames = new List<string>();

        // Initialize the last evaluated table name to null.
        string? lastEvaluatedTableName = null;

        // Paginate through the table names.
        do
        {
            // Get the next batch of table names.
            var request = new ListTablesRequest
            {
                ExclusiveStartTableName = lastEvaluatedTableName
            };

            var response = await dynamoClient.ListTablesAsync(request);

            // Add the table names to the list.
            tableNames.AddRange(response.TableNames);

            // Update the last evaluated table name.
            lastEvaluatedTableName = response.LastEvaluatedTableName;
        } while (lastEvaluatedTableName is not null); // Continue while there are more tables to retrieve.

        // Return the table names as an array.
        return tableNames.ToArray();
    }

    #endregion
}
