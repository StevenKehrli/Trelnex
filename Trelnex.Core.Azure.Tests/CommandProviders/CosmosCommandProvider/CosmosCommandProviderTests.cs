using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Tests for the CosmosCommandProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CosmosCommandProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="CommandProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual CosmosDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosCommandProvider")]
public class CosmosCommandProviderTests : CosmosCommandProviderTestBase
{
    /// <summary>
    /// Sets up the CosmosCommandProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Create the command provider using direct factory instantiation.
        var cosmosClientOptions = new CosmosClientOptions(
            TokenCredential: _tokenCredential,
            AccountEndpoint: _endpointUri,
            DatabaseId: _databaseId,
            ContainerIds: [ _containerId ]
        );

        // Create the CosmosCommandProviderFactory.
        var factory = await CosmosCommandProviderFactory.Create(
            cosmosClientOptions);

        // Create the command provider instance.
        _commandProvider = factory.Create<ITestItem, TestItem>(
            _containerId,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }
}
