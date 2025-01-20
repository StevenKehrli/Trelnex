using FluentValidation;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Commands;

public class ReadCommandValidateTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task ReadCommand_ValidateAsync()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();
        validator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                validator: validator);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var readResult = await commandProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(readResult, Is.Not.Null);
        Assert.That(readResult.Item, Is.Not.Null);

        var validationResult = await readResult.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
