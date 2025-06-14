using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Api.Tests.DataProviders;

[Category("InMemoryDataProviderExtensions")]
public class InMemoryDataProviderExtensionsTests
{
    [Test]
    [Description("Tests that adding the InMemoryDataProvider twice throws an exception")]
    public void InMemoryDataProvider_AlreadyRegistered()
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
                FullName = "InMemoryDataProviderExtensionsTests",
                DisplayName = "InMemoryDataProviderExtensionsTests",
                Version = "0.0.0",
                Description = "InMemoryDataProviderExtensionsTests",
            });

        // Verify that adding the InMemoryDataProviders twice throws an InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddInMemoryDataProviders(
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
