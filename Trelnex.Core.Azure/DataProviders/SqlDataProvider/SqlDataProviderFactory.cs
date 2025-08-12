using System.Data.Common;
using System.Net;
using Azure.Core;
using FluentValidation;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Factory for creating SQL Server data providers.
/// </summary>
/// <remarks>
/// SQL Server-specific implementation of <see cref="DbDataProviderFactory"/>.
/// Manages SQL connection setup, authentication, and provider creation.
/// </remarks>
internal class SqlDataProviderFactory : DbDataProviderFactory
{
    #region Private Fields

    /// <summary>
    /// The client options for SQL Server connection.
    /// </summary>
    private readonly SqlClientOptions _sqlClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDataProviderFactory"/> class.
    /// </summary>
    /// <param name="dataOptions">The data connection options for SQL Server.</param>
    /// <param name="sqlClientOptions">The client options for SQL Server.</param>
    private SqlDataProviderFactory(
        DataOptions dataOptions,
        SqlClientOptions sqlClientOptions)
        : base(dataOptions, sqlClientOptions.TableNames)
    {
        _sqlClientOptions = sqlClientOptions;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="SqlDataProviderFactory"/>.
    /// </summary>
    /// <param name="serviceConfiguration">Service configuration information.</param>
    /// <param name="sqlClientOptions">SQL Server connection options.</param>
    /// <returns>A fully initialized <see cref="SqlDataProviderFactory"/> instance.</returns>
    /// <exception cref="CommandException">When the SQL Server connection cannot be established or required tables are missing.</exception>
    /// <remarks>Verifies connectivity and table existence.</remarks>
    public static async Task<SqlDataProviderFactory> Create(
        ServiceConfiguration serviceConfiguration,
        SqlClientOptions sqlClientOptions)
    {
        // Build a connection string.
        var connectionStringBuilder = new SqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            DataSource = sqlClientOptions.DataSource,
            InitialCatalog = sqlClientOptions.InitialCatalog,
            Encrypt = true,
        };

        // Configure the data access layer.
        var dataOptions = new DataOptions().UseSqlServer(connectionStringBuilder.ConnectionString);

        // Instantiate the factory. Authentication via AAD tokens in BeforeConnectionOpened.
        var factory = new SqlDataProviderFactory(
            dataOptions,
            sqlClientOptions);

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

    /// <inheritdoc />
    protected override string VersionQueryString => "SELECT @@VERSION;";

    #endregion

    #region Protected Methods

    /// <inheritdoc />
    protected override IDataProvider<TInterface> CreateDataProvider<TInterface, TItem>(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null,
        int? eventTimeToLive = null)
    {
        // Create and return a new SqlDataProvider instance.
        return new SqlDataProvider<TInterface, TItem>(
            dataOptions: dataOptions,
            typeName: typeName,
            validator: validator,
            commandOperations: commandOperations,
            eventTimeToLive: eventTimeToLive);
    }

    /// <inheritdoc />
    protected override void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        // Check if the connection is a SqlConnection.
        if (dbConnection is not SqlConnection sqlConnection) return;

        // Get the access token.
        var tokenCredential = _sqlClientOptions.TokenCredential;
        var tokenRequestContext = new TokenRequestContext([_sqlClientOptions.Scope]);
        var accessToken = tokenCredential.GetToken(tokenRequestContext, default).Token;

        // Assign an access token to the SQL connection for AAD authentication.
        sqlConnection.AccessToken = accessToken;
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object> GetStatusData()
    {
        // Return a dictionary containing the status data.
        return new Dictionary<string, object>
        {
            { "dataSource", _sqlClientOptions.DataSource },
            { "initialCatalog", _sqlClientOptions.InitialCatalog },
            { "tableNames", _sqlClientOptions.TableNames },
        };
    }

    #endregion
}
