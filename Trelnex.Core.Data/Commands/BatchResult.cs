using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines the result of a single operation within a batch.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
public interface IBatchResult<TItem>
    where TItem : BaseItem
{
    /// <summary>
    /// Gets the HTTP status code indicating the outcome of the operation.
    /// </summary>
    HttpStatusCode HttpStatusCode { get; }

    /// <summary>
    /// Gets the result wrapper for the processed item, or null if the operation failed.
    /// </summary>
    IReadResult<TItem>? ReadResult { get; }
}

/// <summary>
/// Represents the result of a single operation within a batch.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="httpStatusCode">HTTP status code indicating the operation outcome.</param>
/// <param name="readResult">Result wrapper for the processed item, or null if failed.</param>
internal sealed class BatchResult<TItem>(
    HttpStatusCode httpStatusCode,
    IReadResult<TItem>? readResult)
    : IBatchResult<TItem>
    where TItem : BaseItem
{
    #region Public Properties

    /// <inheritdoc/>
    public HttpStatusCode HttpStatusCode => httpStatusCode;

    /// <inheritdoc/>
    public IReadResult<TItem>? ReadResult => readResult;

    #endregion
}
