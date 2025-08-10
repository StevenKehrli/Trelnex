namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class UpdateCommandSaveTests
{
    [Test]
    [Description("Tests that update command throws when operations are not supported")]
    public async Task UpdateCommandSave_SaveAsync_NotSupported()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider with no supported operations
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Read);

        // Attempt to create an update command, which should throw
        Assert.ThrowsAsync<NotSupportedException>(
            async () => await dataProvider.UpdateAsync(id: id, partitionKey: partitionKey),
            "The requested Update operation is not supported.");
    }

    [Test]
    [Description("Tests that the result returned from saving an update command is read-only")]
    public async Task UpdateCommandSave_SaveAsync_ResultIsReadOnly()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

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

        // Create an update command for the saved item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Update the item values
        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // Save the update and get the result
        var updated = await updateCommand.SaveAsync(
            cancellationToken: default);

        // Verify the item is read-only
        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated, Is.Not.Null);
            Assert.That(updated.Item, Is.Not.Null);

            Assert.That(updated.Item.Version, Is.EqualTo(2));

            Assert.Throws<InvalidOperationException>(
                () => updated.Item.PublicMessage = "Public #3",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => updated.Item.PrivateMessage = "Private #3",
                $"The '{typeof(ITestItem)}' is read-only");
        }    }

    [Test]
    [Description("Tests that an update command cannot be saved more than once")]
    public async Task UpdateCommandSave_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

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

        // Create an update command for the saved item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Update the item values
        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // Save the update command
        await updateCommand.SaveAsync(
            cancellationToken: default);

        // Attempt to save it again, which should throw
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await updateCommand.SaveAsync(
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }
}
