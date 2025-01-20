namespace Trelnex.Core.Data.Tests.Commands;

public class BatchCommandSaveTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task BatchCommand_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        // save it
        await batchCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await createCommand.SaveAsync(
                requestContext: requestContext,
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await batchCommand.SaveAsync(
                requestContext: requestContext,
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }
}
