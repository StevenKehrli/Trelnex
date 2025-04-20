using System.Text.Json.Serialization;

namespace Trelnex.Core.Api.Tests;

internal record TestResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
