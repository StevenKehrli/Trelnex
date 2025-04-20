using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Api.Tests.CommandProviders;

public class InMemoryCommandProviderTests : CommandProviderTests
{
    private MethodInfo? _clearMethod = null!;

    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // This method is called once prior to executing any of the tests in the fixture.

        // create the service collection
        var services = new ServiceCollection();

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration() { Name = "InMemoryCommandProviderTests", Version = "0.0.0" });

        services.AddInMemoryCommandProviders(
            configuration,
            bootstrapLogger,
            options => options.Add<ITestItem, TestItem>(
                typeName: "test-item",
                validator: TestItem.Validator,
                commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // get the command provider
        _commandProvider = serviceProvider.GetRequiredService<ICommandProvider<ITestItem>>();

        // use reflection to get the Clear method from the underlying InMemoryCommandProvider
        _clearMethod = _commandProvider
            .GetType()
            .GetMethod(
                "Clear",
                BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [TearDown]
    public void TestCleanup()
    {
        // This method is called after each test case is run.

        // clear
        _clearMethod?.Invoke(_commandProvider, null);
    }
}
