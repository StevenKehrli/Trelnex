namespace Trelnex.Core.Data;

/// <summary>
/// Represents the operational status of a data provider factory.
/// </summary>
/// <param name="IsHealthy">Indicates whether the factory is operational and ready to handle requests.</param>
/// <param name="Data">Dictionary containing diagnostic and status information as key-value pairs.</param>
public record DataProviderFactoryStatus(
    bool IsHealthy,
    IReadOnlyDictionary<string, object> Data);
