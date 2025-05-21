using System.Data.Common;
using Amazon.RDS.Util;
using FluentValidation;
using LinqToDB;
using Npgsql;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// Factory for creating and configuring PostgreSQL command providers.
/// </summary>
/// <remarks>
/// Creates command providers that connect to a PostgreSQL database using AWS IAM authentication.
/// </remarks>
internal class PostgresCommandProviderFactory : DbCommandProviderFactory
{
    #region Private Fields

    private readonly PostgresClientOptions _postgresClientOptions;

    #endregion

    #region Constructors

    private PostgresCommandProviderFactory(
        DataOptions dataOptions,
        PostgresClientOptions postgresClientOptions)
        : base(dataOptions)
    {
        _postgresClientOptions = postgresClientOptions;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates an instance of the <see cref="PostgresCommandProviderFactory"/>.
    /// </summary>
    /// <param name="serviceConfiguration">Service configuration.</param>
    /// <param name="postgresClientOptions">PostgreSQL client options.</param>
    /// <returns>A configured and validated PostgreSQL command provider factory.</returns>
    /// <remarks>
    /// Configures a new factory and performs a health check to verify database connectivity.
    /// </remarks>
    public static PostgresCommandProviderFactory Create(
        ServiceConfiguration serviceConfiguration,
        PostgresClientOptions postgresClientOptions)
    {
        // build the connection string
        var csb = new NpgsqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            Host = postgresClientOptions.Host,
            Port = postgresClientOptions.Port,
            Database = postgresClientOptions.Database,
            Username = postgresClientOptions.DbUser,
            SslMode = SslMode.Require
        };

        // bootstrap the data options
        var dataOptions = new DataOptions().UsePostgreSQL(csb.ConnectionString);

        // build the factory
        var factory = new PostgresCommandProviderFactory(
            dataOptions,
            postgresClientOptions);

        // assert the factory is healthy
        factory.IsHealthyOrThrow();

        return factory;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Sets the password for the PostgreSQL connection string using AWS IAM authentication.
    /// </summary>
    /// <param name="dbConnection">The database connection to configure.</param>
    /// <remarks>
    /// Generates and sets a fresh IAM authentication token before each connection is opened.
    /// </remarks>
    protected override void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        if (dbConnection is not NpgsqlConnection connection) return;

        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: _postgresClientOptions.AWSCredentials,
            region: _postgresClientOptions.Region,
            hostname: _postgresClientOptions.Host,
            port: _postgresClientOptions.Port,
            dbUser: _postgresClientOptions.DbUser);

        var csb = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
        {
            Password = pwd,
            SslMode = SslMode.Require
        };

        connection.ConnectionString = csb.ConnectionString;
    }

    /// <inheritdoc />
    /// <summary>
    /// Creates a command provider for the specified item type.
    /// </summary>
    /// <typeparam name="TInterface">Interface type for the items.</typeparam>
    /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
    /// <param name="dataOptions">Data access options.</param>
    /// <param name="typeName">Type name to filter items by in the database.</param>
    /// <param name="validator">Optional validator for items of type TItem.</param>
    /// <param name="commandOperations">Operations allowed for this provider.</param>
    /// <returns>A configured <see cref="ICommandProvider{TInterface}"/> instance.</returns>
    protected override ICommandProvider<TInterface> CreateCommandProvider<TInterface, TItem>(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
    {
        return new PostgresCommandProvider<TInterface, TItem>(
            dataOptions,
            typeName,
            validator,
            commandOperations);
    }

    /// <summary>
    /// Gets diagnostic data about the PostgreSQL command provider factory status.
    /// </summary>
    protected override IReadOnlyDictionary<string, object> StatusData
    {
        get
        {
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
    }

    #endregion

    #region Protected Properties

    /// <summary>
    /// Gets the list of table names managed by this factory.
    /// </summary>
    protected override string[] TableNames => _postgresClientOptions.TableNames;

    /// <inheritdoc/>
    /// <summary>
    /// Gets the SQL query used to check database connectivity and version.
    /// </summary>
    protected override string VersionQueryString => "SELECT version();";

    #endregion
}
