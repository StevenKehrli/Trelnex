using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using FluentValidation;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// A builder for creating an instance of the <see cref="DynamoCommandProvider"/>.
/// </summary>
internal class DynamoCommandProviderFactory : ICommandProviderFactory
{
    private readonly AmazonDynamoDBClient _dynamoClient;
    private readonly Func<CommandProviderFactoryStatus> _getStatus;

    private DynamoCommandProviderFactory(
        AmazonDynamoDBClient dynamoClient,
        Func<CommandProviderFactoryStatus> getStatus)
    {
        _dynamoClient = dynamoClient;
        _getStatus = getStatus;
    }

    /// <summary>
    /// Create an instance of the <see cref="DynamoCommandProviderFactory"/>.
    /// </summary>
    /// <param name="dynamoClientOptions">The <see cref="DynamoClient"/> options.</param>
    /// <returns>The <see cref="DynamoCommandProviderFactory"/>.</returns>
    public static async Task<DynamoCommandProviderFactory> Create(
        DynamoClientOptions dynamoClientOptions)
    {
        // create the dynamo client
        var dynamoClient = new AmazonDynamoDBClient(
            dynamoClientOptions.AWSCredentials,
            RegionEndpoint.GetBySystemName(dynamoClientOptions.RegionName));

        CommandProviderFactoryStatus getStatus()
        {
            var data = new Dictionary<string, object>
            {
                { "regionName", dynamoClientOptions.RegionName },
                { "tableNames", dynamoClientOptions.TableNames },
            };

            try
            {
                // get the tables
                string[] getTableNames()
                {
                    var tableNames = new List<string>();

                    string? lastEvaluatedTableName = null;

                    do
                    {
                        // get the next batch of table names
                        var request = new ListTablesRequest
                        {
                            ExclusiveStartTableName = lastEvaluatedTableName
                        };

                        var response = dynamoClient.ListTablesAsync(request).Result;

                        tableNames.AddRange(response.TableNames);

                        lastEvaluatedTableName = response.LastEvaluatedTableName;
                    } while (lastEvaluatedTableName is not null);

                    return tableNames.ToArray();
                }

                var tableNames = getTableNames();

                // get any tables not found
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

        // build the factory
        var factory = new DynamoCommandProviderFactory(
            dynamoClient,
            getStatus);

        return await Task.FromResult(factory);
    }

    /// <summary>
    /// Create an instance of the <see cref="DynamoCommandProvider"/>.
    /// </summary>
    /// <param name="tableName">The table name for the item.</param>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <param name="validator">The fluent validator for the item.</param>
    /// <param name="commandOperations">The value indicating if update and delete commands are allowed. By default, update is allowed; delete is not allowed.</param>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <typeparam name="TItem">The specified item type that implements the specified interface type.</typeparam>
    /// <returns>The <see cref="DynamoCommandProvider"/>.</returns>
    public ICommandProvider<TInterface> Create<TInterface, TItem>(
        string tableName,
        string typeName,
        AbstractValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        var table = Table.LoadTable(
            _dynamoClient,
            tableName);

        return new DynamoCommandProvider<TInterface, TItem>(
            table,
            typeName,
            validator,
            commandOperations);
    }

    public CommandProviderFactoryStatus GetStatus() => _getStatus();
}
