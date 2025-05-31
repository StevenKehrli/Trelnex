namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class BatchCommandSaveTests
{
    [Test]
    [Description("Tests that a batch command and its contained commands cannot be saved more than once")]
    public async Task BatchCommandSave_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type
        var commandProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Create a batch command and add our create command to it
        var batchCommand = commandProvider.Batch();
        batchCommand.Add(createCommand);

        // Save the batch command (which also saves the contained create command)
        await batchCommand.SaveAsync(
            cancellationToken: default);

        // Attempt to save the create command again, which should throw
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await createCommand.SaveAsync(
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");

        // Attempt to save the batch command again, which should also throw
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await batchCommand.SaveAsync(
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }
}
