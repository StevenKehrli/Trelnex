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
    /// Creation timestamp.
    /// </summary>
    DateTime CreatedDate { get; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    DateTime UpdatedDate { get; }

    /// <summary>
    /// Deletion timestamp, or null if not deleted.
    /// </summary>
    DateTime? DeletedDate { get; }

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
public abstract class BaseItem : IBaseItem
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
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("updatedDate")]
    public DateTime UpdatedDate { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("deletedDate")]
    public DateTime? DeletedDate { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("isDeleted")]
    public bool? IsDeleted { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("_etag")]
    public string? ETag { get; internal set; }

    #endregion
}
