using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Azure.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Tests for the extension methods used to register and configure SqlDataProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// Inherits from <see cref="SqlDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live SQL Server instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a SQL server.")]
[Category("SqlDataProvider")]
public class SqlDataProviderExtensionsEventExpirationTests : SqlDataProviderEventTestBase
{
    /// <summary>
    /// Sets up the SqlDataProvider for testing using the dependency injection approach.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Initialize shared resources from configuration
        var configuration = TestSetup();

        services.AddSingleton(_serviceConfiguration);

        // Configure Serilog
        var bootstrapLogger = services.AddSerilog(
            configuration,
            _serviceConfiguration);

        // Add Azure Identity and SqlDataProviders to the service collection.
        services
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddSqlDataProviders(
                configuration,
                bootstrapLogger,
                options => options.Add(
                    typeName: "expiration-test-item",
                    itemValidator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the data provider from the DI container.
        _dataProvider = serviceProvider.GetRequiredService<IDataProvider<TestItem>>();
    }

    [Test]
    [Description("Tests dependency injected SqlDataProvider sets expireAtDateTimeOffset correctly")]
    public async Task SqlDataProviderExtensions_WithExpiration()
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

        // Retrieve the private and optional messages using the helper method.
        using var sqlConnection = GetConnection();
        using var reader = await GetReader(sqlConnection, id, partitionKey, _expirationTableName);

        Assert.That(reader.Read(), Is.True);

        var expireAtDateTimeOffset = created.Item.CreatedDateTimeOffset.AddSeconds(2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader["expireAtDateTimeOffset"], Is.EqualTo(expireAtDateTimeOffset));
        }
    }
}
