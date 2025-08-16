using Microsoft.Extensions.Logging;

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
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Read);

        // Attempt to create an update command, which should throw
        Assert.ThrowsAsync<NotSupportedException>(
            async () => await dataProvider.UpdateAsync(id: id, partitionKey: partitionKey),
            "The requested Update operation is not supported.");
    }

    [Test]
    [Description("Tests that the result returned from saving an update command logs a warning when modified.")]
    public async Task UpdateCommandSave_SaveAsync_ResultIsModified()
    {
        var id = "fb494bc5-0268-437a-bc61-0d60112571c8";
        var partitionKey = "5c833fcd-da29-4361-bab8-5e66c4c0caf3";

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var logger = new TestLogger();
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update,
            logger: logger);

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

            updated.Item.PublicMessage = "Public #3";
            updated.Item.PrivateMessage = "Private #3";

            updated.Dispose();

            var logEntries = logger.LogEntries;

            Assert.That(logEntries, Is.Not.Null);
            Assert.That(logEntries, Has.Count.EqualTo(1));
            Assert.That(logEntries[0].LogLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(logEntries[0].Message, Is.EqualTo("Item id = 'fb494bc5-0268-437a-bc61-0d60112571c8' partitionKey = '5c833fcd-da29-4361-bab8-5e66c4c0caf3' was modified."));
        }
    }

    [Test]
    [Description("Tests that the result returned from saving an update command does not log a warning when not modified.")]
    public async Task UpdateCommandSave_SaveAsync_ResultIsNotModified()
    {
        var id = "c4a1f396-13fe-433a-b4ab-979a5deb5d74";
        var partitionKey = "e550e6fa-1810-4d22-8f14-ebc2bff5f560";

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var logger = new TestLogger();
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update,
            logger: logger);

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

            updated.Dispose();

            var logEntries = logger.LogEntries;

            Assert.That(logEntries, Is.Not.Null);
            Assert.That(logEntries, Has.Count.EqualTo(0));
        }
    }

    [Test]
    [Description("Tests that an update command cannot be saved more than once")]
    public async Task UpdateCommandSave_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
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
