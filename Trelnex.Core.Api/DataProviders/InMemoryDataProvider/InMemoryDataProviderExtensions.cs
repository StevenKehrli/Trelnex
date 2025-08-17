using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.DataProviders;

/// <summary>
/// Extension methods for registering in-memory data providers with dependency injection.
/// </summary>
public static class InMemoryDataProviderExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers in-memory data providers with the service collection.
    /// </summary>
    /// <param name="services">Service collection to register providers with.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="bootstrapLogger">Logger for recording registration activities.</param>
    /// <param name="configureDataProviders">Delegate to configure which providers to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddInMemoryDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Create in-memory data provider factory
        var providerFactory = InMemoryDataProviderFactory
            .Create()
            .GetAwaiter()
            .GetResult();

        // Register factory with DI container
        services.AddDataProviderFactory(providerFactory);

        // Create configuration options for data providers
        var dataProviderOptions = new DataProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory);

        // Execute user configuration
        configureDataProviders(dataProviderOptions);

        return services;
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Handles configuration and registration of in-memory data providers.
    /// </summary>
    /// <param name="services">Service collection for provider registration.</param>
    /// <param name="bootstrapLogger">Logger for recording registration activities.</param>
    /// <param name="providerFactory">Factory for creating data provider instances.</param>
    private class DataProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        InMemoryDataProviderFactory providerFactory)
        : IDataProviderOptions
    {
        /// <summary>
        /// Registers an in-memory data provider for the specified entity type.
        /// </summary>
        /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
        /// <param name="typeName">Type name identifier for the entity in storage.</param>
        /// <param name="itemValidator">Optional validator for entity validation.</param>
        /// <param name="commandOperations">Optional CRUD operations to enable.</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a provider for this type is already registered.</exception>
        public IDataProviderOptions Add<TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TItem : BaseItem, new()
        {
            // Prevent duplicate registrations for the same entity type
            if (services.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IDataProvider<TItem>)))
            {
                throw new InvalidOperationException($"The DataProvider<{typeof(TItem).Name}> is already registered.");
            }

            // Create data provider instance using factory
            var dataProvider = providerFactory.Create(
                typeName: typeName,
                itemValidator: itemValidator,
                commandOperations: commandOperations,
                logger: bootstrapLogger);

            // Register provider as singleton in DI container
            services.AddSingleton(dataProvider);

            // Log successful registration
            object[] args =
            [
                typeof(TItem) // TItem
            ];

            bootstrapLogger.LogInformation(
                message: "Added InMemoryDataProvider<{TItem:l}>.",
                args: args);

            return this;
        }
    }

    #endregion
}
