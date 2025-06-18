using System.Configuration;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.Runtime;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Extension methods for configuring PostgreSQL data providers.
/// </summary>
/// <remarks>
/// Provides dependency injection integration.
/// </remarks>
public static partial class PostgresDataProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds PostgreSQL data providers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="bootstrapLogger">Logger for initialization.</param>
    /// <param name="configureDataProviders">Action to configure providers.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ConfigurationErrorsException">When the PostgresDataProviders section is missing.</exception>
    /// <exception cref="InvalidOperationException">When the ServiceConfiguration is not registered or when attempting to register the same data provider interface twice.</exception>
    /// <exception cref="ArgumentException">When a requested type name has no associated table.</exception>
    /// <remarks>
    /// Configures PostgreSQL data providers for specific entity types.
    /// Uses AWS credentials from the registered credential provider to authenticate with PostgreSQL through IAM.
    /// </remarks>
    public static IServiceCollection AddPostgresDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // get the token credential identity provider
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();

        // Get the database and table configurations from the configuration
        var host = configuration.GetSection("Amazon.PostgresDataProviders:Host").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not found.");

        var port = configuration.GetSection("Amazon.PostgresDataProviders:Port").Get<int?>()
            ?? 5432;

        var database = configuration.GetSection("Amazon.PostgresDataProviders:Database").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not found.");

        var dbUser = configuration.GetSection("Amazon.PostgresDataProviders:DbUser").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not found.");

        var tables = configuration.GetSection("Amazon.PostgresDataProviders:Tables").GetChildren();
        var tableConfigurations = tables
            .Select(t =>
            {
                var tableName = t.GetValue<string>("TableName")
                    ?? throw new ConfigurationErrorsException("The Amazon.PostgresDataProviders configuration is not found.");

                var encryptionServiceFactory = t
                    .GetSection("Encryption")
                    .Get<EncryptionServiceFactory>(options =>
                    {
                        options.BindNonPublicProperties = true;
                        options.ErrorOnUnknownConfiguration = true;
                    });

                return new TableConfiguration(
                    TypeName: t.Key,
                    TableName: tableName,
                    EncryptionServiceFactory: encryptionServiceFactory);
            })
            .ToArray();

        // get the service configuration
        var serviceDescriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // parse the postgres options
        var providerOptions = PostgresDataProviderOptions.Parse(
            host: host,
            port: port,
            database: database,
            dbUser: dbUser,
            tableConfigurations: tableConfigurations);

        // create our factory
        var postgresClientOptions = GetPostgresClientOptions(credentialProvider, providerOptions);

        var providerFactory = PostgresDataProviderFactory
            .Create(serviceConfiguration, postgresClientOptions)
            .GetAwaiter()
            .GetResult();

        // inject the factory as the status interface
        services.AddDataProviderFactory(providerFactory);

        // create the data providers and inject
        var dataProviderOptions = new DataProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory,
            providerOptions: providerOptions);

        configureDataProviders(dataProviderOptions);

        return services;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates PostgreSQL client options with authentication.
    /// </summary>
    /// <param name="credentialProvider">Provider for AWS credentials.</param>
    /// <param name="providerOptions">Configuration options for PostgreSQL.</param>
    /// <returns>Fully configured PostgreSQL client options.</returns>
    /// <remarks>
    /// Retrieves AWS credentials and sets connection parameters.
    /// </remarks>
    private static PostgresClientOptions GetPostgresClientOptions(
        ICredentialProvider<AWSCredentials> credentialProvider,
        PostgresDataProviderOptions providerOptions)
    {
        // get the aws credentials
        var awsCredentials = credentialProvider.GetCredential();

        return new PostgresClientOptions(
            AWSCredentials: awsCredentials,
            Region: providerOptions.Region,
            Host: providerOptions.Host,
            Port: providerOptions.Port,
            Database: providerOptions.Database,
            DbUser: providerOptions.DbUser,
            TableNames: providerOptions.GetTableNames()
        );
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Implementation of <see cref="IDataProviderOptions"/> for configuring PostgreSQL providers.
    /// </summary>
    /// <remarks>
    /// Provides type-to-table mapping and data provider registration.
    /// </remarks>
    private class DataProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        PostgresDataProviderFactory providerFactory,
        PostgresDataProviderOptions providerOptions)
        : IDataProviderOptions
    {
        /// <summary>
        /// Registers a data provider for a specific item type with table mapping.
        /// </summary>
        /// <typeparam name="TInterface">Interface type for the items.</typeparam>
        /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
        /// <param name="typeName">Type name to map to a PostgreSQL table.</param>
        /// <param name="itemValidator">Optional validator for items.</param>
        /// <param name="commandOperations">Operations allowed for this provider.</param>
        /// <returns>The options instance.</returns>
        /// <exception cref="ArgumentException">When no table is configured for the specified type name.</exception>
        /// <exception cref="InvalidOperationException">When a data provider for the interface is already registered.</exception>
        /// <remarks>
        /// Maps a logical entity type with its physical PostgreSQL table location.
        /// </remarks>
        public IDataProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // get the table configuration for the specified item type
            var tableConfiguration = providerOptions.GetTableConfiguration(typeName);

            if (tableConfiguration is null)
            {
                throw new ArgumentException(
                    $"The Table for TypeName '{typeName}' is not found.",
                    nameof(typeName));
            }

            if (services.Any(sd => sd.ServiceType == typeof(IDataProvider<TInterface>)))
            {
                throw new InvalidOperationException(
                    $"The DataProvider<{typeof(TInterface).Name}> is already registered.");
            }

            // Create the encryption service if encryption was specified
            var encryptionService = tableConfiguration.EncryptionServiceFactory?.GetEncryptionService();

            // create the data provider and inject
            var dataProvider = providerFactory.Create<TInterface, TItem>(
                tableName: tableConfiguration.TableName,
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations,
                encryptionService: encryptionService);

            services.AddSingleton(dataProvider);

            object[] args =
            [
                typeof(TInterface), // TInterface,
                typeof(TItem), // TItem,
                providerOptions.Region, // region
                providerOptions.Host, // host
                providerOptions.Port, // port
                providerOptions.Database, // database
                providerOptions.DbUser, // dbUser
                tableConfiguration.TableName, // table
            ];

            // log - the :l format parameter (l = literal) to avoid the quotes
            bootstrapLogger.LogInformation(
                message: "Added PostgresDataProvider<{TInterface:l}, {TItem:l}>: region = '{region:l}', host = '{host:l}', port = '{port:l}', database = '{database:l}', dbUser = '{dbUser:l}', tableName = '{tableName:l}'.",
                args: args);

            return this;
        }
    }

    #endregion

    #region Configuration Records

    /// <summary>
    /// Table configuration mapping type names to PostgreSQL table names.
    /// </summary>
    /// <param name="TypeName">The type name.</param>
    /// <param name="TableName">The table name in PostgreSQL.</param>
    /// <param name="EncryptionServiceFactory">Optional factory for creating encryption services.</param>
    private record TableConfiguration(
        string TypeName,
        string TableName,
        EncryptionServiceFactory? EncryptionServiceFactory);

    #endregion

    #region Provider Options

    /// <summary>
    /// Represents the PostgreSQL data provider options.
    /// </summary>
    private partial class PostgresDataProviderOptions(
        RegionEndpoint region,
        string host,
        int port,
        string database,
        string dbUser)
    {
        #region Public Properties

        /// <summary>
        /// Gets the database name.
        /// </summary>
        public string Database => database;

        /// <summary>
        /// Gets the database username.
        /// </summary>
        public string DbUser => dbUser;

        /// <summary>
        /// Gets the hostname of the PostgreSQL server.
        /// </summary>
        public string Host => host;

        /// <summary>
        /// Gets the port number of the PostgreSQL server.
        /// </summary>
        public int Port => port;

        /// <summary>
        /// Gets the AWS region of the PostgreSQL server.
        /// </summary>
        public RegionEndpoint Region => region;

        #endregion

        #region Private Fields

        /// <summary>
        /// The collection of table configurations by item type.
        /// </summary>
        private readonly Dictionary<string, TableConfiguration> _tableConfigurationsByTypeName = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the table configuration for the specified item type.
        /// </summary>
        /// <param name="typeName">The logical type name.</param>
        /// <returns>The table configuration if found, or <see langword="null"/> if no mapping exists.</returns>
        public TableConfiguration? GetTableConfiguration(
            string typeName)
        {
            return _tableConfigurationsByTypeName.TryGetValue(typeName, out var tableConfiguration)
                ? tableConfiguration
                : null;
        }

        /// <summary>
        /// Gets all configured table names.
        /// </summary>
        /// <returns>An array containing all table names, sorted alphabetically.</returns>
        public string[] GetTableNames()
        {
            return _tableConfigurationsByTypeName
                .Select(tc => tc.Value.TableName)
                .OrderBy(tableName => tableName)
                .ToArray();
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Parses configuration settings into a validated <see cref="PostgresDataProviderOptions"/> instance.
        /// </summary>
        /// <param name="host">The PostgreSQL server host.</param>
        /// <param name="port">The PostgreSQL server port.</param>
        /// <param name="database">The PostgreSQL database name.</param>
        /// <param name="dbUser">The PostgreSQL database username.</param>
        /// <param name="tableConfigurations">An array of table configurations.</param>
        /// <returns>A configured and validated <see cref="PostgresDataProviderOptions"/> instance.</returns>
        /// <exception cref="AggregateException">Thrown when one or more configuration errors are detected.</exception>
        /// <remarks>
        /// Validates that each type name is mapped to exactly one table name.
        /// </remarks>
        internal static PostgresDataProviderOptions Parse(
            string host,
            int port,
            string database,
            string dbUser,
            TableConfiguration[] tableConfigurations)
        {
            // Apply regex pattern matching to extract components.
            var match = HostRegex().Match(host);
            if (match.Success is false)
            {
                throw new ConfigurationErrorsException($"The Host '{host}' is not valid. It should be in the format '<instanceName>.<uniqueId>.<region>.rds.amazonaws.com'.");
            }

            // Get the region from the regex match.
            var regionSystemName = match.Groups["region"].Value;
            var region = RegionEndpoint.GetBySystemName(regionSystemName)
                ?? throw new ConfigurationErrorsException($"The Host '{host}' is not valid. It should be in the format '<instanceName>.<uniqueId>.<region>.rds.amazonaws.com'.");

            // get the server and database
            var options = new PostgresDataProviderOptions(
                region: region,
                host: host,
                port: port,
                database: database,
                dbUser: dbUser);

            // group the tables by item type
            var groups = tableConfigurations
                .GroupBy(o => o.TypeName)
                .ToArray();

            // any exceptions
            var exs = new List<ConfigurationErrorsException>();

            // enumerate each group - should be one
            Array.ForEach(groups, group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A Table for TypeName '{group.Key} is specified more than once."));
            });

            // if there are any exceptions, then throw an aggregate exception of all exceptions
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // enumerate each group and set the table (value) for each item type (key)
            Array.ForEach(groups, group =>
            {
                options._tableConfigurationsByTypeName[group.Key] = group.Single();
            });

            return options;
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Creates a regular expression that parses the host strings.
        /// </summary>
        /// <returns>A <see cref="Regex"/> that matches valid host strings.</returns>
        [GeneratedRegex(@"^(?<instanceName>[^.]+)\.(?<uniqueId>[^.]+)\.(?<region>[a-z]{2}-[a-z]+-\d)\.rds\.amazonaws\.com$")]
        private static partial Regex HostRegex();

        #endregion
    }

    #endregion
}
