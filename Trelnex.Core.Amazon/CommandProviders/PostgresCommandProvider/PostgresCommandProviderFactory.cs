using System.Data.Common;
using Amazon;
using Amazon.RDS.Util;
using FluentValidation;
using LinqToDB;
using Npgsql;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// A builder for creating an instance of the <see cref="PostgresCommandProvider"/>.
/// </summary>
internal class PostgresCommandProviderFactory : DbCommandProviderFactory
{
    private readonly PostgresClientOptions _postgresClientOptions;

    private PostgresCommandProviderFactory(
        DataOptions dataOptions,
        PostgresClientOptions postgresClientOptions)
        : base(dataOptions)
    {
        _postgresClientOptions = postgresClientOptions;
    }

    /// <summary>
    /// Create an instance of the <see cref="PostgresCommandProviderFactory"/>.
    /// </summary>
    /// <param name="serviceConfiguration">The <see cref="ServiceConfiguration"/> options.</param>
    /// <param name="postgresClientOptions">The <see cref="PostgresClientOptions"/> options.</param>
    /// <returns>The <see cref="PostgresCommandProviderFactory"/>.</returns>
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

    /// <inheritdoc />
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
    /// Set the password for the connection string.
    /// </summary>
    /// <param name="dbConnection">The <see cref="DbConnection"/>.</param>
    protected override void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        if (dbConnection is not NpgsqlConnection connection) return;

        var regionEndpoint = RegionEndpoint.GetBySystemName(_postgresClientOptions.Region);

        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: _postgresClientOptions.AWSCredentials,
            region: regionEndpoint,
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

    /// <inheritdoc/>
    protected override string[] TableNames => _postgresClientOptions.TableNames;

    /// <inheritdoc/>
    protected override string VersionQueryString => "SELECT version();";
}
