using System.Data.Common;
using System.Net;
using Amazon.RDS.Util;
using FluentValidation;
using LinqToDB;
using Npgsql;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Factory for creating and configuring PostgreSQL data providers.
/// </summary>
/// <remarks>
/// Creates data providers that connect to a PostgreSQL database using AWS IAM authentication.
/// </remarks>
internal class PostgresDataProviderFactory : DbDataProviderFactory
{
    #region Private Fields

    /// <summary>
    /// The options used to configure the PostgreSQL client.
    /// </summary>
    private readonly PostgresClientOptions _postgresClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDataProviderFactory"/> class.
    /// </summary>
    /// <param name="dataOptions">The data options to use.</param>
    /// <param name="postgresClientOptions">The PostgreSQL client options.</param>
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
    /// Creates an instance of the <see cref="PostgresDataProviderFactory"/>.
    /// </summary>
    /// <param name="serviceConfiguration">Service configuration.</param>
    /// <param name="postgresClientOptions">PostgreSQL client options.</param>
    /// <returns>A configured and validated PostgreSQL data provider factory.</returns>
    /// <remarks>
    /// Configures a new factory and performs a health check to verify database connectivity.
    /// </remarks>
    public static async Task<PostgresDataProviderFactory> Create(
        ServiceConfiguration serviceConfiguration,
        PostgresClientOptions postgresClientOptions)
    {
        // Build the connection string.
        var csb = new NpgsqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            Host = postgresClientOptions.Host,
            Port = postgresClientOptions.Port,
            Database = postgresClientOptions.Database,
            Username = postgresClientOptions.DbUser,
            SslMode = SslMode.Require
        };

        // Bootstrap the data options.
        var dataOptions = new DataOptions().UsePostgreSQL(csb.ConnectionString);

        // Build the factory.
        var factory = new PostgresDataProviderFactory(
            dataOptions,
            postgresClientOptions);

        // Get the operational status of the factory.
        var factoryStatus = await factory.GetStatusAsync();

        // Return the factory if it is healthy; otherwise, throw an exception.
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
        // Ensure the connection is an NpgsqlConnection.
        if (dbConnection is not NpgsqlConnection connection) return;

        // Generate the authentication token for the PostgreSQL database.
        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: _postgresClientOptions.AWSCredentials,
            region: _postgresClientOptions.Region,
            hostname: _postgresClientOptions.Host,
            port: _postgresClientOptions.Port,
            dbUser: _postgresClientOptions.DbUser);

        // Update the connection string with the generated password and SSL mode.
        var csb = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
        {
            Password = pwd,
            SslMode = SslMode.Require
        };

        // Set the connection string to the updated connection string.
        connection.ConnectionString = csb.ConnectionString;
    }

    /// <inheritdoc />
    protected override IDataProvider<TInterface> CreateDataProvider<TInterface, TItem>(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        int? eventTimeToLive = null)
    {
        // Create and return a new PostgresDataProvider instance.
        return new PostgresDataProvider<TInterface, TItem>(
            dataOptions: dataOptions,
            typeName: typeName,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventTimeToLive: eventTimeToLive);
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object> GetStatusData()
    {
        // Return a dictionary containing the status data.
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
