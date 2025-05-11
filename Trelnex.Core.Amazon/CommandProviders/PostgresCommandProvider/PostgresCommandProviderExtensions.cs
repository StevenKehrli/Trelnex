using System.Configuration;
using Amazon.Runtime;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// Extension methods for configuring PostgreSQL command providers.
/// </summary>
/// <remarks>
/// Provides dependency injection integration.
/// </remarks>
public static class PostgresCommandProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds PostgreSQL command providers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="bootstrapLogger">Logger for initialization.</param>
    /// <param name="configureCommandProviders">Action to configure providers.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ConfigurationErrorsException">When the PostgresCommandProviders section is missing.</exception>
    /// <exception cref="InvalidOperationException">When the ServiceConfiguration is not registered or when attempting to register the same command provider interface twice.</exception>
    /// <exception cref="ArgumentException">When a requested type name has no associated table.</exception>
    /// <remarks>
    /// Configures PostgreSQL command providers for specific entity types.
    /// Uses AWS credentials from the registered credential provider to authenticate with PostgreSQL through IAM.
    /// </remarks>
    public static IServiceCollection AddPostgresCommandProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<ICommandProviderOptions> configureCommandProviders)
    {
        // get the token credential identity provider
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();

        var providerConfiguration = configuration
            .GetSection("PostgresCommandProviders")
            .Get<PostgresCommandProviderConfiguration>()
            ?? throw new ConfigurationErrorsException("The PostgresCommandProviders configuration is not found.");

        // get the service configuration
        var serviceDescriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // parse the postgres options
        var providerOptions = PostgresCommandProviderOptions.Parse(providerConfiguration);

        // create our factory
        var postgresClientOptions = GetPostgresClientOptions(credentialProvider, providerOptions);

        var providerFactory = PostgresCommandProviderFactory.Create(
            serviceConfiguration,
            postgresClientOptions);

        // inject the factory as the status interface
        services.AddCommandProviderFactory(providerFactory);

        // create the command providers and inject
        var commandProviderOptions = new CommandProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory,
            providerOptions: providerOptions);

        configureCommandProviders(commandProviderOptions);

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
        PostgresCommandProviderOptions providerOptions)
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

    #region CommandProviderOptions

    /// <summary>
    /// Implementation of <see cref="ICommandProviderOptions"/> for configuring PostgreSQL providers.
    /// </summary>
    /// <remarks>
    /// Provides type-to-table mapping and command provider registration.
    /// </remarks>
    private class CommandProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        PostgresCommandProviderFactory providerFactory,
        PostgresCommandProviderOptions providerOptions)
        : ICommandProviderOptions
    {
        /// <summary>
        /// Registers a command provider for a specific item type with table mapping.
        /// </summary>
        /// <typeparam name="TInterface">Interface type for the items.</typeparam>
        /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
        /// <param name="typeName">Type name to map to a PostgreSQL table.</param>
        /// <param name="itemValidator">Optional validator for items.</param>
        /// <param name="commandOperations">Operations allowed for this provider.</param>
        /// <returns>The options instance.</returns>
        /// <exception cref="ArgumentException">When no table is configured for the specified type name.</exception>
        /// <exception cref="InvalidOperationException">When a command provider for the interface is already registered.</exception>
        /// <remarks>
        /// Maps a logical entity type with its physical PostgreSQL table location.
        /// </remarks>
        public ICommandProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // get the table for the specified item type
            var tableName = providerOptions.GetTableName(typeName);

            if (tableName is null)
            {
                throw new ArgumentException(
                    $"The Table for TypeName '{typeName}' is not found.",
                    nameof(typeName));
            }

            if (services.Any(sd => sd.ServiceType == typeof(ICommandProvider<TInterface>)))
            {
                throw new InvalidOperationException(
                    $"The CommandProvider<{typeof(TInterface).Name}> is already registered.");
            }

            // create the command provider and inject
            var commandProvider = providerFactory.Create<TInterface, TItem>(
                tableName: tableName,
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations);

            services.AddSingleton(commandProvider);

            object[] args =
            [
                typeof(TInterface), // TInterface,
                typeof(TItem), // TItem,
                providerOptions.Region, // region
                providerOptions.Host, // host
                providerOptions.Port, // port
                providerOptions.Database, // database
                providerOptions.DbUser, // dbUser
                tableName, // table
            ];

            // log - the :l format parameter (l = literal) to avoid the quotes
            bootstrapLogger.LogInformation(
                message: "Added PostgresCommandProvider<{TInterface:l}, {TItem:l}>: region = '{region:l}', host = '{host:l}', port = '{port:l}', database = '{database:l}', dbUser = '{dbUser:l}', tableName = '{tableName:l}'.",
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
    private record TableConfiguration(
        string TypeName,
        string TableName);

    /// <summary>
    /// Configuration properties for Postgres command providers.
    /// </summary>
    private record PostgresCommandProviderConfiguration
    {
        /// <summary>
        /// The AWS region of the PostgreSQL server.
        /// </summary>
        public required string Region { get; init; }

        /// <summary>
        /// The hostname of the PostgreSQL server.
        /// </summary>
        public required string Host { get; init; }

        /// <summary>
        /// The port number of the PostgreSQL server.
        /// </summary>
        public required int Port { get; init; } = 5432;

        /// <summary>
        /// The name of the PostgreSQL database.
        /// </summary>
        public required string Database { get; init; }

        /// <summary>
        /// The database username.
        /// </summary>
        public required string DbUser { get; init; }

        /// <summary>
        /// The collection of tables mapped to item types.
        /// </summary>
        public required TableConfiguration[] Tables { get; init; }
    }

    #endregion

    #region Provider Options

    /// <summary>
    /// Represents the PostgreSQL command provider options.
    /// </summary>
    private class PostgresCommandProviderOptions(
        string region,
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
        public string Region => region;

        #endregion

        #region Private Fields

        /// <summary>
        /// The collection of tables by item type.
        /// </summary>
        private readonly Dictionary<string, string> _tableNamesByTypeName = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the table name for the specified item type.
        /// </summary>
        /// <param name="typeName">The logical type name.</param>
        /// <returns>The corresponding table name, or <see langword="null"/> if no mapping exists.</returns>
        public string? GetTableName(
            string typeName)
        {
            return _tableNamesByTypeName.TryGetValue(typeName, out var tableName)
                ? tableName
                : null;
        }

        /// <summary>
        /// Gets all configured table names.
        /// </summary>
        /// <returns>An array containing all table names, sorted alphabetically.</returns>
        public string[] GetTableNames()
        {
            return _tableNamesByTypeName
                .Values
                .OrderBy(tn => tn)
                .ToArray();
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Parses configuration settings into a validated <see cref="PostgresCommandProviderOptions"/> instance.
        /// </summary>
        /// <param name="providerConfiguration">The PostgreSQL command providers configuration.</param>
        /// <returns>A configured and validated <see cref="PostgresCommandProviderOptions"/> instance.</returns>
        /// <exception cref="AggregateException">Thrown when one or more configuration errors are detected.</exception>
        /// <remarks>
        /// Validates that each type name is mapped to exactly one table name.
        /// </remarks>
        internal static PostgresCommandProviderOptions Parse(
            PostgresCommandProviderConfiguration providerConfiguration)
        {
            // get the server and database
            var options = new PostgresCommandProviderOptions(
                region: providerConfiguration.Region,
                host: providerConfiguration.Host,
                port: providerConfiguration.Port,
                database: providerConfiguration.Database,
                dbUser: providerConfiguration.DbUser);

            // group the tables by item type
            var groups = providerConfiguration
                .Tables
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
                options._tableNamesByTypeName[group.Key] = group.Single().TableName;
            });

            return options;
        }

        #endregion
    }

    #endregion
}
