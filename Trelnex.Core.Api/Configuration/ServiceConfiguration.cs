namespace Trelnex.Core.Api.Configuration;

internal record ServiceConfiguration
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}
