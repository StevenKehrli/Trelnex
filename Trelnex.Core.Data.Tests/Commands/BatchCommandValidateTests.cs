using FluentValidation;
using Snapshooter.NUnit;
using ValidationException = Trelnex.Core.Validation.ValidationException;

namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class BatchCommandValidateTests
{
    [Test]
    [Description("Tests validation exception with multiple empty errors for public message")]
    public async Task BatchCommandValidate_SaveAsync_EmptyPublicMessageWithTwoErrors()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set empty public message and valid private message
        createCommand.Item.PublicMessage = string.Empty;
        createCommand.Item.PrivateMessage = "Private #1";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_SaveAsync_MissingPublicAndPrivateMessages()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item (with default empty values)
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_SaveAsync_MissingPrivateMessage()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message empty
        createCommand.Item.PublicMessage = "Public #1";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_SaveAsync_MissingPublicMessage()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only private message, leaving public message empty
        createCommand.Item.PrivateMessage = "Private #1";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_SaveAsync_NullPrivateMessageWithTwoErrors()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message null
        createCommand.Item.PublicMessage = "Public #1";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Save it - this should throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    [Description("Tests validation exception with wrong partition key for batch command during save")]
    public async Task BatchCommandValidate_SaveAsync_WrongPartitionKey()
    {
        // Setup validator with multiple rules for public message
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty");

        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = "a15b53a6-1c81-4285-adba-779145fd00b0";

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = "823bcd49-bb5b-4cb3-8a3a-f678cf03a78a";

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand1 = dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set the public message
        createCommand1.Item.PublicMessage = "Public #1";

        // Create a new command to create our test item
        using var createCommand2 = dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set the public message
        createCommand2.Item.PublicMessage = "Public #2";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand1);
        batchCommand.Add(createCommand2);

        // Attempt to save the batch command again, which should also throw
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    [Description("Tests validation exception with wrong partition key for batch command during save")]
    public async Task BatchCommandValidate_SaveAsync_WrongPartitionKeyAndErrors()
    {
        // Setup validator with multiple rules for public message
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty");

        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = "c5b3eba8-d933-45c6-b9a2-c0cd86f2f6c5";

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = "748af380-281b-4d9f-b375-56d62b4f34b6";

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand1 = dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set the public message
        createCommand1.Item.PublicMessage = "Public #1";

        // Create a new command to create our test item
        using var createCommand2 = dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set empty public message
        createCommand2.Item.PublicMessage = string.Empty;

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand1);
        batchCommand.Add(createCommand2);

        // Attempt to save the batch command again, which should also throw
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_ValidateAsync_EmptyPublicMessageWithTwoErrors()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set empty public message and valid private message
        createCommand.Item.PublicMessage = string.Empty;
        createCommand.Item.PrivateMessage = "Private #1";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Validate the batch command and capture the results
        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when both public and private messages are empty")]
    public async Task BatchCommandValidate_ValidateAsync_MissingPublicAndPrivateMessages()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item (with default empty values)
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Validate the batch command and capture the results
        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when private message is empty")]
    public async Task BatchCommandValidate_ValidateAsync_MissingPrivateMessage()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message empty
        createCommand.Item.PublicMessage = "Public #1";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Validate the batch command and capture the results
        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result when public message is empty")]
    public async Task BatchCommandValidate_ValidateAsync_MissingPublicMessage()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only private message, leaving public message empty
        createCommand.Item.PrivateMessage = "Private #1";

        // Create a batch command and add our create Command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Validate the batch command and capture the results
        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result with multiple null errors for private message")]
    public async Task BatchCommandValidate_ValidateAsync_NullPrivateMessageWithTwoErrors()
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
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set only public message, leaving private message null
        createCommand.Item.PublicMessage = "Public #1";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Validate the batch command and capture the results
        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result with wrong partition key for batch command")]
    public async Task BatchCommandValidate_ValidateAsync_WrongPartitionKey()
    {
        // Setup validator with multiple rules for public message
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty");

        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = "2950dbd7-e46b-4185-b514-af373e54abac";

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = "59cdc22b-04b2-46de-a90f-04b1ed0b6a62";

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand1 = dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set the public message
        createCommand1.Item.PublicMessage = "Public #1";

        // Create a new command to create our test item
        using var createCommand2 = dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set the public message
        createCommand2.Item.PublicMessage = "Public #2";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand1);
        batchCommand.Add(createCommand2);

        // Validate it - this should return a validation result with errors
        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    [Description("Tests validation result with wrong partition key for batch command")]
    public async Task BatchCommandValidate_ValidateAsync_WrongPartitionKeyAndErrors()
    {
        // Setup validator with multiple rules for public message
        var itemValidator = new InlineValidator<TestItem>();
        itemValidator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty");

        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = "dfbdcb72-c5bb-41e6-b234-d0915d7d3a0a";

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = "a87d1e2e-c67f-402f-8780-092a8225f80f";

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type with validator
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            itemValidator: itemValidator,
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand1 = dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set the public message
        createCommand1.Item.PublicMessage = "Public #1";

        // Create a new command to create our test item
        using var createCommand2 = dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set empty public message
        createCommand2.Item.PublicMessage = string.Empty;

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand1);
        batchCommand.Add(createCommand2);

        // Validate it - this should return a validation result with errors
        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
