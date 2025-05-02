using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines the core properties that must be implemented by all data items in the system.
/// </summary>
/// <remarks>
/// This interface establishes a contract for basic identification, typing, versioning,
/// and lifecycle tracking of data items throughout the application.
/// </remarks>
public interface IBaseItem
{
    /// <summary>
    /// Gets the unique identifier that identifies the item within a container.
    /// </summary>
    /// <value>A string containing the unique identifier of the item.</value>
    string Id { get; }

    /// <summary>
    /// Gets the unique identifier that identifies a logical partition within a container.
    /// </summary>
    /// <value>A string containing the partition key for the item.</value>
    /// <remarks>
    /// The partition key determines the physical storage location of the item and is used
    /// for distributing data across multiple storage nodes.
    /// </remarks>
    string PartitionKey { get; }

    /// <summary>
    /// Gets the type name of the item.
    /// </summary>
    /// <value>A string representing the item's type.</value>
    /// <remarks>
    /// The type name is used for polymorphic operations and to distinguish between different
    /// kinds of items within the same storage container. Some type names (like "event") are
    /// reserved for system use. See <see cref="ReservedTypeNames"/>.
    /// </remarks>
    string TypeName { get; }

    /// <summary>
    /// Gets the date and time when this item was created.
    /// </summary>
    /// <value>A <see cref="DateTime"/> value representing the creation timestamp.</value>
    DateTime CreatedDate { get; }

    /// <summary>
    /// Gets the date and time when this item was last updated.
    /// </summary>
    /// <value>A <see cref="DateTime"/> value representing the last update timestamp.</value>
    DateTime UpdatedDate { get; }

    /// <summary>
    /// Gets the date and time when this item was deleted, if applicable.
    /// </summary>
    /// <value>
    /// A <see cref="DateTime"/> value representing the deletion timestamp, or <see langword="null"/>
    /// if the item has not been deleted.
    /// </value>
    DateTime? DeletedDate { get; }

    /// <summary>
    /// Gets a value indicating whether this item has been marked as deleted.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this item has been deleted; otherwise, <see langword="false"/>.
    /// May be <see langword="null"/> if deletion state is not specified.
    /// </value>
    bool? IsDeleted { get; }

    /// <summary>
    /// Gets the identifier for a specific version of this item.
    /// </summary>
    /// <value>
    /// A string containing the ETag value, or <see langword="null"/> if versioning is not applied.
    /// </value>
    /// <remarks>
    /// ETags are used for optimistic concurrency control to prevent conflicting updates.
    /// </remarks>
    string? ETag { get; }
}

/// <summary>
/// Provides a base implementation for all data items in the system.
/// </summary>
/// <remarks>
/// <para>
/// This abstract class implements the <see cref="IBaseItem"/> interface and provides
/// common properties and behavior for all data entities. It handles identity,
/// partitioning, type identification, and lifecycle tracking.
/// </para>
/// <para>
/// All persistent data objects in the system should inherit from this base class
/// to ensure consistent behavior and serialization.
/// </para>
/// </remarks>
/// <seealso cref="IBaseItem"/>
public abstract class BaseItem : IBaseItem
{
    #region Properties

    /// <inheritdoc/>
    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; internal set; } = null!;

    /// <inheritdoc/>
    [JsonInclude]
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; internal set; } = null!;

    /// <inheritdoc/>
    [JsonInclude]
    [JsonPropertyName("typeName")]
    public string TypeName { get; internal set; } = null!;

    /// <inheritdoc/>
    [JsonInclude]
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; internal set; }

    /// <inheritdoc/>
    [JsonInclude]
    [JsonPropertyName("updatedDate")]
    public DateTime UpdatedDate { get; internal set; }

    /// <inheritdoc/>
    [JsonInclude]
    [JsonPropertyName("deletedDate")]
    public DateTime? DeletedDate { get; internal set; }

    /// <inheritdoc/>
    [JsonInclude]
    [JsonPropertyName("isDeleted")]
    public bool? IsDeleted { get; internal set; }

    /// <inheritdoc/>
    [JsonInclude]
    [JsonPropertyName("_etag")]
    public string? ETag { get; internal set; }

    #endregion
}
