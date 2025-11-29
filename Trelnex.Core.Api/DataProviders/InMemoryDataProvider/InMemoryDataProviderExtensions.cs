using System.Collections;
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
    /// <returns>A task that completes when all providers are registered.</returns>
    public static async Task<IServiceCollection> AddInMemoryDataProvidersAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        Action<IDataProviderOptions> configureDataProviders)
    {
        // Create options to capture registrations
        var options = new DataProviderOptions();

        // Execute user configuration to capture registrations
        configureDataProviders(options);

        // Create and register each provider
        foreach (var registration in options)
        {
            await registration.CreateAndRegisterAsync(services, bootstrapLogger);
        }

        return services;
    }

    #endregion

    #region DataProviderOptions

    /// <summary>
    /// Captures registration configurations for in-memory data providers.
    /// </summary>
    private class DataProviderOptions
        : IDataProviderOptions, IEnumerable<IDataProviderRegistration>
    {
        #region Private Fields

        /// <summary>
        /// Collection of provider registrations to process.
        /// </summary>
        private readonly List<IDataProviderRegistration> _registrations = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Captures an in-memory data provider registration for the specified entity type.
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
            // Capture the registration data
            var registration = new DataProviderRegistration<TItem>
            {
                TypeName = typeName,
                ItemValidator = itemValidator,
                CommandOperations = commandOperations
            };

            _registrations.Add(registration);

            return this;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the registrations.
        /// </summary>
        /// <returns>An enumerator for the registrations.</returns>
        public IEnumerator<IDataProviderRegistration> GetEnumerator() => _registrations.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the registrations.
        /// </summary>
        /// <returns>An enumerator for the registrations.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }

    #endregion

    #region DataProviderRegistration

    /// <summary>
    /// Interface for type-erased provider registration storage.
    /// </summary>
    private interface IDataProviderRegistration
    {
        /// <summary>
        /// Creates and registers the data provider with the service collection.
        /// </summary>
        /// <param name="services">Service collection for provider registration.</param>
        /// <param name="logger">Logger for recording registration activities.</param>
        /// <returns>A task that completes when the provider is registered.</returns>
        Task CreateAndRegisterAsync(
            IServiceCollection services,
            ILogger logger);
    }

    /// <summary>
    /// Captures registration data for a single in-memory data provider.
    /// </summary>
    /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
    private record DataProviderRegistration<TItem>
        : IDataProviderRegistration
        where TItem : BaseItem, new()
    {
        /// <summary>
        /// Type name identifier for the entity in storage.
        /// </summary>
        public required string TypeName { get; init; }

        /// <summary>
        /// Optional validator for entity validation.
        /// </summary>
        public IValidator<TItem>? ItemValidator { get; init; }

        /// <summary>
        /// Optional CRUD operations to enable.
        /// </summary>
        public CommandOperations? CommandOperations { get; init; }

        /// <summary>
        /// Creates and registers the in-memory data provider with the service collection.
        /// </summary>
        /// <param name="services">Service collection for provider registration.</param>
        /// <param name="logger">Logger for recording registration activities.</param>
        /// <returns>A task that completes when the provider is registered.</returns>
        public Task CreateAndRegisterAsync(
            IServiceCollection services,
            ILogger logger)
        {
            // Create data provider instance
            var dataProvider = new InMemoryDataProvider<TItem>(
                typeName: TypeName,
                itemValidator: ItemValidator,
                commandOperations: CommandOperations,
                logger: logger);

            // Register provider as singleton in DI container
            services.AddSingleton<IDataProvider<TItem>>(dataProvider);

            // Log successful registration
            object?[] args =
            [
                typeof(TItem),    // TItem
                TypeName,         // typeName
                CommandOperations // commandOperations
            ];

            logger.LogInformation(
                message: "Added InMemoryDataProvider<{TItem:l}>: typeName = '{typeName:l}', commandOperations = '{commandOperations}'.",
                args: args);

            return Task.CompletedTask;
        }
    }

    #endregion
}
