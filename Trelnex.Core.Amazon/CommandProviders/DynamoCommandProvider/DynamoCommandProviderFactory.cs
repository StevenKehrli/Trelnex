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
    /// Function to retrieve the current operational status.
    /// </summary>
    private readonly Func<CommandProviderFactoryStatus> _getStatus;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoCommandProviderFactory"/> class.
    /// </summary>
    /// <param name="dynamoClient">The configured DynamoDB client.</param>
    /// <param name="getStatus">Function that provides operational status information.</param>
    private DynamoCommandProviderFactory(
        AmazonDynamoDBClient dynamoClient,
        Func<CommandProviderFactoryStatus> getStatus)
    {
        _dynamoClient = dynamoClient;
        _getStatus = getStatus;
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

        // Function to retrieve the current operational status of the factory.
        CommandProviderFactoryStatus getStatus()
        {
            var data = new Dictionary<string, object>
            {
                { "region", dynamoClientOptions.Region },
                { "tableNames", dynamoClientOptions.TableNames },
            };

            try
            {
                // Retrieve the list of table names from DynamoDB.
                string[] getTableNames()
                {
                    var tableNames = new List<string>();

                    string? lastEvaluatedTableName = null;

                    do
                    {
                        // Get the next batch of table names.
                        var request = new ListTablesRequest
                        {
                            ExclusiveStartTableName = lastEvaluatedTableName
                        };

                        var response = dynamoClient.ListTablesAsync(request).GetAwaiter().GetResult();

                        tableNames.AddRange(response.TableNames);

                        lastEvaluatedTableName = response.LastEvaluatedTableName;
                    } while (lastEvaluatedTableName is not null);

                    return tableNames.ToArray();
                }

                var tableNames = getTableNames();

                // Identify any required tables that are missing.
                var missingTableNames = new List<string>();
                foreach (var tableName in dynamoClientOptions.TableNames.OrderBy(tableName => tableName))
                {
                    if (tableNames.Any(tn => tn == tableName) is false)
                    {
                        missingTableNames.Add(tableName);
                    }
                }

                if (0 != missingTableNames.Count)
                {
                    data["error"] = $"Missing Tables: {string.Join(", ", missingTableNames)}";
                }

                return new CommandProviderFactoryStatus(
                    IsHealthy: 0 == missingTableNames.Count,
                    Data: data);
            }
            catch (Exception ex)
            {
                data["error"] = ex.Message;

                return new CommandProviderFactoryStatus(
                    IsHealthy: false,
                    Data: data);
            }
        }

        var status = getStatus();
        if (status.IsHealthy is false)
        {
            throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                status.Data["error"] as string);
        }

        // Build and return the factory instance.
        var factory = new DynamoCommandProviderFactory(
            dynamoClient,
            getStatus);

        return await Task.FromResult(factory);
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
    /// <returns>Status information.</returns>
    /// <remarks>
    /// Provides information about the DynamoDB connection and table availability.
    /// </remarks>
    public CommandProviderFactoryStatus GetStatus() => _getStatus();

    #endregion
}
