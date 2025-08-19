using Microsoft.Extensions.Logging;

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
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Read);

        // Attempt to create a delete command, which should throw
        Assert.ThrowsAsync<NotSupportedException>(
            async () => await dataProvider.DeleteAsync(id: id, partitionKey: partitionKey),
            "The requested Delete operation is not supported.");
    }

    [Test]
    [Description("Tests that the result returned from saving a delete command logs a warning when modified.")]
    public async Task DeleteCommandSave_SaveAsync_ResultIsModified()
    {
        var id = "13fcc745-fe36-4f68-ad3c-977fed8d1833";
        var partitionKey = "b0d2c39c-23bc-4b4e-8cc3-49599056cfec";

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with delete operations
        var logger = new TestLogger();
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Delete,
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

            deleted.Item.PublicMessage = "Public #2";
            deleted.Item.PrivateMessage = "Private #2";

            deleted.Dispose();

            var logEntries = logger.LogEntries;

            Assert.That(logEntries, Is.Not.Null);
            Assert.That(logEntries, Has.Count.EqualTo(1));
            Assert.That(logEntries[0].LogLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(logEntries[0].Message, Is.EqualTo("Item id = '13fcc745-fe36-4f68-ad3c-977fed8d1833' partitionKey = 'b0d2c39c-23bc-4b4e-8cc3-49599056cfec' was modified."));
        }
    }

    [Test]
    [Description("Tests that the result returned from saving a delete command does not log a warning when not modified.")]
    public async Task DeleteCommandSave_SaveAsync_ResultIsNotModified()
    {
        var id = "94bbfdb3-1fab-4d50-802f-5cdc405e35db";
        var partitionKey = "61a77f60-8911-4ecc-b41b-69386b8e99ae";

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with delete operations
        var logger = new TestLogger();
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Delete,
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

            deleted.Dispose();

            var logEntries = logger.LogEntries;

            Assert.That(logEntries, Is.Not.Null);
            Assert.That(logEntries, Has.Count.EqualTo(0));
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
        var dataProvider = factory.Create<TestItem>(
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
