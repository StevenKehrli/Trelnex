using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Api.Tests.DataProviders;

[Category("InMemoryDataProvider")]
public class InMemoryDataProviderTests : DataProviderTests
{
    private MethodInfo? _clearMethod = null!;

    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create a new service collection
        var services = new ServiceCollection();

        // Create the test configuration by adding appsettings.json and appsettings.User.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Add Serilog logging
        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration() {
                FullName = "InMemoryDataProviderTests",
                DisplayName = "InMemoryDataProviderTests",
                Version = "0.0.0",
                Description = "InMemoryDataProviderTests",
            });

        // Add the in-memory data providers
        services.AddInMemoryDataProviders(
            configuration,
            bootstrapLogger,
            options => options.Add(
                typeName: "test-item",
                itemValidator: TestItem.Validator,
                commandOperations: CommandOperations.All));

        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get the data provider
        _dataProvider = serviceProvider.GetRequiredService<IDataProvider<TestItem>>();
        Assert.That(_dataProvider, Is.Not.Null);

        // Use reflection to get the Clear method from the underlying InMemoryDataProvider
        _clearMethod = _dataProvider
            .GetType()
            .GetMethod(
                "Clear",
                BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(_clearMethod, Is.Not.Null);
    }

    [TearDown]
    public void TestCleanup()
    {
        // Clear the in-memory data after each test
        _clearMethod?.Invoke(_dataProvider, null);
    }
}
