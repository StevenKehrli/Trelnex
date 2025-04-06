using System.Configuration;
using Amazon.Runtime;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// Extension method to add the necessary command providers to the <see cref="IServiceCollection"/>.
/// </summary>
public static class DynamoCommandProvidersExtensions
{
    /// <summary>
    /// Add the necessary command providers as a <see cref="ICommandProvider{TInterface}"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the CommandProvider bootstrap logs.</param>
    /// <param name="configureCommandProviders">The action to configure the command providers.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDynamoCommandProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<ICommandProviderOptions> configureCommandProviders)
    {
        // get the amazon identity provider
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();

        var providerConfiguration = configuration
            .GetSection("DynamoCommandProviders")
            .Get<DynamoCommandProviderConfiguration>()
            ?? throw new ConfigurationErrorsException("The DynamoCommandProviders configuration is not found.");

        // parse the dynamo options
        var providerOptions = DynamoCommandProviderOptions.Parse(providerConfiguration);

        // create our factory
        var dynamoClientOptions = GetDynamoClientOptions(credentialProvider, providerOptions);

        var providerFactory = DynamoCommandProviderFactory.Create(
            dynamoClientOptions).Result;

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
    /// Gets the <see cref="DynamoClientOptions"/> to be used by <see cref="DynamoClient"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes an <see cref="AccessToken"/> with the necessary <see cref="DynamoClient"/> scopes.
    /// </para>
    /// </remarks>
    /// <param name="credentialProvider">The <see cref="ICredentialProvider{TokenCredential}"/>.</param>
    /// <param name="providerOptions">The <see cref="DynamoCommandProviderOptions"/>.</param>
    /// <returns>A valid <see cref="DynamoClientOptions"/>.</returns>
    private static DynamoClientOptions GetDynamoClientOptions(
        ICredentialProvider<AWSCredentials> credentialProvider,
        DynamoCommandProviderOptions providerOptions)
    {
        // get the aws credentials and initialize
        var awsCredentials = credentialProvider.GetCredential();

        return new DynamoClientOptions(
            AWSCredentials: awsCredentials,
            RegionName: providerOptions.RegionName,
            TableNames: providerOptions.GetTableNames());
    }

    private class CommandProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        DynamoCommandProviderFactory providerFactory,
        DynamoCommandProviderOptions providerOptions)
        : ICommandProviderOptions
    {
        public ICommandProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // get the table name for the specified item type
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
                providerOptions.RegionName, // regionName
                tableName // tableName
            ];

            // log - the :l format parameter (l = literal) to avoid the quotes
            bootstrapLogger.LogInformation(
                message: "Added CommandProvider<{TInterface:l}, {TItem:l}>: regionName = '{regionName:l}', tableName = '{tableName:l}'.",
                args: args);

            return this;
        }
    }

    /// <summary>
    /// Represents the table for the specified item type.
    /// </summary>
    /// <param name="TypeName">The specified item type name.</param>
    /// <param name="TableName">The table for the specified item type.</param>
    private record TableConfiguration(
        string TypeName,
        string TableName);

    /// <summary>
    /// Represents the configuration properties for Dynamo command providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://github.com/dotnet/runtime/issues/83803
    /// </para>
    /// </remarks>
    private record DynamoCommandProviderConfiguration
    {
        /// <summary>
        /// The region for the tables.
        /// </summary>
        public required string RegionName { get; init; }

        /// <summary>
        /// The collection of tables by item type
        /// </summary>
        public required TableConfiguration[] Tables { get; init; }
    }

    /// <summary>
    /// Represents the Dynamo command provider options: the collection of tables by item type.
    /// </summary>
    private class DynamoCommandProviderOptions(
        string regionName)
    {
        /// <summary>
        /// The collection of tables by item type.
        /// </summary>
        private readonly Dictionary<string, string> _tableNamesByTypeName = [];

        /// <summary>
        /// Initialize an instance of <see cref="DynamoCommandProviderOptions"/>.
        /// </summary>
        /// <param name="providerConfiguration">The Dynamo command providers configuration.</param>
        /// <returns>The <see cref="DynamoCommandProviderOptions"/>.</returns>
        /// <exception cref="AggregateException">Represents one or more configuration errors.</exception>
        public static DynamoCommandProviderOptions Parse(
            DynamoCommandProviderConfiguration providerConfiguration)
        {
            var options = new DynamoCommandProviderOptions(
                regionName: providerConfiguration.RegionName);

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
        /// Get the region name.
        /// </summary>
        public string RegionName => regionName;

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
        /// Get the table names.
        /// </summary>
        /// <returns>The array of table names.</returns>
        public string[] GetTableNames()
        {
            return _tableNamesByTypeName
                .Values
                .OrderBy(tn => tn)
                .ToArray();
        }
    }
}
