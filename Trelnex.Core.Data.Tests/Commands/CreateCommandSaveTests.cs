using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class CreateCommandSaveTests
{
    [Test]
    [Description("Tests that create command throws when operations are not supported")]
    public async Task CreateCommandSave_SaveAsync_NotSupported()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Get a data provider with no supported operations
        var dataProvider = await InMemoryDataProvider<TestItem>.CreateAsync(
            typeName: "test-item",
            commandOperations: CommandOperations.Read);

        // Attempt to create an create command, which should throw
        Assert.Throws<NotSupportedException>(
            () => dataProvider.Create(id: id, partitionKey: partitionKey),
            "The requested Create operation is not supported.");
    }

    [Test]
    [Description("Tests that the result returned from saving a create command logs a warning when modified.")]
    public async Task CreateCommandSave_SaveAsync_ResultIsModified()
    {
        var id = "ac2c94cd-f98b-4031-929c-8911dee3082e";
        var partitionKey = "d99535fc-d254-490d-8eb4-4fef25ed1c6f";

        // Get a data provider for our test item type
        var logger = new TestLogger();
        var dataProvider = await InMemoryDataProvider<TestItem>.CreateAsync(
            typeName: "test-item",
            commandOperations: CommandOperations.Create,
            logger: logger);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the command and get the result
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        // Verify the result is read-only
        using (Assert.EnterMultipleScope())
        {
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Item, Is.Not.Null);

            Assert.That(created.Item.Version, Is.EqualTo(1));

            created.Item.PublicMessage = "Public #2";
            created.Item.PrivateMessage = "Private #2";

            created.Dispose();

            var logEntries = logger.LogEntries;

            Assert.That(logEntries, Is.Not.Null);
            Assert.That(logEntries, Has.Count.EqualTo(1));
            Assert.That(logEntries[0].LogLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(logEntries[0].Message, Is.EqualTo("Item id = 'ac2c94cd-f98b-4031-929c-8911dee3082e' partitionKey = 'd99535fc-d254-490d-8eb4-4fef25ed1c6f' was modified."));
        }
    }

    [Test]
    [Description("Tests that the result returned from saving a create command does not log a warning when not modified.")]
    public async Task CreateCommandSave_SaveAsync_ResultIsNotModified()
    {
        var id = "37e31aa2-e67d-4d0b-a1f3-5aac56aaa89b";
        var partitionKey = "096a2674-c2c1-4c8d-a474-d930ad7af489";

        // Get a data provider for our test item type
        var logger = new TestLogger();
        var dataProvider = await InMemoryDataProvider<TestItem>.CreateAsync(
            typeName: "test-item",
            commandOperations: CommandOperations.Create,
            logger: logger);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the command and get the result
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        // Verify the result is read-only
        using (Assert.EnterMultipleScope())
        {
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Item, Is.Not.Null);

            Assert.That(created.Item.Version, Is.EqualTo(1));

            created.Dispose();

            var logEntries = logger.LogEntries;

            Assert.That(logEntries, Is.Not.Null);
            Assert.That(logEntries, Has.Count.EqualTo(0));
        }
    }

    [Test]
    [Description("Tests that a create command cannot be saved more than once")]
    public async Task CreateCommandSave_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Get a data provider for our test item type
        var dataProvider = await InMemoryDataProvider<TestItem>.CreateAsync(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Attempt to save it again, which should throw
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await createCommand.SaveAsync(
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }
}
