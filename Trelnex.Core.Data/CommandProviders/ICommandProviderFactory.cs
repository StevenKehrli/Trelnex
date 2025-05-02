namespace Trelnex.Core.Data;

/// <summary>
/// Defines a factory for creating and managing command providers.
/// </summary>
/// <remarks>
/// The command provider factory is responsible for instantiating and configuring command providers
/// that handle data operations. It maintains information about the status of the command provider system
/// and can be queried to determine if the system is properly initialized and operational.
/// </remarks>
/// <seealso cref="CommandProviderFactoryStatus"/>
public interface ICommandProviderFactory
{
    #region Public Methods

    /// <summary>
    /// Gets the current status of the command provider factory.
    /// </summary>
    /// <returns>The <see cref="CommandProviderFactoryStatus"/> indicating the operational state of the command provider factory.</returns>
    /// <remarks>
    /// This method can be used to check if the command provider factory is properly initialized
    /// and ready to provide command providers for data operations.
    /// </remarks>
    CommandProviderFactoryStatus GetStatus();

    #endregion
}
