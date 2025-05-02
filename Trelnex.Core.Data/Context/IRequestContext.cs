namespace Trelnex.Core.Data;

/// <summary>
/// Defines the contract for accessing contextual information about an HTTP request.
/// </summary>
/// <remarks>
/// This interface provides access to key information about the current request context,
/// including user identity and request routing details. It is used primarily for audit
/// logging and request tracking purposes.
/// </remarks>
/// <seealso cref="ItemEventContext"/>
public interface IRequestContext
{
    #region Properties

    /// <summary>
    /// Gets the unique object ID associated with the ClaimsPrincipal for this request.
    /// </summary>
    /// <value>
    /// A string containing the authenticated user's identity, or <see langword="null"/> if
    /// authentication information is not available.
    /// </value>
    string? ObjectId { get; }

    /// <summary>
    /// Gets the unique identifier to represent this request in trace logs.
    /// </summary>
    /// <value>
    /// A string containing a request-specific tracing identifier, or <see langword="null"/> if
    /// not available.
    /// </value>
    string? HttpTraceIdentifier { get; }

    /// <summary>
    /// Gets the portion of the request path that identifies the requested resource.
    /// </summary>
    /// <value>
    /// A string containing the HTTP request path, or <see langword="null"/> if
    /// not available.
    /// </value>
    string? HttpRequestPath { get; }

    #endregion
}
