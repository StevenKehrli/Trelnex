using System.Collections.Immutable;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

[Category("RBAC")]
[Ignore("Requires a DynamoDB table.")]
public partial class RBACRepositoryTests
{
    private AmazonDynamoDBClient _client = null!;
    private string _tableName = null!;
    private RBACRepository _repository = null!;

    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Load configuration from appsettings.json and optional user-specific settings.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Retrieve AWS credentials using the default credentials provider chain.
        var credentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Get the AWS region from configuration to ensure we connect to the correct DynamoDB endpoint.
        var region = configuration
            .GetSection("RBAC:Region")
            .Get<string>()!;

        // Convert the region string to an AWS RegionEndpoint object required by the AWS SDK.
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        // Get the DynamoDB table name that stores the RBAC data.
        var tableName = configuration
            .GetSection("RBAC:TableName")
            .Get<string>()!;

        // Initialize the DynamoDB client with credentials and region.
        _client = new AmazonDynamoDBClient(credentials, regionEndpoint);
        _tableName = tableName;

        // Create the RBAC repository with the validator, client, and table name.
        _repository = new RBACRepository(
            new ResourceNameValidator(),
            new ScopeNameValidator(),
            new RoleNameValidator(),
            _client,
            tableName);
    }

    [OneTimeTearDown]
    public void TestFixtureCleanup()
    {
        // Release resources allocated by the DynamoDB client to prevent resource leaks.
        _client.Dispose();
    }

    [TearDown]
    public async Task TestCleanup()
    {
        // Perform a scan operation to retrieve all items in the DynamoDB table.
        // This ensures we can clean up the table after each test for proper isolation.
        var scanRequest = new ScanRequest()
        {
            TableName = _tableName,
            ConsistentRead = true
        };

        // Execute the scan operation against DynamoDB.
        var scanResponse = await _client.ScanAsync(scanRequest, default);

        // If the table is empty, there's nothing to clean up.
        if (scanResponse.Items.Count == 0) return;

        // Process each DynamoDB item, extracting the primary key components (resourceName and subjectName) which are needed for deletion.
        var items = scanResponse.Items
            .Select(attributeMap =>
            {
                return (
                    entityName: attributeMap["entityName"].S,
                    subjectName: attributeMap["subjectName"].S);
            });

        // Create DeleteRequest objects for each item.
        // Each request specifies the complete primary key for the item.
        var deleteRequests = items.Select(item =>
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "entityName", new AttributeValue(item.entityName) },
                { "subjectName", new AttributeValue(item.subjectName) }
            };

            return new DeleteRequest { Key = key };
        });

        // Convert DeleteRequest objects to WriteRequest objects required by the BatchWriteItem API.
        var writeRequests = deleteRequests.Select(deleteRequest => new WriteRequest { DeleteRequest = deleteRequest });

        // Prepare the BatchWriteItemRequest, which allows deleting multiple items in a single operation.
        var batchWriteRequest = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                { _tableName, writeRequests.ToList() }
            }
        };

        // Execute batch deletes until all items are processed.
        // DynamoDB might return unprocessed items if the request exceeds service limits.
        while (batchWriteRequest.RequestItems.Count > 0)
        {
            // Execute the batch write operation and capture the response.
            var batchWriteItemResponse = await _client.BatchWriteItemAsync(batchWriteRequest, default);

            // Update the request with any unprocessed items for the next iteration.
            batchWriteRequest.RequestItems = batchWriteItemResponse.UnprocessedItems;
        }
    }

    /// <summary>
    /// Retrieves all items from the DynamoDB table used for RBAC storage.
    /// This helper method is used to verify the state of the underlying data.
    /// </summary>
    /// <returns>A list of items from the DynamoDB table, with attribute values converted to strings.</returns>
    private async Task<List<ImmutableSortedDictionary<string, string>>> GetItemsAsync()
    {
        // Create a scan request to retrieve all items from the RBAC table.
        // This is used for verification in tests to check the raw data state.
        var scanRequest = new ScanRequest()
        {
            TableName = _tableName,
            ConsistentRead = true // Use strong consistency to ensure we get the latest data
        };

        // Execute the scan operation to retrieve all items.
        var scanResponse = await _client.ScanAsync(scanRequest, default);

        // Process the raw DynamoDB items into a more usable format.
        // Convert each item's attributes into a sorted dictionary of string values.
        return scanResponse.Items
            .Select(attributeMap => attributeMap
                .ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.S))
            .OrderBy(item => item["entityName"])
            .ThenBy(item => item["subjectName"])
            .ToList();
    }
}