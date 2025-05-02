namespace Trelnex.Core.Data;

/// <summary>
/// Operational status of a command provider factory.
/// </summary>
/// <remarks>
/// Encapsulates health status and diagnostic information.
/// </remarks>
/// <param name="IsHealthy">
/// Whether factory is operational.
/// </param>
/// <param name="Data">
/// Diagnostic key-value pairs.
/// </param>
public record CommandProviderFactoryStatus(
    bool IsHealthy,
    IReadOnlyDictionary<string, object> Data);
