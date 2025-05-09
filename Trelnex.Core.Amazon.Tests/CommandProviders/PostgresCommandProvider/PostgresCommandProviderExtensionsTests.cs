using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
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

/// <summary>
/// Tests for the extension methods used to register and configure PostgresCommandProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CommandProviderTests"/> to leverage the extensive test suite
/// defined in the base class. The base class implements a comprehensive set of tests for command provider
/// functionality including:
/// <list type="bullet">
/// <item>Batch command operations (create, update, delete with success and failure scenarios)</item>
/// <item>Create command operations (with success and conflict handling)</item>
/// <item>Delete command operations (with success and precondition failure handling)</item>
/// <item>Query command operations (with various filters, ordering, paging)</item>
/// <item>Read command operations</item>
/// <item>Update command operations (with success and precondition failure handling)</item>
/// </list>
///
/// By inheriting from CommandProviderTests, this class runs all those tests against the PostgresCommandProvider
/// implementation specifically, focusing on testing the extension methods for DI registration rather than
/// direct factory instantiation. It also adds an additional test for duplicate registration handling.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual PostgreSQL server
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a Postgres server.")]
[Category("PostgresCommandProvider")]
public class PostgresCommandProviderExtensionsTests : CommandProviderTests
{
    /// <summary>
    /// The connection string used to connect to the PostgreSQL server.
    /// </summary>
    private string _connectionString = null!;

    /// <summary>
    /// The name of the table used for testing.
    /// </summary>
    private string _tableName = null!;

    /// <summary>
    /// Sets up the PostgresCommandProvider for testing using the dependency injection approach.
    /// </summary>
    /// <remarks>
    /// This method initializes the command provider that will be tested by all the test methods
    /// inherited from <see cref="CommandProviderTests"/>. It uses the DI extensions to register
    /// the provider, simulating how it would be used in a real application.
    ///
    /// The setup process involves:
    /// <list type="number">
    /// <item>Creating a service collection</item>
    /// <item>Loading configuration from appsettings.json</item>
    /// <item>Setting up AWS credentials via a custom credential provider</item>
    /// <item>Generating an RDS authentication token</item>
    /// <item>Configuring the Postgres connection string</item>
    /// <item>Configuring Serilog</item>
    /// <item>Registering the PostgresCommandProvider with DI extensions</item>
    /// <item>Building the service provider and retrieving the command provider</item>
    /// </list>
    /// </remarks>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Create a credential provider for AWS services.
        var credentialProvider = new CredentialProvider();
        var awsCredentials = credentialProvider.GetCredential();

        services.AddCredentialProvider(credentialProvider);

        // Get the service configuration from the configuration.
        var serviceConfiguration = configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()!;

        services.AddSingleton(serviceConfiguration);

        // Get the region from the configuration.
        // Example: "us-west-2"
        var region = configuration
            .GetSection("PostgresCommandProviders:Region")
            .Value!;

        // Get the host from the configuration.
        // Example: "postgrescommandprovider-tests.us-west-2.rds.amazonaws.com"
        var host = configuration
            .GetSection("PostgresCommandProviders:Host")
            .Value!;

        // Get the port from the configuration.
        // Example: 5432
        var port = int.Parse(
            configuration
                .GetSection("PostgresCommandProviders:Port")
                .Value!);

        // Get the database from the configuration.
        // Example: "trelnex-core-data-tests"
        var database = configuration
            .GetSection("PostgresCommandProviders:Database")
            .Value!;

        // Get the database user from the configuration.
        // Example: "admin"
        var dbUser = configuration
            .GetSection("PostgresCommandProviders:DbUser")
            .Value!;

        // Get the table name from the configuration.
        // Example: "test-items"
        _tableName = configuration
            .GetSection("PostgresCommandProviders:Tables:0:TableName")
            .Value!;

        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        // Generate an authentication token for RDS.
        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: awsCredentials,
            region: regionEndpoint,
            hostname: host,
            port: port,
            dbUser: dbUser);

        // Build the connection string.
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

        // Add Postgres Command Providers to the service collection.
        services
            .AddPostgresCommandProviders(
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
    /// Cleans up the PostgreSQL tables after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all data from the PostgreSQL tables
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    ///
    /// The cleanup process involves:
    /// <list type="number">
    /// <item>Opening a SQL connection using the connection string</item>
    /// <item>Executing DELETE statements for both the main table and its events table</item>
    /// </list>
    /// </remarks>
    [TearDown]
    public void TestCleanup()
    {
        // Establish a SQL connection using the connection string.
        using var sqlConnection = new NpgsqlConnection(_connectionString);

        sqlConnection.Open();

        // Define the SQL command to delete all rows from the main table and its events table.
        var cmdText = $"DELETE FROM \"{_tableName}-events\"; DELETE FROM \"{_tableName}\";";
        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);

        // Execute the SQL command.
        sqlCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Tests that registering the same type with the PostgresCommandProvider twice results in an exception.
    /// </summary>
    /// <remarks>
    /// This test verifies that the registration extension properly detects and prevents duplicate
    /// registrations of the same entity type, which would lead to ambiguous resolution in the DI container.
    /// </remarks>
    [Test]
    [Description("Tests that registering the same type with the PostgresCommandProvider twice results in an exception.")]
    public void SqlCommandProvider_AlreadyRegistered()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration()
            {
                FullName = "SqlCommandProviderExtensionsTests",
                DisplayName = "SqlCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "SqlCommandProviderExtensionsTests",
            });

        // Attempt to register the same type twice, which should throw an InvalidOperationException.
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

    /// <summary>
    /// Custom implementation of ICredentialProvider for providing AWS credentials in tests.
    /// </summary>
    /// <remarks>
    /// This class provides a simple implementation of the ICredentialProvider interface
    /// for use in tests. It uses the default AWS credentials identity resolver to obtain
    /// credentials, and implements only the essential methods needed for these tests.
    /// </remarks>
    private class CredentialProvider : ICredentialProvider<AWSCredentials>
    {
        private static readonly AWSCredentials _credentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        /// <summary>
        /// Gets the name of the credential provider.
        /// </summary>
        public string Name => "Amazon";

        /// <summary>
        /// Gets an access token provider for the specified scope.
        /// </summary>
        /// <param name="scope">The scope for which to get an access token provider.</param>
        /// <returns>An IAccessTokenProvider instance.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented for tests.</exception>
        public IAccessTokenProvider GetAccessTokenProvider(
            string scope)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the AWS credentials.
        /// </summary>
        /// <returns>The AWS credentials.</returns>
        public AWSCredentials GetCredential()
        {
            return _credentials;
        }

        /// <summary>
        /// Gets the status of the credential provider.
        /// </summary>
        /// <returns>The credential status.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented for tests.</exception>
        public CredentialStatus GetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
