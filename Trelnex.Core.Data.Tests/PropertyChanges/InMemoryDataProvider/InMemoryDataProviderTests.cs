using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

[Category("EventPolicy")]
public class InMemoryDataProviderTests : EventPolicyTests
{
    private InMemoryDataProviderFactory _factory = null!;
    private InMemoryDataProvider<EventPolicyTestItem> _dataProvider = null!;

    protected override IDataProvider<EventPolicyTestItem> GetDataProvider(
        string typeName,
        CommandOperations commandOperations,
        EventPolicy eventPolicy,
        IBlockCipherService? blockCipherService = null)
    {
        // Create our data provider.
        var dataProvider = _factory.Create<EventPolicyTestItem>(
            typeName: typeName,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            blockCipherService: blockCipherService);

        _dataProvider = (dataProvider as InMemoryDataProvider<EventPolicyTestItem>)!;

        return dataProvider;
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

    /// <summary>
    /// Sets up the InMemoryDataProvider for testing.
    /// </summary>
    /// <remarks>
    /// This method initializes the data provider that will be tested by all the test methods
    /// inherited from <see cref="DataProviderTests"/>. It also captures the Clear method
    /// via reflection to allow cleaning up between tests.
    /// </remarks>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create our data provider.
        _factory = await InMemoryDataProviderFactory.Create();
    }
}
