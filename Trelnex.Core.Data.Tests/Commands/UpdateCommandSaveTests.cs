namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class UpdateCommandSaveTests
{
    [Test]
    [Description("Tests that an update command's item becomes read-only after saving")]
    public async Task UpdateCommand_SaveAsync_IsReadOnlyAfterSave()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item");

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the create command first
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // Create an update command for the saved item
        var updateCommand = await commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Update the item values
        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // Save the update and get the result
        var updated = await updateCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated.Item, Is.Not.Null);

        // Verify the item is read-only
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => updated.Item.PublicMessage = "Public #3",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => updated.Item.PrivateMessage = "Private #3",
                $"The '{typeof(ITestItem)}' is read-only");
        });
    }

    [Test]
    [Description("Tests that update command throws when operations are not supported")]
    public async Task UpdateCommand_SaveAsync_NotSupported()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider with no supported operations
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                commandOperations: CommandOperations.None);

        // Attempt to create an update command, which should throw
        Assert.ThrowsAsync<NotSupportedException>(
            async () => await commandProvider.UpdateAsync(id: id, partitionKey: partitionKey),
            "The requested Update operation is not supported.");
    }

    [Test]
    [Description("Tests that the result returned from saving an update command is read-only")]
    public async Task UpdateCommand_SaveAsync_ResultIsReadOnly()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with update operations
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                commandOperations: CommandOperations.Update);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the create command first
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // Create an update command for the saved item
        var updateCommand = await commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Update the item values
        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // Save the update and get the result
        var updated = await updateCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated.Item, Is.Not.Null);

        // Verify the result is read-only
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => updated.Item.PublicMessage = "Public #3",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => updated.Item.PrivateMessage = "Private #3",
                $"The '{typeof(ITestItem)}' is read-only");
        });
    }

    [Test]
    [Description("Tests that an update command cannot be saved more than once")]
    public async Task UpdateCommand_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item");

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the create command first
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // Create an update command for the saved item
        var updateCommand = await commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Update the item values
        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // Save the update command
        await updateCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // Attempt to save it again, which should throw
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await updateCommand.SaveAsync(
                requestContext: requestContext,
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }
}
