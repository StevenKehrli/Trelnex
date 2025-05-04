using FluentValidation;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class DeleteCommandValidateTests
{
    [Test]
    [Description("Tests validation result for a delete command")]
    public async Task DeleteCommandValidate_ValidateAsync_WithValidator()
    {
        // Setup validator requiring public message not empty
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator and delete operations
        var commandProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            validator: validator,
            commandOperations: CommandOperations.Create | CommandOperations.Delete);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set valid values for both messages
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the item first so we can delete it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // Create a delete command for the saved item
        var deleteCommand = await commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Validate the delete command and capture the results
        var validationResult = await deleteCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
