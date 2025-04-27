using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Trelnex.Core.Amazon.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

[Ignore("Requires a Postgres server.")]
public class PostgresCommandProviderExtensionsTests : CommandProviderTests
{
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

        // create a dynamo client for cleanup
        var credentialProvider = new CredentialProvider();
        var awsCredentials = credentialProvider.GetCredential();

        services.AddCredentialProvider(credentialProvider);

        var serviceConfiguration = configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()!;

        services.AddSingleton(serviceConfiguration);

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
            ApplicationName = "PostgresCommandProviderExtensionsTests",
            Host = host,
            Port = port,
            Database = database,
            Username = dbUser,
            Password = pwd,
            SslMode = SslMode.Require
        };

        _connectionString = csb.ConnectionString;

        var bootstrapLogger = services.AddSerilog(
            configuration,
            serviceConfiguration);

        services
            .AddPostgresCommandProviders(
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
        using var sqlConnection = new NpgsqlConnection(_connectionString);

        sqlConnection.Open();

        var cmdText = $"DELETE FROM \"{_tableName}-events\"; DELETE FROM \"{_tableName}\";";
        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);

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
            new ServiceConfiguration() {
                FullName = "SqlCommandProviderExtensionsTests",
                DisplayName = "SqlCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "SqlCommandProviderExtensionsTests",
            });

        // add twice
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddPostgresCommandProviders(
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

    private class CredentialProvider : ICredentialProvider<AWSCredentials>
    {
        private static readonly AWSCredentials _credentials = FallbackCredentialsFactory.GetCredentials();

        public string Name => "Amazon";

        public IAccessTokenProvider GetAccessTokenProvider(
            string scope)
        {
            throw new NotImplementedException();
        }

        public AWSCredentials GetCredential()
        {
            return _credentials;
        }

        public CredentialStatus GetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
