using System.Data.Common;
using Azure.Core;
using FluentValidation;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// A builder for creating an instance of the <see cref="SqlCommandProvider"/>.
/// </summary>
internal class SqlCommandProviderFactory : DbCommandProviderFactory
{
    private readonly SqlClientOptions _sqlClientOptions;

    private SqlCommandProviderFactory(
        DataOptions dataOptions,
        SqlClientOptions sqlClientOptions)
        : base(dataOptions)
    {
        _sqlClientOptions = sqlClientOptions;
    }

    /// <summary>
    /// Create an instance of the <see cref="SqlCommandProviderFactory"/>.
    /// </summary>
    /// <param name="serviceConfiguration">The <see cref="ServiceConfiguration"/> options.</param>
    /// <param name="sqlClientOptions">The <see cref="SqlClientOptions"/> options.</param>
    /// <returns>The <see cref="SqlCommandProviderFactory"/>.</returns>
    public static SqlCommandProviderFactory Create(
        ServiceConfiguration serviceConfiguration,
        SqlClientOptions sqlClientOptions)
    {
        var csb = new SqlConnectionStringBuilder()
        {
            ApplicationName = serviceConfiguration.FullName,
            DataSource = sqlClientOptions.DataSource,
            InitialCatalog = sqlClientOptions.InitialCatalog,
            Encrypt = true,
        };

        // bootstrap the data options
        var dataOptions = new DataOptions().UseSqlServer(csb.ConnectionString);

        // build the factory
        var factory = new SqlCommandProviderFactory(
            dataOptions,
            sqlClientOptions);

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
        return new SqlCommandProvider<TInterface, TItem>(
            dataOptions,
            typeName,
            validator,
            commandOperations);
    }

    /// <summary>
    /// Set the access token on the connection.
    /// </summary>
    /// <param name="dbConnection">The <see cref="DbConnection"/>.</param>
    protected override void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        if (dbConnection is not SqlConnection connection) return;

        // get the access token
        var tokenCredential = _sqlClientOptions.TokenCredential;
        var tokenRequestContext = new TokenRequestContext([ _sqlClientOptions.Scope ]);
        var accessToken = tokenCredential.GetToken(tokenRequestContext, default).Token;

        connection.AccessToken = accessToken;
    }

    protected override IReadOnlyDictionary<string, object> StatusData
    {
        get
        {
            return new Dictionary<string, object>
            {
                { "dataSource", _sqlClientOptions.DataSource },
                { "initialCatalog", _sqlClientOptions.InitialCatalog },
                { "tableNames", _sqlClientOptions.TableNames },
            };
        }
    }

    /// <inheritdoc/>
    protected override string[] TableNames => _sqlClientOptions.TableNames;

    /// <inheritdoc/>
    protected override string VersionQueryString => "SELECT @@VERSION;";
}
