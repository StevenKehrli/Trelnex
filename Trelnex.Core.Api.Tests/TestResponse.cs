using System.Text.Json.Serialization;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A standardized response record for testing API endpoints across HTTP methods.
///
/// This record serves as a consistent, predictable response format across various test endpoints
/// in the application (GET, POST, PUT, DELETE, PATCH). It contains a single message property
/// that allows each endpoint to return a unique, identifiable value that can be verified in tests.
///
/// The TestResponse is returned by multiple endpoints defined in BaseApiTests.cs that have
/// different HTTP methods but similar response structures. It's used by TestClient1.cs to deserialize
/// responses from these endpoints, and by ClientTests.cs to verify the correct response values
/// are received.
///
/// Usage examples:
/// - In BaseApiTests.cs: Endpoints return TestResponse instances with method-specific messages
/// - In TestClient1.cs: Methods deserialize responses into TestResponse objects
/// - In ClientTests.cs: Tests verify the Message property contains the expected value
/// </summary>
internal record TestResponse
{
    /// <summary>
    /// Gets or initializes the response message.
    /// This property stores a unique identifier string that varies by endpoint,
    /// allowing tests to verify they received the correct response from the intended endpoint.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets or initializes the user role.
    /// This property indicates the role of the user making the request,
    /// which can be useful for testing role-based access control or permissions.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; init; } = null;
}
