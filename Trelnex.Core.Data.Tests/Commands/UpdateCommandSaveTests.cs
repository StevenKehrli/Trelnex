namespace Trelnex.Core.Data.Tests.Commands;

public class UpdateCommandTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task UpdateCommandSave_SaveAsync_IsReadOnlyAfterSave()
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

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var updateCommand = await commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // save it and read it back
        var updated = await updateCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated.Item, Is.Not.Null);

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
    public async Task UpdateCommandSave_SaveAsync_NotSupported()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                commandOperations: CommandOperations.None);

        Assert.ThrowsAsync<NotSupportedException>(
            async () => await commandProvider.UpdateAsync(id: id, partitionKey: partitionKey),
            "The requested Update operation is not supported.");
    }

    [Test]
    public async Task UpdateCommand_SaveAsync_ResultIsReadOnly()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                commandOperations: CommandOperations.Update);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var updateCommand = await commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // save it and read it back
        var updated = await updateCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated.Item, Is.Not.Null);

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
    public async Task UpdateCommandSave_SaveAsync_WhenAlreadySaved()
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

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var updateCommand = await commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // save it
        await updateCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await updateCommand.SaveAsync(
                requestContext: requestContext,
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }

}
