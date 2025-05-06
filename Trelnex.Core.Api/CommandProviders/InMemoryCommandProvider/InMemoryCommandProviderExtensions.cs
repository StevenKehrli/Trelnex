using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.CommandProviders;

/// <summary>
/// Provides extension methods for registering in-memory command providers for testing and development.
/// </summary>
/// <remarks>
/// In-memory command providers store data in memory rather than in a persistent data store,
/// making them useful for testing, prototyping, and development scenarios where
/// persistence is not required.
/// </remarks>
public static class InMemoryCommandProviderExtensions
{
    /// <summary>
    /// Registers in-memory command providers for data access in the application.
    /// </summary>
    /// <param name="services">The service collection to add providers to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="bootstrapLogger">Logger for recording provider registration details.</param>
    /// <param name="configureCommandProviders">Configuration delegate for specifying which providers to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Creates an in-memory command provider factory</item>
    ///   <item>Registers the factory with the service collection</item>
    ///   <item>Creates command providers for each repository type via the configuration delegate</item>
    ///   <item>Logs the registration of each command provider</item>
    /// </list>
    ///
    /// In-memory command providers should be used primarily for testing or development
    /// environments where data does not need to persist between application restarts.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddInMemoryCommandProviders(
    ///     configuration,
    ///     logger,
    ///     options => {
    ///         options.Add&lt;IUser, User&gt;("user");
    ///         options.Add&lt;IProduct, Product&gt;("product");
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddInMemoryCommandProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<ICommandProviderOptions> configureCommandProviders)
    {
        // Create an in-memory command provider factory
        var providerFactory = InMemoryCommandProviderFactory.Create().Result;

        // Register the factory with the service collection
        services.AddCommandProviderFactory(providerFactory);

        // Create options for configuring command providers
        var commandProviderOptions = new CommandProviderOptions(
            services: services,
            bootstrapLogger: bootstrapLogger,
            providerFactory: providerFactory);

        // Apply the user's configuration
        configureCommandProviders(commandProviderOptions);

        return services;
    }

    /// <summary>
    /// Implementation of command provider options for in-memory providers.
    /// </summary>
    /// <remarks>
    /// This class handles the creation and registration of in-memory command providers
    /// for specific entity types.
    /// </remarks>
    private class CommandProviderOptions : ICommandProviderOptions
    {
        private readonly IServiceCollection _services;
        private readonly ILogger _bootstrapLogger;
        private readonly InMemoryCommandProviderFactory _providerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandProviderOptions"/> class.
        /// </summary>
        /// <param name="services">The service collection to register providers with.</param>
        /// <param name="bootstrapLogger">Logger for recording provider registration details.</param>
        /// <param name="providerFactory">The factory to create command providers.</param>
        internal CommandProviderOptions(
            IServiceCollection services,
            ILogger bootstrapLogger,
            InMemoryCommandProviderFactory providerFactory)
        {
            _services = services;
            _bootstrapLogger = bootstrapLogger;
            _providerFactory = providerFactory;
        }

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
        /// <remarks>
        /// This method:
        /// <list type="number">
        ///   <item>Verifies that no provider for this interface is already registered</item>
        ///   <item>Creates a command provider for the entity type</item>
        ///   <item>Registers the provider with the dependency injection container</item>
        ///   <item>Logs the registration for debugging and tracing</item>
        /// </list>
        /// </remarks>
        public ICommandProviderOptions Add<TInterface, TItem>(
            string typeName,
            IValidator<TItem>? itemValidator = null,
            CommandOperations? commandOperations = null)
            where TInterface : class, IBaseItem
            where TItem : BaseItem, TInterface, new()
        {
            // Check if a provider for this interface is already registered
            if (_services.Any(sd => sd.ServiceType == typeof(ICommandProvider<TInterface>)))
            {
                throw new InvalidOperationException(
                    $"The CommandProvider<{typeof(TInterface).Name}> is already registered.");
            }

            // Create the command provider for this entity type
            var commandProvider = _providerFactory.Create<TInterface, TItem>(
                typeName: typeName,
                validator: itemValidator,
                commandOperations: commandOperations);

            // Register the provider with the DI container
            _services.AddSingleton(commandProvider);

            // Log the registration using literal format to avoid quotes
            object[] args =
            [
                typeof(TInterface), // TInterface
                typeof(TItem), // TItem
            ];

            _bootstrapLogger.LogInformation(
                message: "Added InMemoryCommandProvider<{TInterface:l}, {TItem:l}>.",
                args: args);

            return this;
        }
    }
}
