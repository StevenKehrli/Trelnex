using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Azure.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Tests for the extension methods used to register and configure CosmosCommandProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CosmosCommandProviderTestBase"/> to leverage the shared 
/// test infrastructure and from <see cref="CommandProviderTests"/> (indirectly) to leverage 
/// the extensive test suite defined in that base class.
/// 
/// This class focuses on testing the extension methods for DI registration rather than
/// direct factory instantiation. It also adds an additional test for duplicate registration handling.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual CosmosDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosCommandProvider")]
public class CosmosCommandProviderExtensionsTests : CosmosCommandProviderTestBase
{
    /// <summary>
    /// Sets up the CosmosCommandProvider for testing using the dependency injection approach.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Initialize shared resources from configuration
        var configuration = TestSetup();

        services.AddSingleton(_serviceConfiguration);

        var bootstrapLogger = services.AddSerilog(
            configuration,
            _serviceConfiguration);

        // Add Azure Identity and Cosmos Command Providers to the service collection.
        services
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddCosmosCommandProviders(
                configuration,
                bootstrapLogger,
                options => options.Add<ITestItem, TestItem>(
                    typeName: "test-item",
                    validator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the command provider from the DI container.
        _commandProvider = serviceProvider.GetRequiredService<ICommandProvider<ITestItem>>();
    }

    /// <summary>
    /// Tests that registering the same type with the CosmosCommandProvider twice results in an exception.
    /// </summary>
    [Test]
    [Description("Tests that registering the same type with the CosmosCommandProvider twice results in an exception.")]
    public void CosmosCommandProvider_AlreadyRegistered()
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

        // Attempt to register the same type twice, which should throw an InvalidOperationException.
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddCosmosCommandProviders(
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
