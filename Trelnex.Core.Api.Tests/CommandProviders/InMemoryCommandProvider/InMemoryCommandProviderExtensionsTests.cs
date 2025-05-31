using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Api.Tests.CommandProviders;

[Category("InMemoryCommandProviderExtensions")]
public class InMemoryCommandProviderExtensionsTests
{
    [Test]
    [Description("Tests that adding the InMemoryCommandProvider twice throws an exception")]
    public void InMemoryCommandProvider_AlreadyRegistered()
    {
        // Create a new service collection
        var services = new ServiceCollection();

        // Create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration() {
                FullName = "InMemoryCommandProviderExtensionsTests",
                DisplayName = "InMemoryCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "InMemoryCommandProviderExtensionsTests",
            });

        // Verify that adding the InMemoryCommandProviders twice throws an InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddInMemoryCommandProviders(
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
}
