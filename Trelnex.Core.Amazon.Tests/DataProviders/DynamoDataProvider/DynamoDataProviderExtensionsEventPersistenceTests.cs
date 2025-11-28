using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the extension methods used to register and configure DynamoDataProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// Inherits from <see cref="DynamoDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live DynamoDB instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a DynamoDB table.")]
[Category("DynamoDataProvider")]
public class DynamoDataProviderExtensionsEventPersistenceTests : DynamoDataProviderEventTestBase
{
    /// <summary>
    /// Sets up the DynamoDataProvider for testing using the dependency injection approach.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Initialize shared resources from configuration and load tables
        var configuration = await TestSetupAsync();

        services.AddSingleton(_serviceConfiguration);

        // Create a credential provider for AWS services.
        var credentialProvider = new CredentialProvider();
        var awsCredentials = credentialProvider.GetCredential();

        services.AddCredentialProvider(credentialProvider);

        // Configure Serilog
        var bootstrapLogger = services.AddSerilog(
            configuration,
            _serviceConfiguration);

        // Add DynamoDataProviders to the service collection.
        await services
            .AddDynamoDataProvidersAsync(
                configuration,
                bootstrapLogger,
                options => options.Add(
                    typeName: "test-item",
                    itemValidator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the data provider from the DI container.
        _dataProvider = serviceProvider.GetRequiredService<IDataProvider<TestItem>>();
    }

    [Test]
    [Description("Tests events stored by dependency injected DynamoDataProvider do not expire.")]
    public async Task DynamoDataProvider_WithoutExpiration()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create a command for creating a test item
        using var createCommand = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";
        createCommand.Item.OptionalMessage = "Optional Message #1";

        // Save the command and capture the result
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);

        // Get the event
        var eventId = $"EVENT^{id}^00000001";
        var key = new Dictionary<string, DynamoDBEntry>
        {
            { "partitionKey", partitionKey },
            { "id", eventId }
        };

        var document = await _eventTable.GetItemAsync(key, default);

        Assert.That(document, Is.Not.Null);
        Assert.That(document.ContainsKey("expireAt"), Is.False);
    }
}
