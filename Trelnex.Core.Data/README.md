# Trelnex.Core.Data

## Overview

`Trelnex.Core.Data` is a .NET library designed to provide essential data access operations in a simple manner while ensuring data integrity.

## Usage

The below examples demonstrate Create, Read, Update, Delete and Query using the `InMemoryCommandProvider<TInterface, TItem>`. In practice, the ASP.NET startup will inject a singleton of `ICommandProvider<TInterface, TItem>` for each requested type name.

## Create

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

## Read

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

## Update

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

## Delete

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

## Query

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

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.
