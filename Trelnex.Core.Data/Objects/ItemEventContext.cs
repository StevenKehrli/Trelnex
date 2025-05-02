using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Contextual information for an item event.
/// </summary>
/// <remarks>
/// Stores tracking and identity information from the original HTTP request for audit trails.
/// </remarks>
public class ItemEventContext
{
    #region Public Properties

    /// <summary>
    /// Unique object ID associated with the authenticated user.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("objectId")]
    public string? ObjectId { get; private set; }

    /// <summary>
    /// Unique identifier for correlating this event with trace logs.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("httpTraceIdentifier")]
    public string? HttpTraceIdentifier { get; private set; }

    /// <summary>
    /// HTTP request path that initiated the change.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("httpRequestPath")]
    public string? HttpRequestPath { get; private set; }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Creates a new context instance from the specified request context.
    /// </summary>
    /// <param name="context">Request context with information to preserve.</param>
    /// <returns>New context with copied request data.</returns>
    /// <remarks>
    /// Extracts tracking information from the original request.
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
