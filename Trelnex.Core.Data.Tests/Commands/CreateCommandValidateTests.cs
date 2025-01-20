using FluentValidation;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Commands;

public class CreateCommandValidateTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task CreateCommandValidate_SaveAsync_PrivateMessage()
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

        // save it - this will throw a validation exception
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
    public async Task CreateCommandValidate_SaveAsync_PrivateMessageTwoNullErrors()
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

        // save it - this will throw a validation exception
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
    public async Task CreateCommandValidate_SaveAsync_PublicAndPrivateMessage()
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

        // save it - this will throw a validation exception
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
    public async Task CreateCommandValidate_SaveAsync_PublicMessage()
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

        // save it - this will throw a validation exception
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
    public async Task CreateCommandValidate_SaveAsync_PublicMessageTwoEmptyErrors()
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

        // save it - this will throw a validation exception
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
    public async Task CreateCommandValidate_ValidateAsync_PrivateMessage()
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

        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    public async Task CreateCommandValidate_ValidateAsync_PrivateMessageTwoNullErrors()
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

        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }


    [Test]
    public async Task CreateCommandValidate_ValidateAsync_PublicAndPrivateMessage()
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

        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    public async Task CreateCommandValidate_ValidateAsync_PublicMessage()
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

        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }

    [Test]
    public async Task CreateCommandValidate_ValidateAsync_PublicMessageTwoEmptyErrors()
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

        var validationResult = await createCommand.ValidateAsync(default);

        Snapshot.Match(validationResult);
    }
}
