using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Trelnex.Core.Amazon.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

[Ignore("Requires a Postgres server.")]
public class PostgresCommandProviderTests : CommandProviderTests
{
    private string _connectionString = null!;
    private string _tableName = null!;

    [OneTimeSetUp]
    public async Task TestFixtureSetup()
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

        var region = configuration
            .GetSection("PostgresCommandProviders:Region")
            .Value!;
        
        var host = configuration
            .GetSection("PostgresCommandProviders:Host")
            .Value!;

        var port = int.Parse(
            configuration
                .GetSection("PostgresCommandProviders:Port")
                .Value!);
        
        var database = configuration
            .GetSection("PostgresCommandProviders:Database")
            .Value!;

        var dbUser = configuration
            .GetSection("PostgresCommandProviders:DbUser")
            .Value!;

        _tableName = configuration
            .GetSection("PostgresCommandProviders:Tables:0:TableName")
            .Value!;

        // create a postgres client for cleanup
        var awsCredentials = FallbackCredentialsFactory.GetCredentials();

        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: awsCredentials,
            region: regionEndpoint,
            hostname: host,
            port: port,
            dbUser: dbUser);

        // build the connection string
        var csb = new NpgsqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            Host = host,
            Port = port,
            Database = database,
            Username = dbUser,
            Password = pwd,
            SslMode = SslMode.Require
        };

        _connectionString = csb.ConnectionString;

        // create the command provider
        var postgresClientOptions = new PostgresClientOptions(
            AWSCredentials: awsCredentials,
            Region: region,
            Host: host,
            Port: port,
            Database: database,
            DbUser: dbUser,
            TableNames: [ _tableName ]
        );

        var factory = await PostgresCommandProviderFactory.Create(
            serviceConfiguration,
            postgresClientOptions);

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
        using var sqlConnection = new NpgsqlConnection(_connectionString);

        sqlConnection.Open();

        var cmdText = $"DELETE FROM \"{_tableName}-events\"; DELETE FROM \"{_tableName}\";";
        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);

        sqlCommand.ExecuteNonQuery();
    }
}
