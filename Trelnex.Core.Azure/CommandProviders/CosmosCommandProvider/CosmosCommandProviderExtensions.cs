using System.Configuration;
using Azure.Core;
using Azure.Security.KeyVault.Keys.Cryptography;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// Extension method to add the necessary command providers to the <see cref="IServiceCollection"/>.
/// </summary>
public static class CosmosCommandProvidersExtensions
{
    /// <summary>
    /// Add the necessary command providers as a <see cref="ICommandProvider{TInterface}"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the CommandProvider bootstrap logs.</param>
    /// <param name="configureCommandProviders">The action to configure the command providers.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCosmosCommandProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<ICommandProviderOptions> configureCommandProviders)
    {
        // get the azure identity provider
        var credentialProvider = services.GetCredentialProvider<TokenCredential>();

        var providerConfiguration = configuration
            .GetSection("CosmosCommandProviders")
            .Get<CosmosCommandProviderConfiguration>()
            ?? throw new ConfigurationErrorsException("The CosmosCommandProviders configuration is not found.");

        // parse the cosmos options
        var providerOptions = CosmosCommandProviderOptions.Parse(providerConfiguration);

        // create our factory
        var cosmosClientOptions = GetCosmosClientOptions(credentialProvider, providerOptions);
        var keyResolverOptions = GetKeyResolverOptions(credentialProvider, providerOptions);

        var providerFactory = CosmosCommandProviderFactory.Create(
            cosmosClientOptions,
            keyResolverOptions).Result;

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
    /// Gets the <see cref="CosmosClientOptions"/> to be used by <see cref="CosmosClient"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes an <see cref="AccessToken"/> with the necessary <see cref="CosmosClient"/> scopes.
    /// </para>
    /// </remarks>
    /// <param name="credentialProvider">The <see cref="ICredentialProvider{TokenCredential}"/>.</param>
    /// <param name="providerOptions">The <see cref="CosmosCommandProviderOptions"/>.</param>
    /// <returns>A valid <see cref="CosmosClientOptions"/>.</returns>
    private static CosmosClientOptions GetCosmosClientOptions(
        ICredentialProvider<TokenCredential> credentialProvider,
        CosmosCommandProviderOptions providerOptions)
    {
        // get the token credential and initialize
        var tokenCredential = credentialProvider.GetCredential();

        // format the scope
        var uri = new Uri(providerOptions.EndpointUri);

        var scope = new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: ".default",
            extraValue: uri.Query).Uri.ToString();

        var tokenRequestContext = new TokenRequestContext(
            scopes: [scope]);

        // warm-up this token
        tokenCredential.GetToken(tokenRequestContext, default);

        return new CosmosClientOptions(
            AccountEndpoint: providerOptions.EndpointUri,
            TokenCredential: tokenCredential,
            DatabaseId: providerOptions.DatabaseId,
            ContainerIds: providerOptions.GetContainerIds());
    }

    /// <summary>
    /// Gets a <see cref="KeyResolverOptions"/> to be used by <see cref="KeyResolver"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes an <see cref="AccessToken"/> with the necessary <see cref="KeyResolver"/> scopes.
    /// </para>
    /// </remarks>
    /// <param name="credentialProvider">The <see cref="ICredentialProvider{TokenCredential}"/>.</param>
    /// <param name="providerOptions">The <see cref="CosmosCommandProviderOptions"/>.</param>
    /// <returns>A valid <see cref="KeyResolverOptions"/>.</returns>
    private static KeyResolverOptions GetKeyResolverOptions(
        ICredentialProvider<TokenCredential> credentialProvider,
        CosmosCommandProviderOptions providerOptions)
    {
        // get the token credential and initialize
        var tokenCredential = credentialProvider.GetCredential();

        var tokenRequestContext = new TokenRequestContext(
            scopes: ["https://vault.azure.net/.default"],
            tenantId: providerOptions.TenantId);

        // warm-up this token
        tokenCredential.GetToken(tokenRequestContext, default);

        return new KeyResolverOptions(
            TokenCredential: tokenCredential);
    }

    private class CommandProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        CosmosCommandProviderFactory providerFactory,
        CosmosCommandProviderOptions providerOptions)
        : ICommandProviderOptions
    {
        public ICommandProviderOptions Add<TInterface, TItem>(
            string typeName,
            AbstractValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // get the container for the specified item type
            var containerId = providerOptions.GetContainerId(typeName);

            if (containerId is null)
            {
                throw new ArgumentException(
                    $"The Container for TypeName '{typeName}' is not found.",
                    nameof(typeName));
            }

            if (services.Any(sd => sd.ServiceType == typeof(ICommandProvider<TInterface>)))
            {
                throw new InvalidOperationException(
                    $"The CommandProvider<{typeof(TInterface).Name}> is already registered.");
            }

            // create the command provider and inject
            var commandProvider = providerFactory.Create<TInterface, TItem>(
                containerId: containerId,
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations);

            services.AddSingleton(commandProvider);

            object[] args =
            [
                typeof(TInterface), // TInterface,
                typeof(TItem), // TItem,
                providerOptions.EndpointUri, // account
                providerOptions.DatabaseId, // database,
                containerId, // container
            ];

            // log - the :l format parameter (l = literal) to avoid the quotes
            bootstrapLogger.LogInformation(
                message: "Added CommandProvider<{TInterface:l}, {TItem:l}>: endpointUri = '{endpointUri:l}', databaseId = '{databaseId:l}', containerId = '{containerId:l}'.",
                args: args);

            return this;
        }
    }

    /// <summary>
    /// Represents the container for the specified item type.
    /// </summary>
    /// <param name="TypeName">The specified item type name.</param>
    /// <param name="ContainerId">The container for the specified item type.</param>
    private record ContainerConfiguration(
        string TypeName,
        string ContainerId);

    /// <summary>
    /// Represents the configuration properties for Cosmos command providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://github.com/dotnet/runtime/issues/83803
    /// </para>
    /// </remarks>
    private record CosmosCommandProviderConfiguration
    {
        /// <summary>
        /// The id of the Azure tenant (represents the organization).
        /// </summary>
        public required string TenantId { get; init; }

        /// <summary>
        /// The Uri to the Cosmos Account.
        /// </summary>
        public required string EndpointUri { get; init; }

        /// <summary>
        /// The database name to initialize.
        /// </summary>
        public required string DatabaseId { get; init; }

        /// <summary>
        /// The collection of containers by item type
        /// </summary>
        public required ContainerConfiguration[] Containers { get; init; }
    }

    /// <summary>
    /// Represents the Cosmos command provider options: the collection of containers by item type.
    /// </summary>
    private class CosmosCommandProviderOptions(
        string tenantId,
        string endpointUri,
        string databaseId)
    {
        /// <summary>
        /// The collection of containers by item type.
        /// </summary>
        private readonly Dictionary<string, string> _containerIdsByTypeName = [];

        /// <summary>
        /// Initialize an instance of <see cref="CosmosCommandProviderOptions"/>.
        /// </summary>
        /// <param name="providerConfiguration">The cosmos command providers configuration.</param>
        /// <returns>The <see cref="CosmosCommandProviderOptions"/>.</returns>
        /// <exception cref="AggregateException">Represents one or more configuration errors.</exception>
        public static CosmosCommandProviderOptions Parse(
            CosmosCommandProviderConfiguration providerConfiguration)
        {
            // get the tenant, endpoint, and database
            var options = new CosmosCommandProviderOptions(
                tenantId: providerConfiguration.TenantId,
                endpointUri: providerConfiguration.EndpointUri,
                databaseId: providerConfiguration.DatabaseId);

            // group the containers by item type
            var groups = providerConfiguration
                .Containers
                .GroupBy(o => o.TypeName)
                .ToArray();

            // any exceptions
            var exs = new List<ConfigurationErrorsException>();

            // enumerate each group - should be one
            Array.ForEach(groups, group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A Container for TypeName '{group.Key} is specified more than once."));
            });

            // if there are any exceptions, then throw an aggregate exception of all exceptions
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // enumerate each group and set the container (value) for each item type (key)
            Array.ForEach(groups, group =>
            {
                options._containerIdsByTypeName[group.Key] = group.Single().ContainerId;
            });

            return options;
        }

        /// <summary>
        /// Get the database.
        /// </summary>
        public string DatabaseId => databaseId;

        /// <summary>
        /// Get the endpoint.
        /// </summary>
        public string EndpointUri => endpointUri;

        /// <summary>
        /// Get the tenant id.
        /// </summary>
        public string TenantId => tenantId;

        /// <summary>
        /// Get the container for the specified item type.
        /// </summary>
        /// <param name="typeName">The specified item type.</param>
        /// <returns>The container for the specified item type.</returns>
        public string? GetContainerId(
            string typeName)
        {
            return _containerIdsByTypeName.TryGetValue(typeName, out var containerId)
                ? containerId
                : null;
        }

        /// <summary>
        /// Get the containers.
        /// </summary>
        /// <returns>The array of containers.</returns>
        public string[] GetContainerIds()
        {
            return _containerIdsByTypeName
                .Values
                .OrderBy(c => c)
                .ToArray();
        }
    }
}
