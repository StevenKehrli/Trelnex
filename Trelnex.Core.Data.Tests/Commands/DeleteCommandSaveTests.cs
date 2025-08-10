namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class DeleteCommandSaveTests
{
    [Test]
    [Description("Tests that delete command throws when operations are not supported")]
    public async Task DeleteCommandSave_SaveAsync_NotSupported()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider with no supported operations
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Read);

        // Attempt to create a delete command, which should throw
        Assert.ThrowsAsync<NotSupportedException>(
            async () => await dataProvider.DeleteAsync(id: id, partitionKey: partitionKey),
            "The requested Delete operation is not supported.");
    }

    [Test]
    [Description("Tests that the result returned from saving a delete command is read-only")]
    public async Task DeleteCommandSave_SaveAsync_ResultIsReadOnly()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with delete operations
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Delete);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the create command first
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the saved item
        using var deleteCommand = await dataProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the delete command and get the result
        var deleted = await deleteCommand.SaveAsync(
            cancellationToken: default);

        // Verify the result is read-only
        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.Not.Null);
            Assert.That(deleted.Item, Is.Not.Null);

            Assert.That(deleted.Item.Version, Is.EqualTo(2));

            Assert.Throws<InvalidOperationException>(
                () => deleted.Item.PublicMessage = "Public #3",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => deleted.Item.PrivateMessage = "Private #3",
                $"The '{typeof(ITestItem)}' is read-only");
        }
    }

    [Test]
    [Description("Tests that a delete command cannot be saved more than once")]
    public async Task DeleteCommandSave_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with delete operations
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Delete);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the create command first
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the saved item
        using var deleteCommand = await dataProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the delete command
        await deleteCommand.SaveAsync(
            cancellationToken: default);

        // Attempt to save it again, which should throw
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await deleteCommand.SaveAsync(
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }
}
