namespace Trelnex.Core.Data;

/// <summary>
/// Represents the status of a command provider factory.
/// </summary>
/// <remarks>
/// This record encapsulates information about the operational state of a command provider factory,
/// including its health status and additional diagnostic data.
/// </remarks>
/// <param name="IsHealthy">A value indicating whether the command provider factory is healthy.
/// <see langword="true"/> indicates normal operation; <see langword="false"/> indicates a problem.</param>
/// <param name="Data">Additional key-value pairs with diagnostic information about the command provider factory.</param>
/// <seealso cref="ICommandProviderFactory"/>
public record CommandProviderFactoryStatus(
    bool IsHealthy,
    IReadOnlyDictionary<string, object> Data);
