namespace Trelnex.Core.Api.Context;

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
}
