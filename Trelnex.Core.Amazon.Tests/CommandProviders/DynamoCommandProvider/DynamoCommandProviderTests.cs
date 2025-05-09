using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Amazon.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

/// <summary>
/// Tests for the DynamoCommandProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CommandProviderTests"/> to leverage the extensive test suite
/// defined in the base class. The base class implements a comprehensive set of tests for command provider
/// functionality including:
/// <list type="bullet">
/// <item>Batch command operations (create, update, delete with success and failure scenarios)</item>
/// <item>Create command operations (with success and conflict handling)</item>
/// <item>Delete command operations (with success and precondition failure handling)</item>
/// <item>Query command operations (with various filters, ordering, paging)</item>
/// <item>Read command operations</item>
/// <item>Update command operations (with success and precondition failure handling)</item>
/// </list>
///
/// By inheriting from CommandProviderTests, this class runs all those tests against the DynamoCommandProvider
/// implementation specifically, using direct factory instantiation rather than dependency injection.
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual DynamoDB table
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a DynamoDB table.")]
[Category("DynamoCommandProvider")]
public class DynamoCommandProviderTests : CommandProviderTests
{
    /// <summary>
    /// The DynamoDB table used for testing.
    /// </summary>
    private Table _table = null!;

    /// <summary>
    /// Sets up the DynamoCommandProvider for testing using the direct factory instantiation approach.
    /// </summary>
    /// <remarks>
    /// This method initializes the command provider that will be tested by all the test methods
    /// inherited from <see cref="CommandProviderTests"/>. It uses direct factory instantiation,
    /// which tests the core functionality without the dependency injection layer.
    ///
    /// The setup process involves:
    /// <list type="number">
    /// <item>Loading configuration from appsettings.json</item>
    /// <item>Retrieving AWS credentials via the default identity resolver</item>
    /// <item>Creating a DynamoDB client for test cleanup</item>
    /// <item>Creating DynamoClientOptions with the necessary parameters</item>
    /// <item>Creating the DynamoCommandProviderFactory</item>
    /// <item>Using the factory to create a specific command provider instance</item>
    /// </list>
    /// </remarks>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Get the region from the configuration.
        // Example: "us-west-2"
        var region = configuration
            .GetSection("DynamoCommandProviders:Region")
            .Value!;

        // Get the table name from the configuration.
        // Example: "test-items"
        var tableName = configuration
            .GetSection("DynamoCommandProviders:Tables:0:TableName")
            .Value!;

        // Create a dynamo client for cleanup.
        var awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        var dynamoClient = new AmazonDynamoDBClient(
            awsCredentials,
            RegionEndpoint.GetBySystemName(region));

        _table = dynamoClient.GetTable(tableName);

        // Create the command provider using direct factory instantiation.
        var dynamoClientOptions = new DynamoClientOptions(
            AWSCredentials: awsCredentials,
            Region: region,
            TableNames: [ tableName ]
        );

        var factory = await DynamoCommandProviderFactory.Create(
            dynamoClientOptions);

        _commandProvider = factory.Create<ITestItem, TestItem>(
            tableName,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }

    /// <summary>
    /// Cleans up the DynamoDB table after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all items from the DynamoDB table
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    ///
    /// The cleanup process involves:
    /// <list type="number">
    /// <item>Creating a scan filter to find all documents in the table</item>
    /// <item>Processing documents in batches until all are retrieved</item>
    /// <item>Deleting each document individually</item>
    /// </list>
    /// </remarks>
    [TearDown]
    public async Task TestCleanup()
    {
        // Create a scan filter to find all documents in the table.
        var scanFilter = new ScanFilter();
        var search = _table.Scan(scanFilter);

        // Process documents in batches until all are retrieved.
        do
        {
            var documents = await search.GetNextSetAsync();
            // Delete each document individually.
            foreach (var document in documents)
            {
                await _table.DeleteItemAsync(document);
            }
        } while (!search.IsDone);
    }
}
