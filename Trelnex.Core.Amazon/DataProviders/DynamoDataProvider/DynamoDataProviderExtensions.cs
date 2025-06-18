using System.Configuration;
using Amazon.Runtime;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.DataProviders;
using Trelnex.Core.Api.Encryption;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Extension methods for configuring DynamoDB data providers.
/// </summary>
/// <remarks>
/// Provides dependency injection integration.
/// </remarks>
public static class DynamoDataProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds DynamoDB data providers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="bootstrapLogger">Logger for initialization.</param>
    /// <param name="configureDataProviders">Action to configure providers.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when the DynamoDataProviders section is missing.</exception>
    /// <exception cref="ArgumentException">When a requested type name has no associated table.</exception>
    /// <exception cref="InvalidOperationException">When attempting to register the same data provider interface twice.</exception>
    /// <remarks>
    /// Configures DynamoDB data providers for specific entity types.
    /// Uses AWS credentials from the registered credential provider to authenticate with DynamoDB.
    /// </remarks>
    public static IServiceCollection AddDynamoDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Retrieve the Amazon identity provider for AWS credentials
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();

        // Get the region and table configurations from the configuration
        var region = configuration.GetSection("Amazon.DynamoDataProviders:Region").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.DynamoDataProviders configuration is not found.");

        var tables = configuration.GetSection("Amazon.DynamoDataProviders:Tables").GetChildren();
        var tableConfigurations = tables
            .Select(section =>
            {
                var tableName = section.GetValue<string>("TableName")
                    ?? throw new ConfigurationErrorsException("The Amazon.DynamoDataProviders configuration is not found.");

                var encryptionService = section
                    .GetSection("Encryption")
                    .CreateEncryptionService();

                return new TableConfiguration(
                    TypeName: section.Key,
                    TableName: tableName,
                    EncryptionService: encryptionService);
            })
            .ToArray();

        // Parse the DynamoDB options from the configuration
        var providerOptions = DynamoDataProviderOptions.Parse(
            region: region,
            tableConfigurations: tableConfigurations);

        // Create DynamoDB client options with authentication
        var dynamoClientOptions = GetDynamoClientOptions(credentialProvider, providerOptions);

        // Create the DynamoDB data provider factory
        var providerFactory = DynamoDataProviderFactory
            .Create(dynamoClientOptions)
            .GetAwaiter()
            .GetResult();

        // Register the factory as the data provider interface
        services.AddDataProviderFactory(providerFactory);

        // Create the data providers and inject them into the service collection
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
        DynamoDataProviderOptions providerOptions)
    {
        // Retrieve AWS credentials and initialize the client options
        var awsCredentials = credentialProvider.GetCredential();

        return new DynamoClientOptions(
            AWSCredentials: awsCredentials,
            Region: providerOptions.Region,
            TableNames: providerOptions.GetTableNames());
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Implementation of <see cref="IDataProviderOptions"/> for configuring DynamoDB providers.
    /// </summary>
    /// <remarks>
    /// Provides type-to-table mapping and data provider registration.
    /// </remarks>
    private class DataProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        DynamoDataProviderFactory providerFactory,
        DynamoDataProviderOptions providerOptions)
        : IDataProviderOptions
    {
        #region Public Methods

        /// <summary>
        /// Registers a data provider for a specific item type with table mapping.
        /// </summary>
        /// <typeparam name="TInterface">Interface type for the items.</typeparam>
        /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
        /// <param name="typeName">Type name to map to a DynamoDB table.</param>
        /// <param name="itemValidator">Optional validator for items.</param>
        /// <param name="commandOperations">Operations allowed for this provider.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="ArgumentException">When no table is configured for the specified type name.</exception>
        /// <exception cref="InvalidOperationException">When a data provider for the interface is already registered.</exception>
        /// <remarks>
        /// Maps a logical entity type with its physical DynamoDB table location.
        /// </remarks>
        public IDataProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // Retrieve the table configuration for the specified item type
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

            // Create the data provider and inject it into the service collection
            var dataProvider = providerFactory.Create<TInterface, TItem>(
                tableName: tableConfiguration.TableName,
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations,
                encryptionService: tableConfiguration.EncryptionService);

            services.AddSingleton(dataProvider);

            object[] args =
            [
                typeof(TInterface), // TInterface,
                typeof(TItem), // TItem,
                providerOptions.Region, // region
                tableConfiguration.TableName // tableName
            ];

            // Log the registration of the data provider
            bootstrapLogger.LogInformation(
                message: "Added DynamoDataProvider<{TInterface:l}, {TItem:l}>: region = '{region:l}', tableName = '{tableName:l}'.",
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
    /// <param name="EncryptionService">Optional encryption service for the table.</param>
    private record TableConfiguration(
        string TypeName,
        string TableName,
        IEncryptionService? EncryptionService);

    #endregion

    #region Provider Options

    /// <summary>
    /// Runtime options for DynamoDB data providers.
    /// </summary>
    /// <param name="region">The AWS region where the DynamoDB tables are located.</param>
    private class DynamoDataProviderOptions(
        string region)
    {
        #region Private Fields

        /// <summary>
        /// The mappings from type names to table configurations.
        /// </summary>
        private readonly Dictionary<string, TableConfiguration> _tableConfigurationsByTypeName = [];

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the AWS region name.
        /// </summary>
        public string Region => region;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the table configuration for a specified type name.
        /// </summary>
        /// <param name="typeName">The type name to look up.</param>
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
        /// <returns>Array of distinct table names sorted alphabetically.</returns>
        public string[] GetTableNames()
        {
            return _tableConfigurationsByTypeName
                .Select(kvp => kvp.Value.TableName)
                .OrderBy(tableName => tableName)
                .ToArray();
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Parses configuration into a validated options object.
        /// </summary>
        /// <param name="region">The AWS region.</param>
        /// <param name="tableConfigurations">Array of table configurations.</param>
        /// <returns>Validated options with type-to-table mappings.</returns>
        /// <exception cref="AggregateException">When configuration contains duplicate type mappings.</exception>
        /// <remarks>
        /// Validates that no type name is mapped to multiple tables.
        /// </remarks>
        internal static DynamoDataProviderOptions Parse(
            string region,
            TableConfiguration[] tableConfigurations)
        {
            var options = new DynamoDataProviderOptions(
                region: region);

            // Group the tables by item type
            var groups = tableConfigurations
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
                options._tableConfigurationsByTypeName[group.Key] = group.Single();
            });

            return options;
        }

        #endregion
    }

    #endregion
}
