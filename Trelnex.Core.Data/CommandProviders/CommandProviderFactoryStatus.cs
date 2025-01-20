namespace Trelnex.Core.Data;

/// <summary>
/// Represents the status of a command provider factory
/// </summary>
/// <param name="IsHealthy">A value indicating whether the command provider is healthy.</param>
/// <param name="Data">Additional key-value pairs describing the command provider.</param>
public record CommandProviderFactoryStatus(
    bool IsHealthy,
    IReadOnlyDictionary<string, object> Data);
