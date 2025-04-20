using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Amazon.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

[Ignore("Requires a DynamoDB instance.")]
public class DynamoCommandProviderExtensionsTests : CommandProviderTests
{
    private Table _table = null!;

    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // This method is called once prior to executing any of the tests in the fixture.

        // create the service collection
        var services = new ServiceCollection();

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // create a dynamo client for cleanup
        var credentialProvider = new CredentialProvider();
        var awsCredentials = credentialProvider.GetCredential();

        services.AddCredentialProvider(credentialProvider);

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

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration() {
                FullName = "DynamoCommandProviderExtensionsTests",
                DisplayName = "DynamoCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "DynamoCommandProviderExtensionsTests"
            });

        services
            .AddDynamoCommandProviders(
                configuration,
                bootstrapLogger,
                options => options.Add<ITestItem, TestItem>(
                    typeName: "test-item",
                    validator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // get the command provider
        _commandProvider = serviceProvider.GetRequiredService<ICommandProvider<ITestItem>>();
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

    [Test]
    public void DynamoCommandProvider_AlreadyRegistered()
    {
        // create the service collection
        var services = new ServiceCollection();

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration() {
                FullName = "DynamoCommandProviderExtensionsTests",
                DisplayName = "DynamoCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "DynamoCommandProviderExtensionsTests"
            });

        // add twice
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddDynamoCommandProviders(
                configuration,
                bootstrapLogger,
                options => options
                    .Add<ITestItem, TestItem>(
                        typeName: "test-item",
                        validator: TestItem.Validator,
                        commandOperations: CommandOperations.All)
                    .Add<ITestItem, TestItem>(
                        typeName: "test-item",
                        validator: TestItem.Validator,
                        commandOperations: CommandOperations.All));
        });
    }

    private class CredentialProvider : ICredentialProvider<AWSCredentials>
    {
        private static readonly AWSCredentials _credentials = FallbackCredentialsFactory.GetCredentials();

        public string Name => "Amazon";

        public IAccessTokenProvider GetAccessTokenProvider(
            string scope)
        {
            throw new NotImplementedException();
        }

        public AWSCredentials GetCredential()
        {
            return _credentials;
        }

        public CredentialStatus GetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
