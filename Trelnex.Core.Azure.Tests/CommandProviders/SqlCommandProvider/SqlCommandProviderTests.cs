using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Tests for the SqlCommandProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="SqlCommandProviderTestBase"/> to leverage the shared 
/// test infrastructure and from <see cref="CommandProviderTests"/> (indirectly) to leverage 
/// the extensive test suite defined in that base class.
/// 
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual SQL Server instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a SQL server.")]
[Category("SqlCommandProvider")]
public class SqlCommandProviderTests : SqlCommandProviderTestBase
{
    /// <summary>
    /// Sets up the SqlCommandProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Configure the SQL client options.
        var sqlClientOptions = new SqlClientOptions(
            TokenCredential: _tokenCredential,
            Scope: _scope,
            DataSource: _dataSource,
            InitialCatalog: _initialCatalog,
            TableNames: [ _tableName ]
        );

        // Create the SqlCommandProviderFactory.
        var factory = SqlCommandProviderFactory.Create(
            _serviceConfiguration,
            sqlClientOptions);

        // Create the command provider instance.
        _commandProvider = factory.Create<ITestItem, TestItem>(
            _tableName,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }
}
