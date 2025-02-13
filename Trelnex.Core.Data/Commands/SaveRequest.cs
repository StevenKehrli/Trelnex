namespace Trelnex.Core.Data;

public record SaveRequest<TInterface, TItem>(
    TItem Item,
    ItemEvent<TItem> Event,
    SaveAction SaveAction)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
