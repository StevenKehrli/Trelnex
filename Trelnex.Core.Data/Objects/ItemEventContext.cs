using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents contextual information about the request that triggered an item event.
/// </summary>
/// <remarks>
/// <para>
/// This class stores important tracking and identity information from the original HTTP request
/// that resulted in a data operation. It is used within <see cref="ItemEvent{TItem}"/> to
/// provide audit trail capabilities by preserving details about who made changes and from where.
/// </para>
/// <para>
/// The properties are designed to facilitate troubleshooting and security auditing without
/// storing excessive or sensitive information.
/// </para>
/// </remarks>
/// <seealso cref="ItemEvent{TItem}"/>
/// <seealso cref="IRequestContext"/>
public class ItemEventContext
{
    #region Public Properties

    /// <summary>
    /// Gets the unique object ID associated with the ClaimsPrincipal for this request.
    /// </summary>
    /// <value>
    /// A string containing the identity's object ID, or <see langword="null"/> if not available.
    /// </value>
    /// <remarks>
    /// This typically represents the authenticated user's unique identifier.
    /// </remarks>
    [JsonInclude]
    [JsonPropertyName("objectId")]
    public string? ObjectId { get; private set; }

    /// <summary>
    /// Gets the unique identifier to represent this request in trace logs.
    /// </summary>
    /// <value>
    /// A string containing the HTTP trace identifier, or <see langword="null"/> if not available.
    /// </value>
    /// <remarks>
    /// This identifier can be used to correlate this event with other log entries for the same request.
    /// </remarks>
    [JsonInclude]
    [JsonPropertyName("httpTraceIdentifier")]
    public string? HttpTraceIdentifier { get; private set; }

    /// <summary>
    /// Gets the portion of the request path that identifies the requested resource.
    /// </summary>
    /// <value>
    /// A string containing the HTTP request path, or <see langword="null"/> if not available.
    /// </value>
    /// <remarks>
    /// This provides context about which API endpoint initiated the change.
    /// </remarks>
    [JsonInclude]
    [JsonPropertyName("httpRequestPath")]
    public string? HttpRequestPath { get; private set; }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Creates a new <see cref="ItemEventContext"/> instance from the specified request context.
    /// </summary>
    /// <param name="context">The request context containing the information to preserve.</param>
    /// <returns>A new <see cref="ItemEventContext"/> instance with data copied from the request context.</returns>
    /// <remarks>
    /// This factory method extracts and preserves only the necessary context information
    /// from the original request.
    /// </remarks>
    internal static ItemEventContext Convert(
        IRequestContext context)
    {
        return new ItemEventContext
        {
            ObjectId = context.ObjectId,
            HttpTraceIdentifier = context.HttpTraceIdentifier,
            HttpRequestPath = context.HttpRequestPath,
        };
    }

    #endregion
}
