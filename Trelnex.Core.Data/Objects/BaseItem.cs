using System.Net;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Base record type for all data items with common properties and metadata.
/// </summary>
public abstract record BaseItem
{
    #region Public Properties

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; internal set; } = null!;

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; internal set; } = null!;

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("typeName")]
    public string TypeName { get; internal set; } = null!;

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("version")]
    public int Version { get; internal set; } = 0;

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("createdDateTimeOffset")]
    public DateTimeOffset CreatedDateTimeOffset { get; internal set; }

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("updatedDateTimeOffset")]
    public DateTimeOffset UpdatedDateTimeOffset { get; internal set; }

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("deletedDateTimeOffset")]
    public DateTimeOffset? DeletedDateTimeOffset { get; internal set; }

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("isDeleted")]
    public bool? IsDeleted { get; internal set; }

    [DoNotTrack]
    [JsonInclude]
    [JsonPropertyName("_etag")]
    public string? ETag { get; internal set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Validates that the provided ETag matches the current item's ETag to ensure optimistic concurrency control.
    /// </summary>
    /// <param name="eTag">The ETag value to compare against the current item's ETag.</param>
    /// <exception cref="HttpStatusCodeException">Thrown with HttpStatusCode.Conflict when the ETags do not match, indicating the item has been modified by another process.</exception>
    public void ValidateETag(
        string? eTag)
    {
        if (string.Equals(ETag, eTag, StringComparison.Ordinal) is false)
        {
            throw new HttpStatusCodeException(HttpStatusCode.Conflict);
        }
    }

    #endregion
}
