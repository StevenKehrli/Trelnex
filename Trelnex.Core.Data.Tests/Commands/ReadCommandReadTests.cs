namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class ReadCommandReadTests
{
    [Test]
    [Description("Tests that the result returned from reading an item is read-only")]
    public async Task ReadCommandRead_ReadAsync_ResultIsReadOnly()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type
        var commandProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the item first so we can read it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // Read the saved item
        var read = await commandProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(read, Is.Not.Null);
        Assert.That(read!.Item, Is.Not.Null);

        // Verify the result is read-only
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => read.Item.PublicMessage = "Public #2",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => read.Item.PrivateMessage = "Private #2",
                $"The '{typeof(ITestItem)}' is read-only");
        });
    }
}
