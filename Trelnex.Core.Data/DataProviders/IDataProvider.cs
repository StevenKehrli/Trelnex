namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for data access and manipulation of a specific item type.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
public interface IDataProvider<TItem>
    where TItem : BaseItem
{
    /// <summary>
    /// Creates a batch command for executing multiple save operations atomically.
    /// </summary>
    /// <returns>A batch command instance for grouping operations.</returns>
    IBatchCommand<TItem> Batch();

    /// <summary>
    /// Creates a new item with the specified identifiers.
    /// </summary>
    /// <param name="id">Unique identifier for the new item.</param>
    /// <param name="partitionKey">Partition key for data distribution.</param>
    /// <returns>A save command for the new item.</returns>
    /// <exception cref="NotSupportedException">Thrown when Create operations are not allowed.</exception>
    ISaveCommand<TItem> Create(
        string id,
        string partitionKey);

    /// <summary>
    /// Prepares an item for soft deletion.
    /// </summary>
    /// <param name="id">Unique identifier of the item to delete.</param>
    /// <param name="partitionKey">Partition key of the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A save command for deletion, or null if the item doesn't exist or is already deleted.</returns>
    /// <exception cref="NotSupportedException">Thrown when Delete operations are not allowed.</exception>
    Task<ISaveCommand<TItem>?> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an item by its identifiers.
    /// </summary>
    /// <param name="id">Unique identifier of the item.</param>
    /// <param name="partitionKey">Partition key of the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read result containing the item, or null if not found or deleted.</returns>
    Task<IReadResult<TItem>?> ReadAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a query command for building and executing queries.
    /// </summary>
    /// <returns>A query command supporting LINQ operations.</returns>
    IQueryCommand<TItem> Query();

    /// <summary>
    /// Prepares an existing item for modification.
    /// </summary>
    /// <param name="id">Unique identifier of the item to update.</param>
    /// <param name="partitionKey">Partition key of the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A save command for updating the item, or null if not found or deleted.</returns>
    /// <exception cref="NotSupportedException">Thrown when Update operations are not allowed.</exception>
    Task<ISaveCommand<TItem>?> UpdateAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);
}
