using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Amazon.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

[Ignore("Requires a DynamoDB table.")]
public class DynamoCommandProviderTests : CommandProviderTests
{
    private Table _table = null!;

    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // This method is called once prior to executing any of the tests in the fixture.

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // create a dynamo client for cleanup
        var awsCredentials = FallbackCredentialsFactory.GetCredentials();

        var regionName = configuration
            .GetSection("DynamoCommandProviders:RegionName")
            .Value!;

        var tableName = configuration
            .GetSection("DynamoCommandProviders:Tables:0:TableName")
            .Value!;

        var dynamoClient = new AmazonDynamoDBClient(
            awsCredentials,
            RegionEndpoint.GetBySystemName(regionName));

        _table = Table.LoadTable(
            dynamoClient,
            tableName);

        // create the command provider
        var dynamoClientOptions = new DynamoClientOptions(
            AWSCredentials: awsCredentials,
            RegionName: regionName,
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

    [TearDown]
    public async Task TestCleanup()
    {
        // This method is called after each test case is run.

        var scanFilter = new ScanFilter();
        var search = _table.Scan(scanFilter);

        do
        {
            var documents = await search.GetNextSetAsync();
            foreach (var document in documents)
            {
                await _table.DeleteItemAsync(document);
            }
        } while (!search.IsDone);
    }
}
