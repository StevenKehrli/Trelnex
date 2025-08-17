using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

[Category("EventPolicy")]
public abstract partial class EventPolicyTests
{
    protected abstract IDataProvider<EventPolicyTestItem> GetDataProvider(
        string typeName,
        CommandOperations commandOperations,
        EventPolicy eventPolicy,
        IBlockCipherService? blockCipherService = null);

    protected abstract ItemEvent[] GetItemEvents(
        string id,
        string partitionKey);
}
