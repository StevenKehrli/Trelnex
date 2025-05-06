using Semver;

namespace Trelnex.Core.Api.Configuration;

/// <summary>
/// Provides basic service identification and metadata information.
/// </summary>
/// <remarks>
/// This record is used to store core service identification details typically found
/// in the ServiceConfiguration section of appsettings.json. These details are used
/// for service discovery, monitoring, API documentation, and logging purposes.
/// </remarks>
public record ServiceConfiguration
{
    /// <summary>
    /// Gets the fully qualified name of the service.
    /// </summary>
    /// <value>
    /// The complete, technical name of the service that uniquely identifies it
    /// within the organization's service registry.
    /// </value>
    /// <remarks>
    /// This is typically used for service discovery and internal references.
    /// </remarks>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets the user-friendly display name of the service.
    /// </summary>
    /// <value>
    /// A human-readable name used in UI, documentation, and logs.
    /// </value>
    /// <remarks>
    /// This name appears in API documentation, health check UIs, and monitoring dashboards.
    /// </remarks>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the service version string.
    /// </summary>
    /// <value>
    /// The semantic version of the service following SemVer 2.0 format.
    /// </value>
    /// <remarks>
    /// Should follow semantic versioning format (e.g., "1.0.0").
    /// This is used for API versioning and documentation.
    /// </remarks>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the service description.
    /// </summary>
    /// <value>
    /// A brief description of the service's purpose and functionality.
    /// </value>
    /// <remarks>
    /// Used in API documentation and service registry entries.
    /// </remarks>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the parsed semantic version object.
    /// </summary>
    /// <value>
    /// A <see cref="SemVersion"/> object representing the parsed version string.
    /// </value>
    /// <remarks>
    /// This property automatically parses the <see cref="Version"/> string and
    /// provides access to individual version components (major, minor, patch).
    /// Throws an exception if the version string is not a valid semantic version.
    /// </remarks>
    public SemVersion SemVersion => SemVersion.Parse(Version);
}
