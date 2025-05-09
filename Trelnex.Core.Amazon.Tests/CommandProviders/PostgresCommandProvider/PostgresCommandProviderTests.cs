using Trelnex.Core.Amazon.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

/// <summary>
/// Tests for the PostgresCommandProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="PostgresCommandProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="CommandProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual PostgreSQL server
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a Postgres server.")]
[Category("PostgresCommandProvider")]
public class PostgresCommandProviderTests : PostgresCommandProviderTestBase
{
    /// <summary>
    /// Sets up the PostgresCommandProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Create the PostgresClientOptions.
        var postgresClientOptions = new PostgresClientOptions(
            AWSCredentials: _awsCredentials,
            Region: _region,
            Host: _host,
            Port: _port,
            Database: _database,
            DbUser: _dbUser,
            TableNames: [ _tableName ]
        );

        // Create the PostgresCommandProviderFactory.
        var factory = PostgresCommandProviderFactory.Create(
            _serviceConfiguration,
            postgresClientOptions);

        // Create the command provider instance.
        _commandProvider = factory.Create<ITestItem, TestItem>(
            _tableName,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }
}
