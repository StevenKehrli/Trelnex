using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.DataProviders;

/// <summary>
/// Provides extension methods for registering in-memory data providers.
/// </summary>
/// <remarks>
/// In-memory data providers are useful for testing and development.
/// </remarks>
public static class InMemoryDataProviderExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers in-memory data providers for data access.
    /// </summary>
    /// <param name="services">The service collection to add providers to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="bootstrapLogger">Logger for recording provider registration details.</param>
    /// <param name="configureDataProviders">Configuration delegate for specifying which providers to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddInMemoryDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Create an in-memory data provider factory.
        var providerFactory = InMemoryDataProviderFactory
            .Create()
            .GetAwaiter()
            .GetResult();

        // Register the factory with the service collection.
        services.AddDataProviderFactory(providerFactory);

        // Create options for configuring data providers.
        var dataProviderOptions = new DataProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory);

        // Apply the user's configuration.
        configureDataProviders(dataProviderOptions);

        return services;
    }

    #endregion

    #region Private Types

    /// <summary>
    /// Implementation of data provider options for in-memory providers.
    /// </summary>
    /// <param name="services">The service collection to register providers with.</param>
    /// <param name="bootstrapLogger">Logger for recording provider registration details.</param>
    /// <param name="providerFactory">The factory to create data providers.</param>
    private class DataProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        InMemoryDataProviderFactory providerFactory)
        : IDataProviderOptions
    {

        /// <summary>
        /// Adds an in-memory data provider for a specific entity type.
        /// </summary>
        /// <typeparam name="TInterface">The entity interface type.</typeparam>
        /// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
        /// <param name="typeName">The type name for the entity in storage.</param>
        /// <param name="itemValidator">Optional validator for entity validation.</param>
        /// <param name="commandOperations">Optional operations to enable (Create, Read, Update, Delete).</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a data provider for the specified interface is already registered.
        /// </exception>
        public IDataProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // Check if a provider for this interface is already registered.
            if (services.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IDataProvider<TInterface>)))
            {
                throw new InvalidOperationException($"The DataProvider<{typeof(TInterface).Name}> is already registered.");
            }

            // Create the data provider for this entity type.
            var dataProvider = providerFactory.Create<TInterface, TItem>(
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations);

            // Register the provider with the DI container.
            services.AddSingleton(dataProvider);

            // Log the registration using literal format to avoid quotes.
            object[] args =
            [
                typeof(TInterface), // TInterface
                typeof(TItem) // TItem
            ];

            bootstrapLogger.LogInformation(
                message: "Added InMemoryDataProvider<{TInterface:l}, {TItem:l}>.",
                args: args);

            return this;
        }
    }

    #endregion
}
