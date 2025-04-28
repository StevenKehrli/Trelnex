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
/// Extension method to add the necessary command providers to the <see cref="IServiceCollection"/>.
/// </summary>
public static class PostgresCommandProvidersExtensions
{
    /// <summary>
    /// Add the necessary command providers as a <see cref="ICommandProvider{TInterface}"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the CommandProvider bootstrap logs.</param>
    /// <param name="configureCommandProviders">The action to configure the command providers.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
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

    /// <summary>
    /// Gets the <see cref="PostgresClientOptions"/> to be used by <see cref="NpgsqlDataSource"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes an <see cref="AccessToken"/> with the necessary <see cref="NpgsqlDataSource"/> scopes.
    /// </para>
    /// </remarks>
    /// <param name="credentialProvider">The <see cref="ICredentialProvider{TokenCredential}"/>.</param>
    /// <param name="providerOptions">The <see cref="PostgresCommandProviderOptions"/>.</param>
    /// <returns>A valid <see cref="PostgresClientOptions"/>.</returns>
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

    private class CommandProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        PostgresCommandProviderFactory providerFactory,
        PostgresCommandProviderOptions providerOptions)
        : ICommandProviderOptions
    {
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

    /// <summary>
    /// Represents the table for the specified item type.
    /// </summary>
    /// <param name="TypeName">The specified item type name.</param>
    /// <param name="TableId">The table for the specified item type.</param>
    private record TableConfiguration(
        string TypeName,
        string TableName);

    /// <summary>
    /// Represents the configuration properties for Postgres command providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://github.com/dotnet/runtime/issues/83803
    /// </para>
    /// </remarks>
    private record PostgresCommandProviderConfiguration
    {
        /// <summary>
        /// The AWS region of the Postgres Server.
        /// </summary>
        public required string Region { get; init; }

        /// <summary>
        /// The name/network address to the Postgres Server.
        /// </summary>
        public required string Host { get; init; }

        /// <summary>
        /// The post number to the Postgres Server.
        /// </summary>
        public required int Port { get; init; } = 5432;

        /// <summary>
        /// The database name to initialize.
        /// </summary>
        public required string Database { get; init; }

        /// <summary>
        /// The database user name to connect with.
        /// </summary>
        public required string DbUser { get; init; }

        /// <summary>
        /// The collection of tables by item type
        /// </summary>
        public required TableConfiguration[] Tables { get; init; }
    }

    /// <summary>
    /// Represents the Postgres command provider options: the collection of tables by item type.
    /// </summary>
    private class PostgresCommandProviderOptions(
        string region,
        string host,
        int port,
        string database,
        string dbUser)
    {
        /// <summary>
        /// The collection of tables by item type.
        /// </summary>
        private readonly Dictionary<string, string> _tableNamesByTypeName = [];

        /// <summary>
        /// Initialize an instance of <see cref="PostgresCommandProviderOptions"/>.
        /// </summary>
        /// <param name="providerConfiguration">The Postgres command providers configuration.</param>
        /// <returns>The <see cref="PostgresCommandProviderOptions"/>.</returns>
        /// <exception cref="AggregateException">Represents one or more configuration errors.</exception>
        public static PostgresCommandProviderOptions Parse(
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

        /// <summary>
        /// The AWS region of the Postgres Server.
        /// </summary>
        public string Region => region;

        /// <summary>
        /// Get the host.
        /// </summary>
        public string Host => host;

        /// <summary>
        /// Get the port.
        /// </summary>
        public int Port => port;

        /// <summary>
        /// Get the database.
        /// </summary>
        public string Database => database;

        /// <summary>
        /// Get the database user.
        /// </summary>
        public string DbUser => dbUser;

        /// <summary>
        /// Get the table for the specified item type.
        /// </summary>
        /// <param name="typeName">The specified item type.</param>
        /// <returns>The table for the specified item type.</returns>
        public string? GetTableName(
            string typeName)
        {
            return _tableNamesByTypeName.TryGetValue(typeName, out var tableName)
                ? tableName
                : null;
        }

        /// <summary>
        /// Get the tables.
        /// </summary>
        /// <returns>The array of tables.</returns>
        public string[] GetTableNames()
        {
            return _tableNamesByTypeName
                .Values
                .OrderBy(tn => tn)
                .ToArray();
        }
    }
}
