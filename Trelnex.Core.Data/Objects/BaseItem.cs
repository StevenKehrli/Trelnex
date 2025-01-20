using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

public interface IBaseItem
{
    /// <summary>
    /// The unique identifier that identifies the item within a container.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The unique identifier that identifies a logical partition within a container.
    /// </summary>
    string PartitionKey { get; }

    /// <summary>
    /// The type name of the item.
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// The date time this item was created.
    /// </summary>
    DateTime CreatedDate { get; }

    /// <summary>
    /// The date time this item was updated.
    /// </summary>
    DateTime UpdatedDate { get; }

    /// <summary>
    /// The date time this item was deleted.
    /// </summary>
    DateTime? DeletedDate { get; }

    /// <summary>
    /// Gets a value indicating if this item was deleted.
    /// </summary>
    bool? IsDeleted { get; }

    /// <summary>
    /// The identifier for a specific version of this item.
    /// </summary>
    string? ETag { get; }
}

public abstract class BaseItem : IBaseItem
{
    /// <summary>
    /// The unique identifier that identifies the item within a container.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; internal set; } = null!;

    /// <summary>
    /// The unique identifier that identifies a logical partition within a container.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; internal set; } = null!;

    /// <summary>
    /// The type name of the item.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("typeName")]
    public string TypeName { get; internal set; } = null!;

    /// <summary>
    /// The date time this item was created.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; internal set; }

    /// <summary>
    /// The date time this item was updated.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("updatedDate")]
    public DateTime UpdatedDate { get; internal set; }

    /// <summary>
    /// The date time this item was deleted.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("deletedDate")]
    public DateTime? DeletedDate { get; internal set; }

    /// <summary>
    /// Gets a value indicating if this item was deleted.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("isDeleted")]
    public bool? IsDeleted { get; internal set; }

    /// <summary>
    /// The identifier for a specific version of this item.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("_etag")]
    public string? ETag { get; internal set; }
}
