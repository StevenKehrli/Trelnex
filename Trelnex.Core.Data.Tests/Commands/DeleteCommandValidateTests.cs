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
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator and delete operations
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Delete);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set valid values for both messages
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the item first so we can delete it
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the saved item
        using var deleteCommand = await dataProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Validate the delete command and capture the results
        var validationResult = await deleteCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
