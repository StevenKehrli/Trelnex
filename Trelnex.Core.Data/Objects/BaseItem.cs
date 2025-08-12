using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Core properties for all data items.
/// </summary>
/// <remarks>
/// Contract for identification, typing, versioning, and lifecycle tracking.
/// </remarks>
public interface IBaseItem
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Logical partition identifier.
    /// </summary>
    /// <remarks>
    /// Determines physical storage location.
    /// </remarks>
    string PartitionKey { get; }

    /// <summary>
    /// Type name of the item.
    /// </summary>
    /// <remarks>
    /// Distinguishes between item types.
    /// </remarks>
    string TypeName { get; }

    /// <summary>
    /// Gets the version number of the item.
    /// </summary>
    /// <value>
    /// An integer representing the current version of the item.
    /// </value>
    int Version { get; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    DateTimeOffset CreatedDateTimeOffset { get; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    DateTimeOffset UpdatedDateTimeOffset { get; }

    /// <summary>
    /// Deletion timestamp, or null if not deleted.
    /// </summary>
    DateTimeOffset? DeletedDateTimeOffset { get; }

    /// <summary>
    /// Flag indicating if item is deleted.
    /// </summary>
    bool? IsDeleted { get; }

    /// <summary>
    /// Version identifier for optimistic concurrency control.
    /// </summary>
    /// <remarks>
    /// Prevents conflicting updates.
    /// </remarks>
    string? ETag { get; }
}

/// <summary>
/// Base implementation for data items.
/// </summary>
/// <remarks>
/// Implements <see cref="IBaseItem"/> with common properties.
/// </remarks>
public abstract record BaseItem : IBaseItem
{
    #region Public Properties

    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; internal set; } = null!;

    [JsonInclude]
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; internal set; } = null!;

    [JsonInclude]
    [JsonPropertyName("typeName")]
    public string TypeName { get; internal set; } = null!;

    [JsonInclude]
    [JsonPropertyName("version")]
    public int Version { get; internal set; } = 0;

    [JsonInclude]
    [JsonPropertyName("createdDateTimeOffset")]
    public DateTimeOffset CreatedDateTimeOffset { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("updatedDateTimeOffset")]
    public DateTimeOffset UpdatedDateTimeOffset { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("deletedDateTimeOffset")]
    public DateTimeOffset? DeletedDateTimeOffset { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("isDeleted")]
    public bool? IsDeleted { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("_etag")]
    public string? ETag { get; internal set; }

    #endregion
}
