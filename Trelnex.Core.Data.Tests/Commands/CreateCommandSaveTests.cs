namespace Trelnex.Core.Data.Tests.Commands;

public class CreateCommandSaveTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task CreateCommand_SaveAsync_IsReadOnlyAfterSave()
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
    public async Task CreateCommand_SaveAsync_ResultIsReadOnly()
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

        // save it and read it back
        var created = await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);
        Assert.That(created.Item, Is.Not.Null);

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
    public async Task CreateCommand_SaveAsync_WhenAlreadySaved()
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

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await createCommand.SaveAsync(
                requestContext: requestContext,
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }
}
