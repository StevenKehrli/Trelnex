namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for managing data provider instances and retrieving status information.
/// </summary>
public interface IDataProviderFactory
{
    /// <summary>
    /// Retrieves the current operational status of the data provider factory.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the status retrieval operation.</param>
    /// <returns>A task containing the factory's current status information.</returns>
    Task<DataProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
