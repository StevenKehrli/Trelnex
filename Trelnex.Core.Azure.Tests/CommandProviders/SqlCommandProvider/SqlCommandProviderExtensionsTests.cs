using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Azure.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

[Ignore("Requires a SQL server.")]
public class SqlCommandProviderExtensionsTests : CommandProviderTests
{
    private readonly string _scope = "https://database.windows.net/.default";

    private TokenCredential _tokenCredential = null!;
    private string _connectionString = null!;
    private string _tableName = null!;

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

        var bootstrapLogger = services.AddSerilog(
            configuration,
            "Trelnex.Integration.Tests");

        services
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddSqlCommandProviders(
                configuration,
                bootstrapLogger,
                options => options.Add<ITestItem, TestItem>(
                    typeName: "test-item",
                    validator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // get the command provider
        _commandProvider = serviceProvider.GetRequiredService<ICommandProvider<ITestItem>>();
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

    [Test]
    public void SqlCommandProvider_AlreadyRegistered()
    {
        // create the service collection
        var services = new ServiceCollection();

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        var bootstrapLogger = services.AddSerilog(
            configuration,
            "Trelnex.Integration.Tests");

        // add twice
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddSqlCommandProviders(
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
