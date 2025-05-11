using System.Data.Common;
using Azure.Core;
using FluentValidation;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// Factory for creating SQL Server command providers.
/// </summary>
/// <remarks>
/// SQL Server-specific implementation of <see cref="DbCommandProviderFactory"/>.
/// Manages SQL connection setup, authentication, and provider creation.
/// </remarks>
internal class SqlCommandProviderFactory : DbCommandProviderFactory
{
    #region Private Fields

    /// <summary>
    /// The client options for SQL Server connection.
    /// </summary>
    private readonly SqlClientOptions _sqlClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCommandProviderFactory"/> class.
    /// </summary>
    /// <param name="dataOptions">The data connection options for SQL Server.</param>
    /// <param name="sqlClientOptions">The client options for SQL Server.</param>
    private SqlCommandProviderFactory(
        DataOptions dataOptions,
        SqlClientOptions sqlClientOptions)
        : base(dataOptions)
    {
        _sqlClientOptions = sqlClientOptions;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="SqlCommandProviderFactory"/>.
    /// </summary>
    /// <param name="serviceConfiguration">Service configuration information.</param>
    /// <param name="sqlClientOptions">SQL Server connection options.</param>
    /// <returns>A fully initialized <see cref="SqlCommandProviderFactory"/> instance.</returns>
    /// <exception cref="CommandException">When the SQL Server connection cannot be established or required tables are missing.</exception>
    /// <remarks>Verifies connectivity and table existence.</remarks>
    public static SqlCommandProviderFactory Create(
        ServiceConfiguration serviceConfiguration,
        SqlClientOptions sqlClientOptions)
    {
        // Build a connection string.
        var connectionStringBuilder = new SqlConnectionStringBuilder()
        {
            ApplicationName = serviceConfiguration.FullName,
            DataSource = sqlClientOptions.DataSource,
            InitialCatalog = sqlClientOptions.InitialCatalog,
            Encrypt = true,
        };

        // Configure the data access layer.
        var dataOptions = new DataOptions().UseSqlServer(connectionStringBuilder.ConnectionString);

        // Instantiate the factory. Authentication via AAD tokens in BeforeConnectionOpened.
        var commandProviderFactory = new SqlCommandProviderFactory(
            dataOptions,
            sqlClientOptions);

        // Verify connectivity and table existence.
        commandProviderFactory.IsHealthyOrThrow();

        return commandProviderFactory;
    }

    #endregion

    #region Protected Methods

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
    /// Sets the access token on the database connection before use.
    /// </summary>
    /// <param name="dbConnection">The database connection to configure.</param>
    /// <remarks>Assigns an access token to the SQL connection for AAD authentication.</remarks>
    protected override void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        // Check if the connection is a SqlConnection.
        if (dbConnection is not SqlConnection sqlConnection) return;

        // Get the access token.
        var tokenCredential = _sqlClientOptions.TokenCredential;
        var tokenRequestContext = new TokenRequestContext([ _sqlClientOptions.Scope ]);
        var accessToken = tokenCredential.GetToken(tokenRequestContext, default).Token;

        // Assign an access token to the SQL connection for AAD authentication.
        sqlConnection.AccessToken = accessToken;
    }

    #endregion

    #region Protected Properties

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override string[] TableNames => _sqlClientOptions.TableNames;

    /// <inheritdoc />
    protected override string VersionQueryString => "SELECT @@VERSION;";

    #endregion
}
