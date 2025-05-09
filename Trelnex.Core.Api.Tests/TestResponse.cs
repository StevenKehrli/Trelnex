using System.Text.Json.Serialization;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A simple record used as the response type for testing API endpoints.
/// It contains a single message property that is serialized as "message" in JSON.
/// This record is used to verify the responses from the test API endpoints.
/// </summary>
internal record TestResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
