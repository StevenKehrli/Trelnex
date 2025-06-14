namespace Trelnex.Core.Data;

/// <summary>
/// Factory for creating and managing data provider instances.
/// </summary>
/// <remarks>
/// Serves as the central component for data provider lifecycle management and health monitoring.
/// </remarks>
public interface IDataProviderFactory
{
    /// <summary>
    /// Asynchronously retrieves the current operational status of the data provider factory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to abort the status check operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains status information
    /// including database connectivity, container availability, and service health metrics.
    /// </returns>
    Task<DataProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
