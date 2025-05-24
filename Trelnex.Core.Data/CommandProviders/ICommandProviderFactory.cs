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
    /// Asynchronously gets the current operational status of the factory.
    /// </summary>
    /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
    /// <returns>Status information including connectivity and container availability.</returns>
    Task<CommandProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
