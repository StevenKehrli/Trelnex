using Semver;

namespace Trelnex.Core.Api.Configuration;

/// <summary>
/// Provides basic service identification and metadata.
/// </summary>
public record ServiceConfiguration
{
    /// <summary>
    /// Gets the fully qualified name of the service.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets the user-friendly display name of the service.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the service version string following SemVer 2.0 format (e.g., "1.0.0").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the service description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the parsed semantic version object.
    /// </summary>
    public SemVersion SemVersion => SemVersion.Parse(Version);
}
