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
/// Extension methods for configuring DynamoDB command providers.
/// </summary>
/// <remarks>
/// Provides dependency injection integration.
/// </remarks>
public static class DynamoCommandProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds DynamoDB command providers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="bootstrapLogger">Logger for initialization.</param>
    /// <param name="configureCommandProviders">Action to configure providers.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when the DynamoCommandProviders section is missing.</exception>
    /// <exception cref="ArgumentException">When a requested type name has no associated table.</exception>
    /// <exception cref="InvalidOperationException">When attempting to register the same command provider interface twice.</exception>
    /// <remarks>
    /// Configures DynamoDB command providers for specific entity types.
    /// Uses AWS credentials from the registered credential provider to authenticate with DynamoDB.
    /// </remarks>
    public static IServiceCollection AddDynamoCommandProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<ICommandProviderOptions> configureCommandProviders)
    {
        // Retrieve the Amazon identity provider for AWS credentials
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();

        var providerConfiguration = configuration
            .GetSection("DynamoCommandProviders")
            .Get<DynamoCommandProviderConfiguration>()
            ?? throw new ConfigurationErrorsException("The DynamoCommandProviders configuration is not found.");

        // Parse the DynamoDB options from the configuration
        var providerOptions = DynamoCommandProviderOptions.Parse(providerConfiguration);

        // Create DynamoDB client options with authentication
        var dynamoClientOptions = GetDynamoClientOptions(credentialProvider, providerOptions);

        // Create the DynamoDB command provider factory
        var providerFactory = DynamoCommandProviderFactory.Create(
            dynamoClientOptions).GetAwaiter().GetResult();

        // Register the factory as the command provider interface
        services.AddCommandProviderFactory(providerFactory);

        // Create the command providers and inject them into the service collection
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
    /// Creates DynamoDB client options with authentication.
    /// </summary>
    /// <param name="credentialProvider">Provider for AWS credentials.</param>
    /// <param name="providerOptions">Configuration options for DynamoDB.</param>
    /// <returns>Fully configured DynamoDB client options.</returns>
    /// <remarks>
    /// Retrieves AWS credentials and sets connection parameters.
    /// </remarks>
    private static DynamoClientOptions GetDynamoClientOptions(
        ICredentialProvider<AWSCredentials> credentialProvider,
        DynamoCommandProviderOptions providerOptions)
    {
        // Retrieve AWS credentials and initialize the client options
        var awsCredentials = credentialProvider.GetCredential();

        return new DynamoClientOptions(
            AWSCredentials: awsCredentials,
            Region: providerOptions.Region,
            TableNames: providerOptions.GetTableNames());
    }

    #endregion

    #region CommandProviderOptions

    /// <summary>
    /// Implementation of <see cref="ICommandProviderOptions"/> for configuring DynamoDB providers.
    /// </summary>
    /// <remarks>
    /// Provides type-to-table mapping and command provider registration.
    /// </remarks>
    private class CommandProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        DynamoCommandProviderFactory providerFactory,
        DynamoCommandProviderOptions providerOptions)
        : ICommandProviderOptions
    {
        #region Public Methods

        /// <summary>
        /// Registers a command provider for a specific item type with table mapping.
        /// </summary>
        /// <typeparam name="TInterface">Interface type for the items.</typeparam>
        /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
        /// <param name="typeName">Type name to map to a DynamoDB table.</param>
        /// <param name="itemValidator">Optional validator for items.</param>
        /// <param name="commandOperations">Operations allowed for this provider.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="ArgumentException">When no table is configured for the specified type name.</exception>
        /// <exception cref="InvalidOperationException">When a command provider for the interface is already registered.</exception>
        /// <remarks>
        /// Maps a logical entity type with its physical DynamoDB table location.
        /// </remarks>
        public ICommandProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // Retrieve the table name for the specified item type
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

            // Create the command provider and inject it into the service collection
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
                tableName // tableName
            ];

            // Log the registration of the command provider
            bootstrapLogger.LogInformation(
                message: "Added DynamoCommandProvider<{TInterface:l}, {TItem:l}>: region = '{region:l}', tableName = '{tableName:l}'.",
                args: args);

            return this;
        }

        #endregion
    }

    #endregion

    #region Configuration Records

    /// <summary>
    /// Table configuration mapping type names to DynamoDB table names.
    /// </summary>
    /// <param name="TypeName">The type name.</param>
    /// <param name="TableName">The table name in DynamoDB.</param>
    private record TableConfiguration(
        string TypeName,
        string TableName);

    /// <summary>
    /// Configuration properties for DynamoDB command providers.
    /// </summary>
    private record DynamoCommandProviderConfiguration
    {
        /// <summary>
        /// The AWS region where the DynamoDB tables are located.
        /// </summary>
        public required string Region { get; init; }

        /// <summary>
        /// The collection of table mappings by item type.
        /// </summary>
        public required TableConfiguration[] Tables { get; init; }
    }

    #endregion

    #region Provider Options

    /// <summary>
    /// Runtime options for DynamoDB command providers.
    /// </summary>
    /// <param name="region">The AWS region where the DynamoDB tables are located.</param>
    private class DynamoCommandProviderOptions(
        string region)
    {
        #region Private Fields

        /// <summary>
        /// The mappings from type names to table names.
        /// </summary>
        private readonly Dictionary<string, string> _tableNamesByTypeName = [];

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the AWS region name.
        /// </summary>
        public string Region => region;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the table name for a specified type name.
        /// </summary>
        /// <param name="typeName">The type name to look up.</param>
        /// <returns>The table name if found, or <see langword="null"/> if no mapping exists.</returns>
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
        /// <returns>Array of distinct table names sorted alphabetically.</returns>
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
        /// Parses configuration into a validated options object.
        /// </summary>
        /// <param name="providerConfiguration">Raw configuration data.</param>
        /// <returns>Validated options with type-to-table mappings.</returns>
        /// <exception cref="AggregateException">When configuration contains duplicate type mappings.</exception>
        /// <remarks>
        /// Validates that no type name is mapped to multiple tables.
        /// </remarks>
        internal static DynamoCommandProviderOptions Parse(
            DynamoCommandProviderConfiguration providerConfiguration)
        {
            var options = new DynamoCommandProviderOptions(
                region: providerConfiguration.Region);

            // Group the tables by item type
            var groups = providerConfiguration
                .Tables
                .GroupBy(o => o.TypeName)
                .ToArray();

            // Collect any configuration errors
            var exs = new List<ConfigurationErrorsException>();

            // Validate that each type name maps to only one table
            Array.ForEach(groups, group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A Table for TypeName '{group.Key} is specified more than once."));
            });

            // Throw an aggregate exception if there are any configuration errors
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // Populate the type-to-table mapping
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
