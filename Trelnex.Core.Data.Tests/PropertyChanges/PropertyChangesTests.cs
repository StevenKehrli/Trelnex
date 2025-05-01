using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

[Category("PropertyChanges")]
public class PropertyChangesTests
{
    [Test]
    [Description("Tests that property changes are tracked correctly when properties are modified")]
    public async Task PropertyChanges_IdAndMessage()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item");

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set properties to new values that should be tracked
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as ProxyManager<ITestItem, TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that no property changes are tracked when properties are set to their default values")]
    public async Task PropertyChanges_NoChange()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item");

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set properties to their default values - should not be tracked as changes
        createCommand.Item.PublicId = 0;
        createCommand.Item.PublicMessage = null!;

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as ProxyManager<ITestItem, TestItem>)!.GetPropertyChanges();

        // verify that no properties were changed
        Assert.That(propertyChanges, Is.Null);
    }

    [Test]
    [Description("Tests that property changes are not tracked when properties are set and then reset to their original values")]
    public async Task PropertyChanges_SetAndReset()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item");

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first set properties to new values
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";

        // then reset them back to their default values
        createCommand.Item.PublicId = 0;
        createCommand.Item.PublicMessage = null!;

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as ProxyManager<ITestItem, TestItem>)!.GetPropertyChanges();

        // verify that no properties were changed since they were reset to original values
        Assert.That(propertyChanges, Is.Null);
    }
}
