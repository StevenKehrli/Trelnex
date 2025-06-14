namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class CreateCommandSaveTests
{
    [Test]
    [Description("Tests that a create command's item becomes read-only after saving")]
    public async Task CreateCommandSave_SaveAsync_IsReadOnlyAfterSave()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<ITestItem, TestItem>(
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

        // Verify the item is read-only after save
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => createCommand.Item.PublicMessage = "Public #2",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => createCommand.Item.PrivateMessage = "Private #2",
                $"The '{typeof(ITestItem)}' is read-only");
        });
    }

    [Test]
    [Description("Tests that create command throws when operations are not supported")]
    public async Task CreateCommandSave_SaveAsync_NotSupported()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider with no supported operations
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Read);

        // Attempt to create an create command, which should throw
        Assert.Throws<NotSupportedException>(
            () => dataProvider.Create(id: id, partitionKey: partitionKey),
            "The requested Create operation is not supported.");
    }

    [Test]
    [Description("Tests that the result returned from saving a create command is read-only")]
    public async Task CreateCommandSave_SaveAsync_ResultIsReadOnly()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

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

        Assert.That(created, Is.Not.Null);
        Assert.That(created.Item, Is.Not.Null);

        // Verify the result is read-only
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => created.Item.PublicMessage = "Public #2",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => created.Item.PrivateMessage = "Private #2",
                $"The '{typeof(ITestItem)}' is read-only");
        });
    }

    [Test]
    [Description("Tests that a create command cannot be saved more than once")]
    public async Task CreateCommandSave_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<ITestItem, TestItem>(
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
