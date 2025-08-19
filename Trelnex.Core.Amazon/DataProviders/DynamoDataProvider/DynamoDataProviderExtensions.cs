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
/// Extension methods for configuring DynamoDB data providers with dependency injection.
/// </summary>
public static class DynamoDataProvidersExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers DynamoDB data providers with the service collection using configuration.
    /// </summary>
    /// <param name="services">Service collection to register providers with.</param>
    /// <param name="configuration">Application configuration containing DynamoDB settings.</param>
    /// <param name="bootstrapLogger">Logger for recording registration activities.</param>
    /// <param name="configureDataProviders">Delegate to configure which providers to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required configuration sections are missing.</exception>
    /// <exception cref="ArgumentException">Thrown when a type name has no associated table configuration.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to register duplicate providers.</exception>
    public static IServiceCollection AddDynamoDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Get AWS credentials from registered credential provider
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();

        // Extract DynamoDB configuration from application settings
        var region = configuration.GetSection("Amazon.DynamoDataProviders:Region").Get<string>()
            ?? throw new ConfigurationErrorsException("The Amazon.DynamoDataProviders configuration is not valid.");

        var tables = configuration.GetSection("Amazon.DynamoDataProviders:Tables").GetChildren();
        var tableConfigurations = tables
            .Select(section =>
            {
                var itemTableName = section.GetValue<string>("ItemTableName")
                    ?? throw new ConfigurationErrorsException("The Amazon.DynamoDataProviders configuration is not valid.");

                // Default the event table name to {itemTableName}-events
                // If the EventPolicy is Disabled, we do not use it
                var eventTableName = section.GetValue<string>("EventTableName", $"{itemTableName}-events");
                var eventPolicy = section.GetValue("EventPolicy", EventPolicy.AllChanges);
                var eventTimeToLive = section.GetValue<int?>("EventTimeToLive");

                var blockCipherService = section.CreateBlockCipherService();

                return new TableConfiguration(
                    TypeName: section.Key,
                    ItemTableName: itemTableName,
                    EventTableName: eventTableName,
                    EventPolicy: eventPolicy,
                    EventTimeToLive: eventTimeToLive,
                    BlockCipherService: blockCipherService);
            })
            .ToArray();

        // Build provider options from configuration
        var providerOptions = DynamoDataProviderOptions.Parse(
            region: region,
            tableConfigurations: tableConfigurations);

        // Configure DynamoDB client with credentials and region
        var dynamoClientOptions = GetDynamoClientOptions(credentialProvider, providerOptions);

        // Create and register DynamoDB provider factory
        var providerFactory = DynamoDataProviderFactory
            .Create(dynamoClientOptions)
            .GetAwaiter()
            .GetResult();

        services.AddDataProviderFactory(providerFactory);

        // Configure individual data providers using factory
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
    /// Creates DynamoDB client options with AWS credentials and connection settings.
    /// </summary>
    /// <param name="credentialProvider">Provider for AWS credentials.</param>
    /// <param name="providerOptions">DynamoDB configuration options.</param>
    /// <returns>Configured DynamoDB client options.</returns>
    private static DynamoClientOptions GetDynamoClientOptions(
        ICredentialProvider<AWSCredentials> credentialProvider,
        DynamoDataProviderOptions providerOptions)
    {
        // Get AWS credentials and build client configuration
        var awsCredentials = credentialProvider.GetCredential();

        return new DynamoClientOptions(
            AWSCredentials: awsCredentials,
            Region: providerOptions.Region,
            TableNames: providerOptions.GetTableNames());
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Handles registration of DynamoDB data providers with type-to-table mapping.
    /// </summary>
    private class DataProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        DynamoDataProviderFactory providerFactory,
        DynamoDataProviderOptions providerOptions)
        : IDataProviderOptions
    {
        #region Public Methods

        /// <summary>
        /// Registers a DynamoDB data provider for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier that maps to a DynamoDB table.</param>
        /// <param name="itemValidator">Optional validator for entity validation.</param>
        /// <param name="commandOperations">Optional CRUD operations to enable.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when no table is configured for the type name.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a provider for this type is already registered.</exception>
        public IDataProviderOptions Add<TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TItem : BaseItem, new()
        {
            // Look up table configuration for the specified type
            var tableConfiguration = providerOptions.GetTableConfiguration(typeName);

            if (tableConfiguration is null)
            {
                throw new ArgumentException(
                    $"The Table for TypeName '{typeName}' is not found.",
                    nameof(typeName));
            }

            if (services.Any(sd => sd.ServiceType == typeof(IDataProvider<TItem>)))
            {
                throw new InvalidOperationException(
                    $"The DataProvider<{typeof(TItem).Name}> is already registered.");
            }

            // Create data provider instance using factory and table configuration
            var dataProvider = providerFactory.Create(
                typeName: typeName,
                itemTableName: tableConfiguration.ItemTableName,
                eventTableName: tableConfiguration.EventTableName,
                itemValidator: itemValidator,
                commandOperations: commandOperations,
                eventPolicy: tableConfiguration.EventPolicy,
                eventTimeToLive: tableConfiguration.EventTimeToLive,
                blockCipherService: tableConfiguration.BlockCipherService,
                logger: bootstrapLogger);

            services.AddSingleton(dataProvider);

            object[] args =
            [
                typeof(TItem), // TItem,
                providerOptions.Region, // region
                tableConfiguration.ItemTableName, // itemTableName
                tableConfiguration.EventTableName, // eventTableName
            ];

            // Log successful provider registration
            bootstrapLogger.LogInformation(
                message: "Added DynamoDataProvider<{TItem:l}>: region = '{region:l}', itemTableName = '{itemTableName:l}', eventTableName = '{eventTableName:l}'.",
                args: args);

            return this;
        }

        #endregion
    }

    #endregion

    #region Configuration Records

    /// <summary>
    /// Configuration mapping a type name to its DynamoDB table and settings.
    /// </summary>
    /// <param name="TypeName">The logical type name identifier.</param>
    /// <param name="ItemTableName">The physical DynamoDB table name for items.</param>
    /// <param name="EventTableName">The physical DynamoDB table name for events.</param>
    /// <param name="EventPolicy">Event policy for change tracking.</param>
    /// <param name="EventTimeToLive">Optional TTL for events in seconds.</param>
    /// <param name="BlockCipherService">Optional encryption service for the table.</param>
    private record TableConfiguration(
        string TypeName,
        string ItemTableName,
        string EventTableName,
        EventPolicy EventPolicy,
        int? EventTimeToLive,
        IBlockCipherService? BlockCipherService);

    #endregion

    #region Provider Options

    /// <summary>
    /// Configuration options for DynamoDB data providers with type-to-table mappings.
    /// </summary>
    /// <param name="region">AWS region where DynamoDB tables are located.</param>
    private class DynamoDataProviderOptions(
        string region)
    {
        #region Private Fields

        // Dictionary mapping type names to their table configurations
        private readonly Dictionary<string, TableConfiguration> _tableConfigurationsByTypeName = [];

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the AWS region name for DynamoDB operations.
        /// </summary>
        public string Region => region;

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves table configuration for a specified type name.
        /// </summary>
        /// <param name="typeName">Type name to look up.</param>
        /// <returns>Table configuration if found, null otherwise.</returns>
        public TableConfiguration? GetTableConfiguration(
            string typeName)
        {
            return _tableConfigurationsByTypeName.TryGetValue(typeName, out var tableConfiguration)
                ? tableConfiguration
                : null;
        }

        /// <summary>
        /// Gets all configured DynamoDB table names.
        /// </summary>
        /// <returns>Sorted array of unique table names.</returns>
        public string[] GetTableNames()
        {
            var tableNames = new HashSet<string>();

            foreach (var tableConfiguration in _tableConfigurationsByTypeName.Values)
            {
                tableNames.Add(tableConfiguration.ItemTableName);

                if (tableConfiguration.EventPolicy != EventPolicy.Disabled)
                {
                    tableNames.Add(tableConfiguration.EventTableName);
                }
            }

            return tableNames
                .OrderBy(tableName => tableName)
                .ToArray();
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Parses configuration arrays into validated provider options.
        /// </summary>
        /// <param name="region">AWS region for DynamoDB operations.</param>
        /// <param name="tableConfigurations">Array of table configurations to validate.</param>
        /// <returns>Validated provider options with type-to-table mappings.</returns>
        /// <exception cref="AggregateException">Thrown when configuration contains duplicate type mappings.</exception>
        internal static DynamoDataProviderOptions Parse(
            string region,
            TableConfiguration[] tableConfigurations)
        {
            var options = new DynamoDataProviderOptions(
                region: region);

            // Group configurations by type name to detect duplicates
            var groups = tableConfigurations
                .GroupBy(o => o.TypeName)
                .ToArray();

            // Validate configuration for duplicate type mappings
            var exs = new List<ConfigurationErrorsException>();

            Array.ForEach(groups, group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A Table for TypeName '{group.Key} is specified more than once."));
            });

            // Throw aggregate exception if validation errors found
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // Build type-to-table mapping dictionary
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
