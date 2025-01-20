using System.Net;

namespace Trelnex.Core.Data;

internal record SaveResult<TInterface, TItem>(
    HttpStatusCode HttpStatusCode,
    TItem? Item)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
