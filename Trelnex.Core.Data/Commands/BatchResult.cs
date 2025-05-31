using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Results from batch operations.
/// </summary>
/// <typeparam name="TInterface">Interface type for items in result.</typeparam>
/// <remarks>
/// Represents outcome of a single item operation within a batch.
/// </remarks>
public interface IBatchResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// HTTP status code.
    /// </summary>
    HttpStatusCode HttpStatusCode { get; }

    /// <summary>
    /// Read-only result (null if failed).
    /// </summary>
    IReadResult<TInterface>? ReadResult { get; }
}

/// <summary>
/// Immutable batch operation result.
/// </summary>
/// <typeparam name="TInterface">Interface type for items.</typeparam>
/// <typeparam name="TItem">Concrete item implementation type.</typeparam>
/// <param name="httpStatusCode">Status code.</param>
/// <param name="readResult">Processed item result (null for failures).</param>
/// <remarks>
/// Implements <see cref="IBatchResult{TInterface}"/>.
/// </remarks>
internal sealed class BatchResult<TInterface, TItem>(
    HttpStatusCode httpStatusCode,
    IReadResult<TInterface>? readResult)
    : IBatchResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Public Properties

    /// <inheritdoc/>
    public HttpStatusCode HttpStatusCode => httpStatusCode;

    /// <inheritdoc/>
    public IReadResult<TInterface>? ReadResult => readResult;

    #endregion
}
