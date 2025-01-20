namespace Trelnex.Core.Data.Tests.Commands;

public class DeleteCommandTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task DeleteCommand_SaveAsync_IsReadOnlyAfterSave()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                commandOperations: CommandOperations.Delete);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var deleteCommand = await commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => deleteCommand.Item.PublicMessage = "Public #2",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => deleteCommand.Item.PrivateMessage = "Private #2",
                $"The '{typeof(ITestItem)}' is read-only");
        });
    }

    [Test]
    public async Task DeleteCommand_SaveAsync_NotSupported()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                commandOperations: CommandOperations.None);

        Assert.ThrowsAsync<NotSupportedException>(
            async () => await commandProvider.DeleteAsync(id: id, partitionKey: partitionKey),
            "The requested Delete operation is not supported.");
    }

    [Test]
    public async Task DeleteCommand_SaveAsync_ResultIsReadOnly()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                commandOperations: CommandOperations.Delete);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var deleteCommand = await commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // save it and read it back
        var deleted = await deleteCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(deleted, Is.Not.Null);
        Assert.That(deleted.Item, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => deleted.Item.PublicMessage = "Public #3",
                $"The '{typeof(ITestItem)}' is read-only");

            Assert.Throws<InvalidOperationException>(
                () => deleted.Item.PrivateMessage = "Private #3",
                $"The '{typeof(ITestItem)}' is read-only");
        });
    }

    [Test]
    public async Task DeleteCommand_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                commandOperations: CommandOperations.Delete);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var deleteCommand = await commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // save it
        await deleteCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await deleteCommand.SaveAsync(
                requestContext: requestContext,
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }

}
