using FluentValidation;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Commands;

public class DeleteCommandValidateTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task DeleteCommand_ValidateAsync()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                validator: validator,
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

        var validationResult = await deleteCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
