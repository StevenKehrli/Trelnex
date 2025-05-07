using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.CommandProviders;

/// <summary>
/// Provides extension methods for registering in-memory command providers.
/// </summary>
/// <remarks>
/// In-memory command providers are useful for testing and development.
/// </remarks>
public static class InMemoryCommandProviderExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers in-memory command providers for data access.
    /// </summary>
    /// <param name="services">The service collection to add providers to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="bootstrapLogger">Logger for recording provider registration details.</param>
    /// <param name="configureCommandProviders">Configuration delegate for specifying which providers to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddInMemoryCommandProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<ICommandProviderOptions> configureCommandProviders)
    {
        // Create an in-memory command provider factory.
        var providerFactory = InMemoryCommandProviderFactory.Create().GetAwaiter().GetResult();

        // Register the factory with the service collection.
        services.AddCommandProviderFactory(providerFactory);

        // Create options for configuring command providers.
        var commandProviderOptions = new CommandProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory);

        // Apply the user's configuration.
        configureCommandProviders(commandProviderOptions);

        return services;
    }

    #endregion

    #region Private Types

    /// <summary>
    /// Implementation of command provider options for in-memory providers.
    /// </summary>
    /// <param name="services">The service collection to register providers with.</param>
    /// <param name="bootstrapLogger">Logger for recording provider registration details.</param>
    /// <param name="providerFactory">The factory to create command providers.</param>
    private class CommandProviderOptions(
        IServiceCollection services,
        ILogger bootstrapLogger,
        InMemoryCommandProviderFactory providerFactory)
        : ICommandProviderOptions
    {

        /// <summary>
        /// Adds an in-memory command provider for a specific entity type.
        /// </summary>
        /// <typeparam name="TInterface">The entity interface type.</typeparam>
        /// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
        /// <param name="typeName">The type name for the entity in storage.</param>
        /// <param name="itemValidator">Optional validator for entity validation.</param>
        /// <param name="commandOperations">Optional operations to enable (Create, Read, Update, Delete).</param>
        /// <returns>The options instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a command provider for the specified interface is already registered.
        /// </exception>
        public ICommandProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // Check if a provider for this interface is already registered.
            if (services.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(ICommandProvider<TInterface>)))
            {
                throw new InvalidOperationException($"The CommandProvider<{typeof(TInterface).Name}> is already registered.");
            }

            // Create the command provider for this entity type.
            var commandProvider = providerFactory.Create<TInterface, TItem>(
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations);

            // Register the provider with the DI container.
            services.AddSingleton(commandProvider);

            // Log the registration using literal format to avoid quotes.
            object[] args =
            [
                typeof(TInterface), // TInterface
                typeof(TItem) // TItem
            ];

            bootstrapLogger.LogInformation(
                message: "Added InMemoryCommandProvider<{TInterface:l}, {TItem:l}>.",
                args: args);

            return this;
        }
    }

    #endregion
}
