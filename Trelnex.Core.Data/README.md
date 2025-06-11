# Trelnex.Core.Data

`Trelnex.Core.Data` is a .NET library designed to provide essential data access operations in a simple manner while ensuring data integrity. It offers a strongly-typed API for Create, Read, Update, Delete (CRUD) operations with built-in tracking of data changes, validation support, optimistic concurrency control, and event logging.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Key Features

- **Interface-based programming** - Work with strongly-typed interfaces rather than concrete implementation details
- **Pluggable architecture** - Support for multiple backend data stores
- **Change tracking** - Automatically track changes to model properties using the `TrackChange` attribute (see [Tracking Changes with the TrackChangeAttribute](#tracking-changes-with-the-trackchangeattribute))
- **Property encryption** - Securely encrypt sensitive data with the `Encrypt` attribute
- **Event logging** - Generate audit events for all data modifications with property change history
- **Validation** - Built-in validation support via FluentValidation
- **Batch operations** - Perform multiple operations atomically with transaction-like semantics
- **Expression-based queries** - Rich querying capabilities with LINQ-like syntax
- **Optimistic concurrency** - Prevent conflicting updates using ETags
- **Soft deletion** - Items are marked as deleted rather than physically removed
- **In-memory provider** - Ready-to-use implementation for testing and prototyping

## Architecture

Trelnex.Core.Data is built around several core concepts:

### Command Pattern

The library uses the Command pattern to encapsulate data operations. Each command is an object that maintains its own state and handles validation and execution.

### Command Providers

Command providers are the main entry point for generating commands to interact with data. They implement the `ICommandProvider<TInterface>` interface and determine which operations are permitted through the `CommandOperations` flags enum:

- **Read** - Default permission, allows read operations only
- **Create** - Allows creation of new items
- **Update** - Allows modification of existing items
- **Delete** - Allows marking items as deleted
- **All** - Combination of Create, Update, and Delete

### Proxy System

A dynamic proxy system enables:
- Automatic enforcement of read-only protection for data that should not be modified
- Command objects that become read-only after execution to prevent reuse

### BaseItem and IBaseItem

All data entities inherit from `BaseItem` or implement `IBaseItem`, providing common properties like:
- **Id** - Unique identifier
- **PartitionKey** - Logical partition key that determines physical storage location
- **TypeName** - Discriminator that distinguishes between item types
- **CreatedDate** - When the item was created
- **UpdatedDate** - When the item was last updated
- **DeletedDate** - When the item was soft-deleted (null if not deleted)
- **IsDeleted** - Flag indicating if the item is deleted
- **ETag** - Version identifier for optimistic concurrency control

## Command Types

The library provides several command types:

- **ReadCommand** - Provides read-only access to items
- **SaveCommand** - Handles Create, Update, and Delete operations
- **QueryCommand** - Enables LINQ-style querying of collections
- **BatchCommand** - Executes multiple operations as a single atomic transaction

## Validation

The system includes comprehensive validation:

- **Base validation** - All items are validated for required base properties
- **Domain validation** - Custom validators can be provided for domain-specific rules
- **Pre-execution validation** - Commands validate before executing
- **Batch validation** - Batch commands validate all items before execution

## Change Tracking

See [Tracking Changes with the TrackChangeAttribute](#tracking-changes-with-the-trackchangeattribute).

## Usage

The below examples demonstrate Create, Read, Update, Delete and Query using the `InMemoryCommandProvider<TInterface, TItem>`. In practice, the ASP.NET startup will inject a singleton of `ICommandProvider<TInterface, TItem>` for each requested type name.

### Create

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// Create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName,
        commandOperations: CommandOperations.All); // Allow all operations

// Create an ISaveCommand<ITestItem> to create the item
using var createCommand = commandProvider.Create(
    id: id,
    partitionKey: partitionKey);

// Set the item properties (changes will be tracked if [TrackChange] is applied)
createCommand.Item.PublicMessage = "Public #1";
createCommand.Item.PrivateMessage = "Private #1";

// Save the item and get the IReadResult<ITestItem>
var requestContext = TestRequestContext.Create();

using var result = await createCommand.SaveAsync(
    requestContext: requestContext,
    cancellationToken: default);

// After SaveAsync, the command becomes read-only and can't be reused
// Both command and result are automatically disposed when they go out of scope
```

### Read

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// Create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName);

// Get the IReadResult<TItem>
using var result = await commandProvider.ReadAsync(
    id: id,
    partitionKey: partitionKey);

// Items from ReadAsync are always read-only
// Result is automatically disposed when it goes out of scope
```

### Update

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// Create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName,
        commandOperations: CommandOperations.All); // Need Update permission

// Create an ISaveCommand<ITestItem> to update the item
using var updateCommand = await commandProvider.UpdateAsync(
    id: id,
    partitionKey: partitionKey);

// If no item exists or it's deleted, UpdateAsync returns null

// Update the item properties (changes will be tracked if [TrackChange] is applied)
updateCommand.Item.PublicMessage = "Public #2";
updateCommand.Item.PrivateMessage = "Private #2";

// Save the item and get the IReadResult<ITestItem>
var requestContext = TestRequestContext.Create();

using var result = await updateCommand.SaveAsync(
    requestContext: requestContext,
    cancellationToken: default);

// ETag is automatically updated to prevent conflicts
// Both command and result are automatically disposed when they go out of scope
```

### Delete

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// Create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName,
        commandOperations: CommandOperations.All); // Need Delete permission

// Create an ISaveCommand<ITestItem> to delete the item
using var deleteCommand = await commandProvider.DeleteAsync(
    id: id,
    partitionKey: partitionKey);

// If no item exists or it's already deleted, DeleteAsync returns null

// Save the item (performs a soft delete by setting IsDeleted=true and DeletedDate)
var requestContext = TestRequestContext.Create();

using var result = await deleteCommand.SaveAsync(
    requestContext: requestContext,
    cancellationToken: default);

// Both command and result are automatically disposed when they go out of scope
```

### Query

```csharp
using Trelnex.Core.Data;
using Trelnex.Core.Disposables;

// Create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName);

// Build the query with LINQ expressions
var queryCommand = commandProvider.Query();
queryCommand.Where(i => i.PublicMessage == "Public #1");

// Get the items with automatic disposal management
using var results = await queryCommand.ToAsyncEnumerable().ToDisposableEnumerableAsync();

// All query results are automatically disposed when 'results' goes out of scope
// Soft-deleted items are automatically filtered out from queries
```

### Batch Operations

```csharp
using Trelnex.Core.Data;

// Create our ICommandProvider<ITestItem, TestItem>
var commandProvider =
    InMemoryCommandProvider.Create<ITestItem, TestItem>(
        typeName: _typeName,
        commandOperations: CommandOperations.All);

// Create a batch command
var batchCommand = commandProvider.Batch();

// Create commands to add to the batch (all must have the same partitionKey)
var createCommand1 = commandProvider.Create(
    id: "id1",
    partitionKey: partitionKey);
createCommand1.Item.PublicMessage = "Public #1";

var createCommand2 = commandProvider.Create(
    id: "id2",
    partitionKey: partitionKey);
createCommand2.Item.PublicMessage = "Public #2";

// Add commands to the batch
batchCommand.Add(createCommand1);
batchCommand.Add(createCommand2);

// Validate the batch (optional)
var validationResults = await batchCommand.ValidateAsync();
if (validationResults.Any(vr => !vr.IsValid))
{
    // handle validation errors
}

// Save the batch and get the array of IBatchResult<ITestItem>
var requestContext = TestRequestContext.Create();

var results = await batchCommand.SaveAsync(
    requestContext: requestContext,
    cancellationToken: default);

// If any operation fails, the entire batch is rolled back
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
    : CommandProvider<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    public CustomCommandProvider(
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        : base(typeName, validator, commandOperations)
    {
    }

    // Implement the required abstract methods
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Return a queryable for your data store
    }

    protected override IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // Execute the query against your data store
    }

    protected override Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        // Retrieve an item from your data store
    }

    protected override Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Save a batch of items to your data store
    }
}

public class CustomCommandProviderFactory : ICommandProviderFactory
{
    public ICommandProvider<TInterface, TItem> CreateCommandProvider<TInterface, TItem>(
        string typeName)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        return new CustomCommandProvider<TInterface, TItem>(
            typeName,
            validator: GetValidator<TItem>(),
            commandOperations: CommandOperations.All);
    }

    private IValidator<TItem>? GetValidator<TItem>()
    {
        // Return a validator for the item type, if available
    }
}
```

## Tracking Changes with the TrackChangeAttribute

The system captures the property name, old value, and new value for each change, providing a detailed audit trail.

To track changes to properties, decorate them with the `TrackChangeAttribute`:

```csharp
public interface ITestItem : IBaseItem
{
    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }

    TestSettings Settings { get; set; }
}

public class TestSettings
{
    [TrackChange]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [TrackChange]
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class TestItem : BaseItem, ITestItem
{
    [TrackChange]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = string.Empty;

     // changes will not be tracked for privateMessage
    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = string.Empty;

    [TrackChange]
    [JsonPropertyName("settings")]
    public TestSettings Settings { get; set; } = new();
}
```

### Change Tracking Features

The change tracking system provides comprehensive monitoring of data modifications:

- **JSON Pointer Paths** - Each change is recorded with a precise JSON Pointer path (e.g., `/customer/name`, `/orders/0/quantity`)
- **Nested Object Support** - Changes to nested objects and their properties are automatically tracked when both the parent property and child properties have `[TrackChange]`
- **Value Transitions** - Both old and new values are captured for each property change
- **Array and Collection Changes** - Modifications to arrays, lists, and dictionaries are tracked at the individual element level

### Hierarchical Tracking Rules

For nested objects and collections, the `[TrackChange]` attribute must be present at both levels:

1. **Parent Property** - The containing property must have `[TrackChange]`
2. **Child Properties** - Individual properties within nested objects must also have `[TrackChange]`

If the parent property lacks `[TrackChange]`, no changes within that object will be tracked, even if child properties have the attribute.

### Change Event Auditing

When properties decorated with `[TrackChange]` are modified, the system:

1. **Captures Change Details** - Records the property path, old value, and new value for each modification
2. **Generates Property Changes** - Creates an array of `PropertyChange` objects containing the audit trail
3. **Persists with Item Events** - Includes the property changes in the event object that is saved alongside the item

**Example Property Change:**
```json
{
  "PropertyName": "/settings/value",
  "OldValue": "old configuration",
  "NewValue": "new configuration"
}
```

### Security and Privacy Considerations

**Important:** Property change tracking captures and persists the actual values (both old and new) in the audit trail. This provides detailed auditing capabilities but may expose sensitive information:

- **Data Exposure** - Old and new values are stored in plain text in the event log
- **Compliance Risk** - May conflict with data privacy regulations (GDPR, CCPA) for sensitive data
- **Access Control** - Event logs may be accessible to administrators or auditors

**Recommendations:**
- Use `[TrackChange]` judiciously on sensitive properties
- Consider omitting the attribute from properties containing PII, credentials, or confidential data
- Implement appropriate access controls for event logs and audit trails

## Encrypting Properties with the EncryptAttribute

To encrypt sensitive data properties, decorate them with the `EncryptAttribute`:

```csharp
public interface ICustomerItem : IBaseItem
{
    string Name { get; set; }

    string SocialSecurityNumber { get; set; }

    string CreditCardNumber { get; set; }
}

public class CustomerItem : BaseItem, ICustomerItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Encrypt]
    [JsonPropertyName("ssn")]
    public string SocialSecurityNumber { get; set; } = string.Empty;

    [Encrypt]
    [JsonPropertyName("creditCard")]
    public string CreditCardNumber { get; set; } = string.Empty;
}
```

Properties marked with `[Encrypt]` are automatically encrypted before storage and decrypted when retrieved. This ensures that sensitive data remains protected at rest in the data store.

### Encryption and Change Tracking Interaction

**Important:** When both `[Encrypt]` and `[TrackChange]` are specified on a property:

- **Storage** - The property value is encrypted when saved to the data store
- **Change Tracking** - Property changes are captured with **unencrypted** old and new values in the audit trail
- **Security Risk** - This exposes sensitive data in plain text within the event logs

**Recommendations for Sensitive Data:**
- **Use only `[Encrypt]`** - For maximum security, omit `[TrackChange]` on encrypted properties
- **Risk Assessment** - If audit trails are required, ensure event logs have appropriate access controls
- **Compliance** - Consider data privacy regulations when tracking changes to encrypted properties

The `EncryptionService` uses:
- Authenticated encryption with AES-GCM
- HKDF for secure key derivation
- Random salt and IV generation for each encryption operation
- 256-bit encryption keys

This provides transparent encryption for sensitive data without requiring changes to your application's business logic.

## Optimistic Concurrency Control

The system uses ETags to implement optimistic concurrency control:

1. When an item is read, it includes an ETag value
2. When the item is updated, the ETag is checked against the current value in the store
3. If the ETags don't match, the update fails with a PreconditionFailed status
4. When an update succeeds, a new ETag is generated

This prevents conflicting updates to the same data when multiple users or processes are working concurrently.
