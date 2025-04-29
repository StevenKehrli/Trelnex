using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

[Ignore("Requires a SQL server.")]
public class SqlCommandProviderTests : CommandProviderTests
{
    private readonly string _scope = "https://database.windows.net/.default";

    private TokenCredential _tokenCredential = null!;
    private string _connectionString = null!;
    private string _tableName = null!;

    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // This method is called once prior to executing any of the tests in the fixture.

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        var serviceConfiguration = configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()!;

        var dataSource = configuration
            .GetSection("SqlCommandProviders:DataSource")
            .Value!;

        var initialCatalog = configuration
            .GetSection("SqlCommandProviders:InitialCatalog")
            .Value!;

        _tableName = configuration
            .GetSection("SqlCommandProviders:Tables:0:TableName")
            .Value!;

        var scsBuilder = new SqlConnectionStringBuilder()
        {
            DataSource = dataSource,
            InitialCatalog = initialCatalog,
            Encrypt = true,
        };

        _connectionString = scsBuilder.ConnectionString;

        // create the command provider
        _tokenCredential = new DefaultAzureCredential();

        var sqlClientOptions = new SqlClientOptions(
            TokenCredential: _tokenCredential,
            Scope: _scope,
            DataSource: dataSource,
            InitialCatalog: initialCatalog,
            TableNames: [ _tableName ]
        );

        var factory = SqlCommandProviderFactory.Create(
            serviceConfiguration,
            sqlClientOptions);

        _commandProvider = factory.Create<ITestItem, TestItem>(
            _tableName,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }

    [TearDown]
    public void TestCleanup()
    {
        // This method is called after each test has run.
        using var sqlConnection = new SqlConnection(_connectionString);

        var tokenRequestContext = new TokenRequestContext([ _scope ]);
        sqlConnection.AccessToken = _tokenCredential.GetToken(tokenRequestContext, default).Token;

        sqlConnection.Open();

        var cmdText = $"DELETE FROM [{_tableName}-events]; DELETE FROM [{_tableName}];";
        var sqlCommand = new SqlCommand(cmdText, sqlConnection);

        sqlCommand.ExecuteNonQuery();
    }
}
