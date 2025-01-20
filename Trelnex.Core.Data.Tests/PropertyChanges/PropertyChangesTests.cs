using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

public class PropertyChangesTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task PropertyChanges_IdAndMessage()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set - but no change
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        var propertyChanges = (createCommand as ProxyManager<ITestItem, TestItem>)!.GetPropertyChanges();

        Snapshot.Match(propertyChanges);
    }

    [Test]
    public async Task PropertyChanges_NoChange()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set - but no change
        createCommand.Item.PublicId = 0;
        createCommand.Item.PublicMessage = null!;

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        var propertyChanges = (createCommand as ProxyManager<ITestItem, TestItem>)!.GetPropertyChanges();

        Assert.That(propertyChanges, Is.Null);
    }

    [Test]
    public async Task PropertyChanges_SetAndReset()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";

        // reset
        createCommand.Item.PublicId = 0;
        createCommand.Item.PublicMessage = null!;

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        var propertyChanges = (createCommand as ProxyManager<ITestItem, TestItem>)!.GetPropertyChanges();

        Assert.That(propertyChanges, Is.Null);
    }
}
