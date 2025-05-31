using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Api.Tests.CommandProviders;

[Category("InMemoryCommandProvider")]
public class InMemoryCommandProviderTests : CommandProviderTests
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
                FullName = "InMemoryCommandProviderTests",
                DisplayName = "InMemoryCommandProviderTests",
                Version = "0.0.0",
                Description = "InMemoryCommandProviderTests",
            });

        // Add the in-memory command providers
        services.AddInMemoryCommandProviders(
            configuration,
            bootstrapLogger,
            options => options.Add<ITestItem, TestItem>(
                typeName: "test-item",
                validator: TestItem.Validator,
                commandOperations: CommandOperations.All));

        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get the command provider
        _commandProvider = serviceProvider.GetRequiredService<ICommandProvider<ITestItem>>();
        Assert.That(_commandProvider, Is.Not.Null);

        // Use reflection to get the Clear method from the underlying InMemoryCommandProvider
        _clearMethod = _commandProvider
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
        _clearMethod?.Invoke(_commandProvider, null);
    }
}
