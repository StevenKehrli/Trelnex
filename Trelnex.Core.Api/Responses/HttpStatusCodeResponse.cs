using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Core.Api.Responses;

public record HttpStatusCodeResponse
{
    [JsonPropertyName("statusCode")]
    [SwaggerSchema("The http status code,", Nullable = false)]
    public required int StatusCode { get; init; }

    [JsonPropertyName("message")]
    [SwaggerSchema("The message that describes the reason for the status code.", Nullable = false)]
    public required string Message { get; init; }

    [JsonPropertyName("errors")]
    [SwaggerSchema("The errors that describe the reason for the status code.")]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
