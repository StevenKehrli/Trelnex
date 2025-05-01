using FluentValidation;
using Snapshooter.NUnit;
using ValidationException = Trelnex.Core.Validation.ValidationException;

namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class CreateCommandValidateTests
{
    [Test]
    [Description("Tests validation exception with multiple empty errors for public message")]
    public async Task CreateCommandValidate_SaveAsync_EmptyPublicMessageWithTwoErrors()
    {
        // Setup validator with multiple rules for public message
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #1");
        validator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set empty public message and valid private message
        createCommand.Item.PublicMessage = string.Empty;
        createCommand.Item.PrivateMessage = "Private #1";

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                requestContext: requestContext,
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
    public async Task CreateCommandValidate_SaveAsync_MissingPublicAndPrivateMessages()
    {
        // Setup validator requiring both public and private messages
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();
        validator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item (with default empty values)
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                requestContext: requestContext,
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
    public async Task CreateCommandValidate_SaveAsync_MissingPrivateMessage()
    {
        // Setup validator requiring private message not empty
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message empty
        createCommand.Item.PublicMessage = "Public #1";

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                requestContext: requestContext,
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
    public async Task CreateCommandValidate_SaveAsync_MissingPublicMessage()
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

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only private message, leaving public message empty
        createCommand.Item.PrivateMessage = "Private #1";

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                requestContext: requestContext,
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
    public async Task CreateCommandValidate_SaveAsync_NullPrivateMessageWithTwoErrors()
    {
        // Setup validator with multiple rules for private message
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #1");
        validator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message null
        createCommand.Item.PublicMessage = "Public #1";

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await createCommand.SaveAsync(
                requestContext: requestContext,
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
    [Description("Tests validation result with multiple empty errors for public message")]
    public async Task CreateCommandValidate_ValidateAsync_EmptyPublicMessageWithTwoErrors()
    {
        // Setup validator with multiple rules for public message
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #1");
        validator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set empty public message and valid private message
        createCommand.Item.PublicMessage = string.Empty;
        createCommand.Item.PrivateMessage = "Private #1";

        // Validate the command and capture the results
        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when both public and private messages are empty")]
    public async Task CreateCommandValidate_ValidateAsync_MissingPublicAndPrivateMessages()
    {
        // Setup validator requiring both public and private messages
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();
        validator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item (with default empty values)
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Validate the command and capture the results
        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when private message is empty")]
    public async Task CreateCommandValidate_ValidateAsync_MissingPrivateMessage()
    {
        // Setup validator requiring private message not empty
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message empty
        createCommand.Item.PublicMessage = "Public #1";

        // Validate the command and capture the results
        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when public message is empty")]
    public async Task CreateCommandValidate_ValidateAsync_MissingPublicMessage()
    {
        // Setup validator requiring public message not empty
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only private message, leaving public message empty
        createCommand.Item.PrivateMessage = "Private #1";

        // Validate the command and capture the results
        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result with multiple null errors for private message")]
    public async Task CreateCommandValidate_ValidateAsync_NullPrivateMessageWithTwoErrors()
    {
        // Setup validator with multiple rules for private message
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #1");
        validator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with validator
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                validator: validator);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message null
        createCommand.Item.PublicMessage = "Public #1";

        // Validate the command and capture the results
        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
