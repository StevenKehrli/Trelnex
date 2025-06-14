using FluentValidation;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class ReadCommandValidateTests
{
    [Test]
    [Description("Tests validation result for a read command with valid data")]
    public async Task ReadCommandValidate_ValidateAsync_WithValidator()
    {
        // Setup validator requiring both messages not empty
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();
        validator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            validator: validator,
            commandOperations: CommandOperations.Create);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set valid values for both messages
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the item first so we can read it
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Read the saved item
        using var readResult = await dataProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(readResult, Is.Not.Null);
        Assert.That(readResult.Item, Is.Not.Null);

        // Validate the read command and capture the results
        var validationResult = await readResult.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
