using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Encryption;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

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
public abstract class DynamoDataProviderTestBase : DataProviderTests
{
    /// <summary>
    /// The AWS credentials for DynamoDB authentication.
    /// </summary>
    protected AWSCredentials _awsCredentials = null!;

    /// <summary>
    /// The region for AWS services.
    /// </summary>
    /// <example>us-west-2</example>
    protected string _region = null!;

    /// <summary>
    /// The service configuration containing application settings like name, version, and description.
    /// </summary>
    /// <remarks>
    /// This configuration is loaded from the ServiceConfiguration section in appsettings.json.
    /// </remarks>
    protected ServiceConfiguration _serviceConfiguration = null!;

    /// <summary>
    /// The block cipher service used for encrypting and decrypting test data.
    /// </summary>
    protected IBlockCipherService _blockCipherService = null!;

    /// <summary>
    /// The DynamoDB table used for item testing.
    /// </summary>
    protected Table _itemTable = null!;

    /// <summary>
    /// The DynamoDB table used for event testing.
    /// </summary>
    protected Table _eventTable = null!;

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
        _region = configuration
            .GetSection("Amazon.DynamoDataProviders:Region")
            .Get<string>()!;

        // Get the item table name from the configuration.
        // Example: "test-items"
        var testItemItemTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:test-item:ItemTableName")
            .Get<string>()!;

        // Get the event table name from the configuration.
        // Example: "test-items-events"
        var testItemEventTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:test-item:EventTableName")
            .Get<string>()!;

        // Get the encrypted item table name from the configuration.
        // Example: "test-items"
        var encryptedTestItemItemTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:encrypted-test-item:ItemTableName")
            .Get<string>()!;

        // Get the encrypted item table name from the configuration.
        // Example: "test-items-events"
        var encryptedTestItemEventTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:encrypted-test-item:EventTableName")
            .Get<string>()!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encryptedTestItemItemTableName, Is.EqualTo(testItemItemTableName));
            Assert.That(encryptedTestItemEventTableName, Is.EqualTo(testItemEventTableName));
        }

        // Create the block cipher service from configuration using the factory pattern.
        // This deserializes the algorithm type and settings, then creates the appropriate service.
        _blockCipherService = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:encrypted-test-item")
            .CreateBlockCipherService()!;

        // Create AWS credentials
        _awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Create DynamoDB client and load tables
        var dynamoClient = new AmazonDynamoDBClient(
            _awsCredentials,
            RegionEndpoint.GetBySystemName(_region));

        _itemTable = await dynamoClient.LoadTableAsync(
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            testItemItemTableName);

        _eventTable = await dynamoClient.LoadTableAsync(
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            testItemEventTableName);

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
