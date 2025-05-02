namespace Trelnex.Core.Data;

/// <summary>
/// Represents the operational status of a command provider factory for health monitoring and diagnostics.
/// </summary>
/// <remarks>
/// <para>
/// This record encapsulates information about the operational state of a command provider factory,
/// including its health status and additional diagnostic data. It is primarily used for health checks
/// and monitoring of data access components in the system.
/// </para>
/// <para>
/// The status can indicate various conditions such as:
/// </para>
/// <list type="bullet">
///   <item><description>Connection status to the underlying data store</description></item>
///   <item><description>Resource availability (connection pools, memory, etc.)</description></item>
///   <item><description>Configuration validation results</description></item>
///   <item><description>Performance metrics and operational statistics</description></item>
/// </list>
/// <para>
/// The <see cref="Data"/> dictionary allows for extensible reporting of provider-specific
/// diagnostic information without changing the status contract. Common data keys might include:
/// </para>
/// <list type="bullet">
///   <item><description>"connectionString": Masked connection string used by the provider</description></item>
///   <item><description>"lastConnection": Timestamp of the last successful connection</description></item>
///   <item><description>"errorDetails": Details about any failure condition</description></item>
///   <item><description>"providerType": The specific implementation type of the provider</description></item>
///   <item><description>"metricName": Provider-specific performance metrics</description></item>
/// </list>
/// </remarks>
/// <param name="IsHealthy">
/// A value indicating whether the command provider factory is in a healthy operational state.
/// <see langword="true"/> indicates normal operation; <see langword="false"/> indicates a problem
/// that may require attention or intervention.
/// </param>
/// <param name="Data">
/// Additional key-value pairs with diagnostic information about the command provider factory's state.
/// This dictionary can contain provider-specific details to help diagnose issues or monitor performance.
/// </param>
/// <seealso cref="ICommandProviderFactory"/>
public record CommandProviderFactoryStatus(
    bool IsHealthy,
    IReadOnlyDictionary<string, object> Data);
