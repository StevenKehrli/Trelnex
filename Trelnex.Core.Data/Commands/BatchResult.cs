using System.Net;

namespace Trelnex.Core.Data;

public interface IBatchResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the <see cref="HttpStatusCode"/> of this batch result.
    /// </summary>
    HttpStatusCode HttpStatusCode { get; }

    /// <summary>
    /// Geths the <see cref="IReadResult{TInterface}"/>  of this batch result, if successful.
    /// </summary>
    IReadResult<TInterface>? ReadResult { get; }
}

internal class BatchResult<TInterface, TItem>(
    HttpStatusCode httpStatusCode,
    IReadResult<TInterface>? readResult)
    : IBatchResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    public HttpStatusCode HttpStatusCode => httpStatusCode;

    public IReadResult<TInterface>? ReadResult => readResult;
}
