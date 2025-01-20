namespace Trelnex.Core.Data.Tests.Commands;

public class ReadCommandTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task ReadCommand_ReadAsync_ResultIsReadOnly()
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

        var read = await commandProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(read, Is.Not.Null);
        Assert.That(read!.Item, Is.Not.Null);

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
