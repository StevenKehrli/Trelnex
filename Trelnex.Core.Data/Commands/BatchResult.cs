using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines the contract for results returned from batch operations.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the items in the batch result. Must implement <see cref="IBaseItem"/>.</typeparam>
public interface IBatchResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the HTTP status code of this batch result.
    /// </summary>
    /// <value>An <see cref="HttpStatusCode"/> representing the outcome of the batch operation.</value>
    HttpStatusCode HttpStatusCode { get; }

    /// <summary>
    /// Gets the read result of this batch operation, if successful.
    /// </summary>
    /// <value>
    /// An <see cref="IReadResult{TInterface}"/> containing the read results if the operation was successful;
    /// otherwise, <see langword="null"/>.
    /// </value>
    IReadResult<TInterface>? ReadResult { get; }
}

/// <summary>
/// Implements the result of a batch operation for a specific item type.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the items in the batch result. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <typeparam name="TItem">The concrete item type in the batch. Must inherit from <see cref="BaseItem"/> and implement <typeparamref name="TInterface"/>.</typeparam>
/// <param name="httpStatusCode">The HTTP status code representing the outcome of the batch operation.</param>
/// <param name="readResult">The read result containing the processed items, if the operation was successful.</param>
/// <remarks>
/// This class provides a concrete implementation for batch operation results.
/// The HTTP status code indicates the overall success or failure of the operation.
/// </remarks>
internal class BatchResult<TInterface, TItem>(
    HttpStatusCode httpStatusCode,
    IReadResult<TInterface>? readResult)
    : IBatchResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Properties

    /// <inheritdoc/>
    public HttpStatusCode HttpStatusCode => httpStatusCode;

    /// <inheritdoc/>
    public IReadResult<TInterface>? ReadResult => readResult;

    #endregion
}
