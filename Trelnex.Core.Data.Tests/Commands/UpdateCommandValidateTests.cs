using FluentValidation;
using Snapshooter.NUnit;
using ValidationException = Trelnex.Core.Validation.ValidationException;

namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class UpdateCommandValidateTests
{
    [Test]
    [Description("Tests validation exception with multiple empty errors for public message")]
    public async Task UpdateCommandValidate_SaveAsync_EmptyPublicMessageWithTwoErrors()
    {
        // Setup validator with multiple rules for public message
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #1");
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set empty public message and valid private message
        createCommand.Item.PublicMessage = string.Empty;
        createCommand.Item.PrivateMessage = "Private #1";

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                cancellationToken: default))!;

        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests validation exception when both public and private messages are empty")]
    public async Task UpdateCommandValidate_SaveAsync_MissingPublicAndPrivateMessages()
    {
        // Setup validator requiring both public and private messages
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty();
        itemValidator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create a new command to create our test item (with default empty values)
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                cancellationToken: default))!;

        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests validation exception when private message is empty")]
    public async Task UpdateCommandValidate_SaveAsync_MissingPrivateMessage()
    {
        // Setup validator requiring private message not empty
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message empty
        createCommand.Item.PublicMessage = "Public #1";

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                cancellationToken: default))!;

        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests validation exception when public message is empty")]
    public async Task UpdateCommandValidate_SaveAsync_MissingPublicMessage()
    {
        // Setup validator requiring public message not empty
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only private message, leaving public message empty
        createCommand.Item.PrivateMessage = "Private #1";

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                cancellationToken: default))!;

        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests validation exception with multiple null errors for private message")]
    public async Task UpdateCommandValidate_SaveAsync_NullPrivateMessageWithTwoErrors()
    {
        // Setup validator with multiple rules for private message
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #1");
        itemValidator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message null
        createCommand.Item.PublicMessage = "Public #1";

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                cancellationToken: default))!;

        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests validation result with multiple empty errors for public message in update command")]
    public async Task UpdateCommandValidate_ValidateAsync_EmptyPublicMessageWithTwoErrors()
    {
        // Setup validator with multiple rules for public message
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #1");
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create and save a valid initial item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the initial item
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create an update command for the saved item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Set public message to empty to cause validation errors
        updateCommand.Item.PublicMessage = string.Empty;

        // Validate the update command and capture the results
        var validationResult = await updateCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when both public and private messages are missing in update command")]
    public async Task UpdateCommandValidate_ValidateAsync_MissingPublicAndPrivateMessages()
    {
        // Setup validator requiring both public and private messages
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty();
        itemValidator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create and save a valid initial item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the initial item
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create an update command for the saved item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Set both messages to null to cause validation errors
        updateCommand.Item.PublicMessage = null!;
        updateCommand.Item.PrivateMessage = null!;

        // Validate the update command and capture the results
        var validationResult = await updateCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when private message is missing in update command")]
    public async Task UpdateCommandValidate_ValidateAsync_MissingPrivateMessage()
    {
        // Setup validator requiring private message not empty
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create and save a valid initial item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the initial item
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create an update command for the saved item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Set private message to null to cause validation error
        updateCommand.Item.PrivateMessage = null!;

        // Validate the update command and capture the results
        var validationResult = await updateCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when public message is missing in update command")]
    public async Task UpdateCommandValidate_ValidateAsync_MissingPublicMessage()
    {
        // Setup validator requiring public message not empty
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create and save a valid initial item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the initial item
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create an update command for the saved item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Set public message to null to cause validation error
        updateCommand.Item.PublicMessage = null!;

        // Validate the update command and capture the results
        var validationResult = await updateCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result with multiple null errors for private message in update command")]
    public async Task UpdateCommandValidate_ValidateAsync_NullPrivateMessageWithTwoErrors()
    {
        // Setup validator with multiple rules for private message
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #1");
        itemValidator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create and save a valid initial item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the initial item
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create an update command for the saved item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Set private message to null to cause validation errors
        updateCommand.Item.PrivateMessage = null!;

        // Validate the update command and capture the results
        var validationResult = await updateCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
