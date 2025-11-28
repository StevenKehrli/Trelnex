using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

[Category("EventPolicy")]
public class InMemoryDataProviderTests : EventPolicyTests
{
    private InMemoryDataProvider<EventPolicyTestItem> _dataProvider = null!;

    protected override Task<IDataProvider<EventPolicyTestItem>> GetDataProviderAsync(
        string typeName,
        CommandOperations commandOperations,
        EventPolicy eventPolicy,
        IBlockCipherService? blockCipherService = null)
    {
        // Create our data provider.
        var dataProvider = new InMemoryDataProvider<EventPolicyTestItem>(
            typeName: typeName,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            blockCipherService: blockCipherService);

        _dataProvider = dataProvider;

        return Task.FromResult<IDataProvider<EventPolicyTestItem>>(dataProvider);
    }

    protected override Task<ItemEvent[]> GetItemEventsAsync(
        string id,
        string partitionKey)
    {
        var events = _dataProvider.GetEvents()
            .Where(e => e.RelatedId == id && e.PartitionKey == partitionKey)
            .ToArray();

        return Task.FromResult(events);
    }
}