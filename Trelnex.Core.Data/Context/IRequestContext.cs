namespace Trelnex.Core.Data;

/// <summary>
/// Defines the contract for a request context.
/// </summary>
public interface IRequestContext
{
    /// <summary>
    /// Gets the unique object ID associated with the ClaimsPrincipal for this request.
    /// </summary>
    string? ObjectId { get; }

    /// <summary>
    /// Gets the unique identifier to represent this request in trace logs.
    /// </summary>
    string? HttpTraceIdentifier { get; }

    /// <summary>
    /// Gets the portion of the request path that identifies the requested resource.
    /// </summary>
    string? HttpRequestPath { get; }
}
