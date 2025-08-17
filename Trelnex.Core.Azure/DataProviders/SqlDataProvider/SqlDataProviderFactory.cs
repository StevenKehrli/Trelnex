using System.Data.Common;
using System.Net;
using Azure.Core;
using FluentValidation;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Factory for creating SQL Server data providers with Azure token authentication.
/// </summary>
internal class SqlDataProviderFactory : DbDataProviderFactory
{
    #region Private Fields

    // Configuration options for SQL Server client connection
    private readonly SqlClientOptions _sqlClientOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new SQL Server data provider factory with connection options.
    /// </summary>
    /// <param name="dataOptions">LinqToDB data connection options.</param>
    /// <param name="sqlClientOptions">SQL Server-specific connection configuration.</param>
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
    /// Creates and validates a SQL Server data provider factory instance.
    /// </summary>
    /// <param name="serviceConfiguration">Service configuration for connection metadata.</param>
    /// <param name="sqlClientOptions">SQL Server connection configuration.</param>
    /// <returns>Validated factory instance ready for creating data providers.</returns>
    /// <exception cref="CommandException">Thrown when database connection fails or is unhealthy.</exception>
    public static async Task<SqlDataProviderFactory> Create(
        ServiceConfiguration serviceConfiguration,
        SqlClientOptions sqlClientOptions)
    {
        // Configure SQL Server connection string with service and database details
        var connectionStringBuilder = new SqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            DataSource = sqlClientOptions.DataSource,
            InitialCatalog = sqlClientOptions.InitialCatalog,
            Encrypt = true,
        };

        // Initialize LinqToDB data options for SQL Server
        var dataOptions = new DataOptions().UseSqlServer(connectionStringBuilder.ConnectionString);

        // Create factory instance with Azure token authentication
        var factory = new SqlDataProviderFactory(
            dataOptions,
            sqlClientOptions);

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

    /// <inheritdoc />
    protected override string VersionQueryString => "SELECT @@VERSION;";

    #endregion

    #region Protected Methods

    /// <inheritdoc />
    protected override IDataProvider<TItem> CreateDataProvider<TItem>(
        string typeName,
        DataOptions dataOptions,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        EventPolicy? eventPolicy = null,
        int? eventTimeToLive = null,
        ILogger? logger = null)
    {
        // Create SQL Server-specific data provider instance
        return new SqlDataProvider<TItem>(
            typeName: typeName,
            dataOptions: dataOptions,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            eventTimeToLive: eventTimeToLive,
            logger: logger);
    }

    /// <inheritdoc />
    protected override void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        // Only process SQL Server connections
        if (dbConnection is not SqlConnection sqlConnection) return;

        // Generate Azure authentication token for SQL Server
        var tokenCredential = _sqlClientOptions.TokenCredential;
        var tokenRequestContext = new TokenRequestContext([_sqlClientOptions.Scope]);
        var accessToken = tokenCredential.GetToken(tokenRequestContext, default).Token;

        // Set access token for Azure AD authentication
        sqlConnection.AccessToken = accessToken;
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object> GetStatusData()
    {
        // Return SQL Server connection configuration for health monitoring
        return new Dictionary<string, object>
        {
            { "dataSource", _sqlClientOptions.DataSource },
            { "initialCatalog", _sqlClientOptions.InitialCatalog },
            { "tableNames", _sqlClientOptions.TableNames },
        };
    }

    #endregion
}
