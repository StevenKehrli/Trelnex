using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Base class for DynamoDataProvider tests containing shared functionality.
/// </summary>
/// <remarks>
/// This class provides common infrastructure for testing DynamoDB data providers, including:
/// - Shared configuration loading
/// - AWS credential management
/// - DynamoDB client setup
/// - Test cleanup logic
/// </remarks>
public abstract class DynamoDataProviderEventTestBase
{
    /// <summary>
    /// The data provider used for testing.
    /// </summary>
    protected IDataProvider<TestItem> _dataProvider = null!;

    /// <summary>
    /// The DynamoDB table used for testing.
    /// </summary>
    protected Table _eventTable = null!;

    /// <summary>
    /// The DynamoDB item table used for testing.
    /// </summary>
    protected Table _itemTable = null!;

    /// <summary>
    /// The service configuration containing application settings like name, version, and description.
    /// </summary>
    /// <remarks>
    /// This configuration is loaded from the ServiceConfiguration section in appsettings.json.
    /// </remarks>
    protected ServiceConfiguration _serviceConfiguration = null!;

    /// <summary>
    /// Sets up the common test infrastructure for DynamoDB data provider tests.
    /// </summary>
    /// <returns>The loaded configuration.</returns>
    protected async Task<IConfiguration> TestSetupAsync()
    {
        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Get the service configuration from the configuration.
        _serviceConfiguration = configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()!;

        // Get the region from the configuration.
        // Example: "us-west-2"
        var region = configuration
            .GetSection("Amazon.DynamoDataProviders:Region")
            .Get<string>()!;

        // Get the expiration item table name from the configuration.
        // Example: "test-items"
        var expirationTestItemItemTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:expiration-test-item:ItemTableName")
            .Get<string>()!;

        // Get the expiration event table name from the configuration.
        // Example: "test-items-events"
        var expirationTestItemEventTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:expiration-test-item:EventTableName")
            .Get<string>()!;

        // Get the persistence item table name from the configuration.
        // Example: "test-items"
        var testItemItemTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:test-item:ItemTableName")
            .Get<string>();

        // Get the persistence event table name from the configuration.
        // Example: "test-items-events"
        var testItemEventTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:test-item:EventTableName")
            .Get<string>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(testItemItemTableName, Is.EqualTo(expirationTestItemItemTableName));
            Assert.That(testItemEventTableName, Is.EqualTo(expirationTestItemEventTableName));
        }

        // Create AWS credentials
        var awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Create DynamoDB client and load tables
        var dynamoClient = new AmazonDynamoDBClient(
            awsCredentials,
            RegionEndpoint.GetBySystemName(region));

        _itemTable = await dynamoClient.LoadTableAsync(
            NullLogger.Instance,
            expirationTestItemItemTableName);

        _eventTable = await dynamoClient.LoadTableAsync(
            NullLogger.Instance,
            expirationTestItemEventTableName);

        return configuration;
    }

    /// <summary>
    /// Cleans up the DynamoDB table after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all items from the DynamoDB table
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    /// </remarks>
    [TearDown]
    public async Task TestCleanup()
    {
        await TableCleanup(_eventTable);
        await TableCleanup(_itemTable);
    }

    private static async Task TableCleanup(
        Table table)
    {
        // Create a scan filter to find all documents in the table.
        var scanFilter = new ScanFilter();
        var search = table.Scan(scanFilter);

        // Iterate through the results in batches.
        do
        {
            var documents = await search.GetNextSetAsync();

            // Delete each document individually.
            foreach (var document in documents)
            {
                await table.DeleteItemAsync(document);
            }
        } while (search.IsDone is false);
    }
}
