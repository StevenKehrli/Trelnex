namespace Trelnex.Core.Data;

/// <summary>
/// Contextual information about an HTTP request.
/// </summary>
/// <remarks>
/// Provides access to user identity and request details.
/// </remarks>
public interface IRequestContext
{
    /// <summary>
    /// Unique object ID of the authenticated user (can be null).
    /// </summary>
    string? ObjectId { get; }

    /// <summary>
    /// Unique identifier for this request in trace logs (can be null).
    /// </summary>
    string? HttpTraceIdentifier { get; }

    /// <summary>
    /// HTTP request path (can be null).
    /// </summary>
    string? HttpRequestPath { get; }
}
