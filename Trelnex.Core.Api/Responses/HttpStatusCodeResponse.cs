using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Core.Api.Responses;

/// <summary>
/// Represents a standardized HTTP response containing status code, message, and optional validation errors.
/// </summary>
/// <remarks>
/// This record provides a consistent structure for API error responses, enabling
/// clients to reliably parse and handle error conditions. It's used primarily by
/// the exception handling middleware to transform exceptions into structured JSON responses.
///
/// The structure follows REST API best practices by including:
/// <list type="bullet">
///   <item>A numeric status code matching the HTTP response status</item>
///   <item>A human-readable message explaining the error</item>
///   <item>Optional structured validation errors for form submissions</item>
/// </list>
/// </remarks>
public record HttpStatusCodeResponse
{
    /// <summary>
    /// Gets or sets the HTTP status code for the response.
    /// </summary>
    /// <value>
    /// An integer matching standard HTTP status codes (e.g., 400, 401, 404, 500).
    /// </value>
    /// <remarks>
    /// This value will match the actual HTTP status code sent in the response headers,
    /// providing consistency between the response body and the protocol-level status.
    /// </remarks>
    [JsonPropertyName("statusCode")]
    [SwaggerSchema("The HTTP status code.", Nullable = false)]
    public required int StatusCode { get; init; }

    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    /// <value>
    /// A descriptive message explaining the error condition.
    /// </value>
    /// <remarks>
    /// This message should be suitable for displaying to end users,
    /// though it may be technical in nature for developer-focused APIs.
    /// It should not contain sensitive information.
    /// </remarks>
    [JsonPropertyName("message")]
    [SwaggerSchema("The message describing the reason for the status code.", Nullable = false)]
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets a dictionary of validation errors by field name.
    /// </summary>
    /// <value>
    /// A dictionary mapping field names to arrays of validation error messages,
    /// or <see langword="null"/> if no validation errors are present.
    /// </value>
    /// <remarks>
    /// This property is typically populated for 400 Bad Request responses
    /// caused by validation failures. Each key represents a field or property name,
    /// and the associated string array contains one or more validation error messages
    /// for that field.
    ///
    /// For example:
    /// <code>
    /// {
    ///   "errors": {
    ///     "username": ["Username is required", "Username must be at least 3 characters"],
    ///     "email": ["Invalid email format"]
    ///   }
    /// }
    /// </code>
    /// </remarks>
    [JsonPropertyName("errors")]
    [SwaggerSchema("A dictionary of field-specific validation errors.")]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
