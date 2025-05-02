namespace Trelnex.Core.Data;

/// <summary>
/// Defines a factory for creating, configuring, and managing command providers across the application.
/// </summary>
/// <remarks>
/// <para>
/// The command provider factory is a core component of the data access infrastructure, serving as the
/// central registry and factory for command providers that handle data operations. It's responsible for:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Creating appropriately configured command providers for different entity types
///     </description>
///   </item>
///   <item>
///     <description>
///       Managing connections to the underlying data store(s)
///     </description>
///   </item>
///   <item>
///     <description>
///       Providing health and status information about the data access subsystem
///     </description>
///   </item>
///   <item>
///     <description>
///       Handling initialization and configuration of the data access layer
///     </description>
///   </item>
/// </list>
/// <para>
/// Implementations of this interface typically consume configuration provided through
/// <see cref="ICommandProviderOptions"/> and use that configuration to instantiate appropriate
/// command providers for each entity type. The factory maintains internal state tracking the health
/// and connectivity of the data store, which can be queried through the <see cref="GetStatus"/> method.
/// </para>
/// <para>
/// Common implementations include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <c>DbCommandProviderFactory</c>: Creates database-backed command providers
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>InMemoryCommandProviderFactory</c>: Creates in-memory implementations for testing or lightweight usage
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>CachedCommandProviderFactory</c>: Wraps other factories with caching capabilities
///     </description>
///   </item>
/// </list>
/// </remarks>
/// <seealso cref="CommandProviderFactoryStatus"/>
/// <seealso cref="ICommandProviderOptions"/>
/// <seealso cref="ICommandProvider{TInterface}"/>
public interface ICommandProviderFactory
{
    #region Public Methods

    /// <summary>
    /// Gets the current operational status of the command provider factory and its data store connection.
    /// </summary>
    /// <returns>
    /// A <see cref="CommandProviderFactoryStatus"/> object indicating the operational state
    /// of the command provider factory and its underlying data store connection.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a snapshot of the current health and status of the data access system.
    /// It can be used for health checks, monitoring, and diagnostics to determine if the data
    /// access layer is operational and properly connected to its underlying data store.
    /// </para>
    /// <para>
    /// The status information typically includes:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Overall health indicator (whether the system is operational)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Connection status to the underlying data store
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Resource utilization metrics (e.g., connection pool usage)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Configuration validation results
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Error conditions if the system is not fully operational
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// This information can be integrated with application health check systems and monitoring
    /// dashboards to provide visibility into the data access layer's operational status.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Check if the command provider factory is healthy
    /// var status = commandProviderFactory.GetStatus();
    /// if (!status.IsHealthy)
    /// {
    ///     logger.LogError("Data access layer is unhealthy: {Details}",
    ///         status.Data.ContainsKey("errorDetails") ? status.Data["errorDetails"] : "Unknown error");
    /// }
    /// </code>
    /// </example>
    CommandProviderFactoryStatus GetStatus();

    #endregion
}
