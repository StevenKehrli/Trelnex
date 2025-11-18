using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class ReadCommandReadTests
{
    [Test]
    [Description("Tests that the result returned from reading an item logs a warning when modified.")]
    public async Task ReadCommandRead_ReadAsync_ResultIsModified()
    {
        var id = "a15f192b-e695-4220-9fd8-bc07653ca2ce";
        var partitionKey = "76f66bd8-c265-4b9c-9ff0-8ade598e1cc7";

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

        // Save the item first so we can read it
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Read the saved item
        using var read = await dataProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        // Verify the result is read-only
        using (Assert.EnterMultipleScope())
        {
            Assert.That(read, Is.Not.Null);
            Assert.That(read!.Item, Is.Not.Null);

            Assert.That(read.Item.Version, Is.EqualTo(1));

            read.Item.PublicMessage = "Public #2";
            read.Item.PrivateMessage = "Private #2";

            read.Dispose();

            var logEntries = logger.LogEntries;

            Assert.That(logEntries, Is.Not.Null);
            Assert.That(logEntries, Has.Count.EqualTo(1));
            Assert.That(logEntries[0].LogLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(logEntries[0].Message, Is.EqualTo("Item id = 'a15f192b-e695-4220-9fd8-bc07653ca2ce' partitionKey = '76f66bd8-c265-4b9c-9ff0-8ade598e1cc7' was modified."));
        }
    }

    [Test]
    [Description("Tests that the result returned from reading an item does not log a warning when not modified.")]
    public async Task ReadCommandRead_ReadAsync_ResultIsNotModified()
    {
        var id = "36db3199-97ce-4dc4-9181-b2add674d03b";
        var partitionKey = "f98be670-d708-4722-8551-bd5ef4515aab";

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

        // Save the item first so we can read it
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Read the saved item
        using var read = await dataProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        // Verify the result is read-only
        using (Assert.EnterMultipleScope())
        {
            Assert.That(read, Is.Not.Null);
            Assert.That(read!.Item, Is.Not.Null);

            Assert.That(read.Item.Version, Is.EqualTo(1));

            read.Dispose();

            var logEntries = logger.LogEntries;

            Assert.That(logEntries, Is.Not.Null);
            Assert.That(logEntries, Has.Count.EqualTo(0));
        }
    }
}
