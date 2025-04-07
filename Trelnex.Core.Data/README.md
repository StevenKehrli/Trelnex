# Trelnex.Core.Data

`Trelnex.Core.Data` is a .NET library designed to provide essential data access operations in a simple manner while ensuring data integrity. It offers a strongly-typed API for Create, Read, Update, Delete (CRUD) operations with built-in tracking of data changes, validation support, and event logging.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Key Features

- **Interface-based programming** - Work with strongly-typed interfaces rather than concrete implementation details
- **Change tracking** - Automatically track changes to model properties using the `TrackChange` attribute
- **Event logging** - Generate audit events for all data modifications
- **Validation** - Built-in validation support via FluentValidation
- **Batch operations** - Perform multiple operations atomically
- **Expression-based queries** - Rich querying capabilities with LINQ-like syntax
- **In-memory provider** - Ready-to-use implementation for testing and prototyping

## Architecture

Trelnex.Core.Data is built around several core concepts:

### Command Providers

The library uses the Command pattern to encapsulate data operations. Command providers are the main entry point for generating commands to interact with data.

### Proxy System

A dynamic proxy system enables:
- Property change tracking
- Read-only protection for data that shouldn't be modified
- Consistent interface regardless of the underlying data store

### BaseItem and IBaseItem

All data entities inherit from `BaseItem` or implement `IBaseItem`, providing common properties like:
- Id
- PartitionKey
- TypeName
- CreatedDate
- UpdatedDate
- DeletedDate
- IsDeleted
- ETag

## Usage

The below examples demonstrate Create, Read, Update, Delete and Query using the `InMemoryCommandProvider<TInterface, TItem>`. In practice, the ASP.NET startup will inject a singleton of `ICommandProvider<TInterface, TItem>` for each requested type name.

### Create

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName);

// create an ISaveCommand<ITestItem> to create the item
var createCommand = commandProvider.Create(
    id: id,
    partitionKey: partitionKey);

// set the item properties
createCommand.Item.PublicMessage = "Public #1";
createCommand.Item.PrivateMessage = "Private #1";

// save the item and get the IReadResult<ITestItem>
var requestContext = TestRequestContext.Create();

var result = await createCommand.SaveAsync(
    requestContext: requestContext,
    cancellationToken: default);
```

### Read

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName);

// get the IReadResult<TItem>
var result = await commandProvider.ReadAsync(
    id: id,
    partitionKey: partitionKey);
```

### Update

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName);

// create an ISaveCommand<ITestItem> to create the item
var updateCommand = await commandProvider.UpdateAsync(
    id: id,
    partitionKey: partitionKey);

// update the item properties
updateCommand.Item.PublicMessage = "Public #2";
updateCommand.Item.PrivateMessage = "Private #2";

// save the item and get the IReadResult<ITestItem>
var requestContext = TestRequestContext.Create();

var result = await updateCommand.SaveAsync(
    requestContext: requestContext,
    cancellationToken: default);
```

### Delete

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName);

// create an ISaveCommand<ITestItem> to delete the item
var deleteCommand = await commandProvider.DeleteAsync(
    id: id,
    partitionKey: partitionKey);

// save the item and get the IReadResult<ITestItem>
var requestContext = TestRequestContext.Create();

var result = await deleteCommand.SaveAsync(
    requestContext: requestContext,
    cancellationToken: default);
```

### Query

```csharp
using Trelnex.Core.Data;

// create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName);

// query
var queryCommand = commandProvider.Query();
queryCommand.Where(i => i.PublicMessage == "Public #1");

// get the items as an array of IReadResult<TItem>
var results = await queryCommand.ToAsyncEnumerable().ToArrayAsync();
```

### Batch Operations

```csharp
using Trelnex.Core.Data;

// create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName);

// create a batch command
var batchCommand = commandProvider.Batch();

// create commands to add to the batch
var createCommand1 = commandProvider.Create(
    id: "id1",
    partitionKey: partitionKey);
createCommand1.Item.PublicMessage = "Public #1";

var createCommand2 = commandProvider.Create(
    id: "id2",
    partitionKey: partitionKey);
createCommand2.Item.PublicMessage = "Public #2";

// add commands to the batch
batchCommand.Add(createCommand1);
batchCommand.Add(createCommand2);

// save the batch and get the array of IBatchResult<ITestItem>
var requestContext = TestRequestContext.Create();

var results = await batchCommand.SaveAsync(
    requestContext: requestContext,
    cancellationToken: default);
```

## Dependency Injection

To use Trelnex.Core.Data with dependency injection in ASP.NET Core:

```csharp
using Trelnex.Core.Data;

// Register the command provider factory
services.AddCommandProviderFactory(
    new YourCommandProviderFactory());

// Optional - register specific command providers
services.AddSingleton<ICommandProvider<IUser, User>>(
    serviceProvider => serviceProvider.GetRequiredService<ICommandProviderFactory>()
        .CreateCommandProvider<IUser, User>("user"));
```

## Creating Custom Command Providers

To implement your own data store, create a custom command provider:

```csharp
public class CustomCommandProvider<TInterface, TItem>
    : CommandProviderBase<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    // Implement the abstract methods from CommandProviderBase
}

public class CustomCommandProviderFactory : ICommandProviderFactory
{
    public ICommandProvider<TInterface, TItem> CreateCommandProvider<TInterface, TItem>(string typeName)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface
    {
        return new CustomCommandProvider<TInterface, TItem>(typeName);
    }
}
```
