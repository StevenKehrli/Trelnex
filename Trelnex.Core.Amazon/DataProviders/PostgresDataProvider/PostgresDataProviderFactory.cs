using System.Data.Common;
using System.Net;
using Amazon.RDS.Util;
using FluentValidation;
using LinqToDB;
using Microsoft.Extensions.Logging;
using Npgsql;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Factory for creating PostgreSQL data providers with AWS IAM authentication.
/// </summary>
internal class PostgresDataProviderFactory : DbDataProviderFactory
{
    #region Private Fields

    // Configuration options for PostgreSQL client connection
    private readonly PostgresClientOptions _postgresClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new PostgreSQL data provider factory with connection options.
    /// </summary>
    /// <param name="dataOptions">LinqToDB data connection options.</param>
    /// <param name="postgresClientOptions">PostgreSQL-specific connection configuration.</param>
    private PostgresDataProviderFactory(
        DataOptions dataOptions,
        PostgresClientOptions postgresClientOptions)
        : base(dataOptions, postgresClientOptions.TableNames)
    {
        _postgresClientOptions = postgresClientOptions;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates and validates a PostgreSQL data provider factory instance.
    /// </summary>
    /// <param name="serviceConfiguration">Service configuration for connection metadata.</param>
    /// <param name="postgresClientOptions">PostgreSQL connection configuration.</param>
    /// <returns>Validated factory instance ready for creating data providers.</returns>
    /// <exception cref="CommandException">Thrown when database connection fails or is unhealthy.</exception>
    public static async Task<PostgresDataProviderFactory> Create(
        ServiceConfiguration serviceConfiguration,
        PostgresClientOptions postgresClientOptions)
    {
        // Configure PostgreSQL connection string with service and database details
        var csb = new NpgsqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            Host = postgresClientOptions.Host,
            Port = postgresClientOptions.Port,
            Database = postgresClientOptions.Database,
            Username = postgresClientOptions.DbUser,
            SslMode = SslMode.Require
        };

        // Initialize LinqToDB data options for PostgreSQL
        var dataOptions = new DataOptions().UsePostgreSQL(csb.ConnectionString);

        // Create factory instance
        var factory = new PostgresDataProviderFactory(
            dataOptions,
            postgresClientOptions);

        // Verify factory health and database connectivity
        var factoryStatus = await factory.GetStatusAsync();

        // Return factory if healthy, otherwise throw exception with error details
        return factoryStatus.IsHealthy
            ? factory
            : throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                factoryStatus.Data["error"] as string);
    }

    #endregion

    #region Protected Properties

    /// <inheritdoc/>
    protected override string VersionQueryString => "SELECT version();";

    #endregion

    #region Protected Methods

    /// <inheritdoc />
    protected override void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        // Only process Npgsql connections
        if (dbConnection is not NpgsqlConnection connection) return;

        // Generate AWS IAM authentication token for PostgreSQL
        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: _postgresClientOptions.AWSCredentials,
            region: _postgresClientOptions.Region,
            hostname: _postgresClientOptions.Host,
            port: _postgresClientOptions.Port,
            dbUser: _postgresClientOptions.DbUser);

        // Update connection string with generated authentication token
        var csb = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
        {
            Password = pwd,
            SslMode = SslMode.Require
        };

        connection.ConnectionString = csb.ConnectionString;
    }

    /// <inheritdoc />
    protected override IDataProvider<TItem> CreateDataProvider<TItem>(
        string typeName,
        DataOptions dataOptions,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        int? eventTimeToLive = null,
        ILogger? logger = null)
    {
        // Create PostgreSQL-specific data provider instance
        return new PostgresDataProvider<TItem>(
            typeName: typeName,
            dataOptions: dataOptions,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventTimeToLive: eventTimeToLive,
            logger: logger);
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object> GetStatusData()
    {
        // Return PostgreSQL connection configuration for health monitoring
        return new Dictionary<string, object>
        {
            { "region", _postgresClientOptions.Region },
            { "host", _postgresClientOptions.Host },
            { "port", _postgresClientOptions.Port },
            { "database", _postgresClientOptions.Database },
            { "dbUser", _postgresClientOptions.DbUser },
            { "tableNames", _postgresClientOptions.TableNames }
        };
    }

    #endregion
}
