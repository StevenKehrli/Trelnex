using FluentValidation;
using Snapshooter.NUnit;
using ValidationException = Trelnex.Core.Validation.ValidationException;

namespace Trelnex.Core.Data.Tests.Commands;

public class BatchCommandValidateTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task BatchCommandValidate_SaveAsync_PrivateMessage()
    {
        var validator = new InlineValidator<TestItem>();
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

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        // save it - this will throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_SaveAsync_PrivateMessageTwoNullErrors()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #1");
        validator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #2");

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

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        // save it - this will throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_SaveAsync_PublicAndPrivateMessage()
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

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        // save it - this will throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_SaveAsync_PublicMessage()
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
                validator: validator);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PrivateMessage = "Private #1";

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        // save it - this will throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_SaveAsync_PublicMessageTwoEmptyErrors()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #1");
        validator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #2");

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

        createCommand.Item.PublicMessage = string.Empty;
        createCommand.Item.PrivateMessage = "Private #1";

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        // save it - this will throw a validation exception
        var ex = Assert.ThrowsAsync<ValidationException>(
            async () => await batchCommand.SaveAsync(
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
    public async Task BatchCommandValidate_ValidateAsync_PrivateMessage()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                validator: validator);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    public async Task BatchCommandValidate_ValidateAsync_PrivateMessageTwoNullErrors()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #1");
        validator.RuleFor(k => k.PrivateMessage).NotNull().WithMessage("NotNull #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                validator: validator);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = "Public #1";

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }


    [Test]
    public async Task BatchCommandValidate_ValidateAsync_PublicAndPrivateMessage()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();
        validator.RuleFor(k => k.PrivateMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                validator: validator);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    public async Task BatchCommandValidate_ValidateAsync_PublicMessage()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                validator: validator);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PrivateMessage = "Private #1";

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    public async Task BatchCommandValidate_ValidateAsync_PublicMessageTwoEmptyErrors()
    {
        var validator = new InlineValidator<TestItem>();
        validator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #1");
        validator.RuleFor(k => k.PublicMessage).NotEmpty().WithMessage("NotEmpty #2");

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName,
                validator: validator);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicMessage = string.Empty;
        createCommand.Item.PrivateMessage = "Private #1";

        var batchCommand = commandProvider.Batch();

        batchCommand.Add(createCommand);

        var validationResult = await batchCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
