namespace Trelnex.Core.Data;

/// <summary>
/// Represents the operational status and health information of a data provider factory.
/// </summary>
/// <remarks>
/// Contains health status indicators and diagnostic data for monitoring and troubleshooting.
/// </remarks>
/// <param name="IsHealthy">
/// Indicates whether the data provider factory is fully operational and ready to serve requests.
/// </param>
/// <param name="Data">
/// Collection of diagnostic information including database connectivity status, container availability,
/// performance metrics, and other relevant operational data as key-value pairs.
/// </param>
public record DataProviderFactoryStatus(
    bool IsHealthy,
    IReadOnlyDictionary<string, object> Data);
