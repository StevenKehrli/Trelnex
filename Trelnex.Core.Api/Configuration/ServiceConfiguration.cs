using Semver;

namespace Trelnex.Core.Api.Configuration;

public record ServiceConfiguration
{
    public required string FullName { get; init; }
    public required string DisplayName { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }

    public SemVersion SemVersion => SemVersion.Parse(Version);
}
