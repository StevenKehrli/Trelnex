namespace Trelnex.Core.Data;

/// <summary>
/// Factory for creating and managing command providers.
/// </summary>
/// <remarks>
/// Core component of data access infrastructure.
/// </remarks>
public interface ICommandProviderFactory
{
    /// <summary>
    /// Gets current operational status.
    /// </summary>
    /// <returns>Status object with health information.</returns>
    /// <remarks>
    /// Provides snapshot of health and connectivity.
    /// </remarks>
    CommandProviderFactoryStatus GetStatus();
}
