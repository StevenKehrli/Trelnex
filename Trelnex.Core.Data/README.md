# Trelnex.Core.Data

`Trelnex.Core.Data` is a .NET library designed to provide essential data access operations in a simple manner while ensuring data integrity. It offers a strongly-typed API for Create, Read, Update, Delete (CRUD) operations with built-in tracking of data changes, validation support, optimistic concurrency control, event sourcing, and comprehensive audit trails.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Key Features

- **Interface-based programming** - Work with strongly-typed interfaces rather than concrete implementation details
- **Pluggable architecture** - Support for multiple backend data stores (in-memory, database via LinqToDB)
- **Change tracking** - Automatically track changes to model properties using the `Track` attribute with JSON Pointer paths
- **Property encryption** - Securely encrypt sensitive data with field-level encryption using AES-GCM
- **Event sourcing** - Complete audit trail with W3C trace context integration for all data modifications
- **Thread-safe operations** - All operations are thread-safe with proper concurrency control
- **Validation** - Built-in validation support via FluentValidation with composite validators
- **Batch operations** - Perform multiple operations atomically with full ACID transaction semantics
- **LINQ querying** - Full LINQ support with async streaming capabilities
- **Optimistic concurrency** - Prevent conflicting updates using ETags with automatic version management
- **Soft deletion** - Items are marked as deleted rather than physically removed
- **JSON-based serialization** - Efficient JSON processing with System.Text.Json
- **Memory management** - Proper resource disposal with IDisposable pattern throughout

## Architecture

Trelnex.Core.Data is built around several core concepts:

### Data Provider Interface and Command Pattern

The library uses the Command Pattern to encapsulate data operations. Each command is an object that maintains its own state and handles validation and execution.

### Data Providers

Data providers are the main entry point for generating commands to interact with data. They implement the `IDataProvider<TItem>` interface and determine which operations are permitted through the `CommandOperations` flags enum:

- **Read** - No operations allowed (value = 0)
- **Create** - Allows creation of new items
- **Update** - Allows modification of existing items
- **Delete** - Allows marking items as deleted
- **All** - Combination of Create, Update, and Delete

### Command Lifecycle Management and Thread Safety

The library uses command invalidation and thread-safe item management:
- Commands become invalid after execution by nulling their internal delegates
- This prevents accidental reuse of executed commands
- Thread-safe operations are ensured through `ItemManager<TItem>` with semaphore-based concurrency control
- `ItemManager<TItem>` is a crucial base class that provides thread-safe access to items using `SemaphoreSlim`
- All commands (`SaveCommand`, `ReadResult`, `QueryResult`) inherit from `ItemManager` for consistent thread safety

### BaseItem

All data entities inherit from `BaseItem` (a record type), providing common properties:
- **Id** - Unique identifier (string)
- **PartitionKey** - Logical partition key that determines physical storage location
- **TypeName** - Discriminator that distinguishes between item types
- **Version** - Integer version that increments with each update for schema evolution
- **CreatedDateTimeOffset** - When the item was created (DateTimeOffset)
- **UpdatedDateTimeOffset** - When the item was last updated (DateTimeOffset)
- **DeletedDateTimeOffset** - When the item was soft-deleted (null if not deleted)
- **IsDeleted** - Nullable boolean flag indicating if the item is deleted (null = not deleted, true = deleted)
- **ETag** - Version identifier for optimistic concurrency control

## Command Types

The library provides several command types:

- **SaveCommand** - Thread-safe command for Create, Update, and Delete operations with validation
- **QueryCommand** - LINQ-style querying with async streaming and materialization options
- **BatchCommand** - Executes multiple operations as a single atomic transaction with rollback support
- **ReadResult** - Immutable result wrapper for read operations with automatic disposal
- **SaveResult** - Result wrapper containing the saved item and associated event data
- **BatchResult** - Array of results from batch operations with success/failure tracking

## Validation

The system includes comprehensive validation:

- **Base validation** - All items are validated for required base properties (Id, PartitionKey, TypeName)
- **Domain validation** - Custom FluentValidation validators can be provided for domain-specific rules
- **Composite validation** - Combines base and domain validators automatically
- **Pre-execution validation** - Commands validate before executing operations
- **Batch validation** - Batch commands validate all items before execution with detailed error reporting
- **Thread-safe validation** - All validation operations are thread-safe

## Change Tracking and Event Sourcing

The system provides comprehensive change tracking and event sourcing capabilities:

- **JSON Pointer Paths** - Changes are tracked using precise JSON Pointer paths (RFC 6901)
- **Property-level tracking** - Individual property changes with old and new values
- **Nested object support** - Deep tracking of complex object hierarchies
- **Thread-safe operations** - All change tracking is thread-safe using semaphores
- **W3C Trace Context** - Full integration with distributed tracing standards
- **Event persistence** - All changes generate immutable ItemEvent records
- **Audit trail** - Complete history of all operations for compliance and debugging

See [Tracking Changes with the TrackAttribute](#tracking-changes-with-the-trackattribute) for detailed configuration.

## Usage

The below examples demonstrate Create, Read, Update, Delete and Query using the `InMemoryDataProvider<TItem>`. In practice, data providers are created through factories and can be used as singletons. All operations are thread-safe and support cancellation tokens.

### Create

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// Create data provider through factory
var factory = new InMemoryDataProviderFactory();
var dataProvider = factory.Create<TestItem>(
    typeName: "TestItem",
    commandOperations: CommandOperations.All);

// Create a save command for new item
using var createCommand = dataProvider.Create(
    id: id,
    partitionKey: partitionKey);

// Set the item properties through the managed item
createCommand.Item.PublicMessage = "Public #1";
createCommand.Item.PrivateMessage = "Private #1";

// Save the item
using var result = await createCommand.SaveAsync(cancellationToken: default);

// Command becomes invalid after execution
// Result contains the saved item and generated event
// All resources are automatically disposed
```

### Read

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// Create data provider
var factory = new InMemoryDataProviderFactory();
var dataProvider = factory.Create<TestItem>(typeName: "TestItem");

// Read item by id and partition key
using var result = await dataProvider.ReadAsync(
    id: id,
    partitionKey: partitionKey,
    cancellationToken: default);

// Items from ReadAsync are immutable
// Returns null if item doesn't exist or is soft-deleted
// Result automatically disposed when it goes out of scope
```

### Update

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// Create data provider with update permissions
var factory = new InMemoryDataProviderFactory();
var dataProvider = factory.Create<TestItem>(
    typeName: "TestItem",
    commandOperations: CommandOperations.All);

// Create update command (loads existing item)
using var updateCommand = await dataProvider.UpdateAsync(
    id: id,
    partitionKey: partitionKey,
    cancellationToken: default);

// Returns null if item doesn't exist or is soft-deleted
if (updateCommand == null) return;

// Update properties through the managed item
updateCommand.Item.PublicMessage = "Public #2";
updateCommand.Item.PrivateMessage = "Private #2";

// Save with optimistic concurrency control
using var result = await updateCommand.SaveAsync(cancellationToken: default);

// ETag automatically updated for concurrency control
// Change events generated for all tracked properties
```

### Delete

```csharp
using Trelnex.Core.Data;

var id = "0346bbe4-0154-449f-860d-f3c1819aa174";
var partitionKey = "c8a6b519-3323-4bcb-9945-ab30d8ff96ff";

// Create data provider with delete permissions
var factory = new InMemoryDataProviderFactory();
var dataProvider = factory.Create<TestItem>(
    typeName: "TestItem",
    commandOperations: CommandOperations.All);

// Create delete command (loads existing item)
using var deleteCommand = await dataProvider.DeleteAsync(
    id: id,
    partitionKey: partitionKey,
    cancellationToken: default);

// Returns null if item doesn't exist or already soft-deleted
if (deleteCommand == null) return;

// Perform soft delete (sets DeletedDateTimeOffset, IsDeleted becomes true)
using var result = await deleteCommand.SaveAsync(cancellationToken: default);

// Item marked as deleted, not physically removed
// Delete event generated for audit trail
```

### Query

```csharp
using Trelnex.Core.Data;
using Trelnex.Core.Disposables;

// Create data provider
var factory = new InMemoryDataProviderFactory();
var dataProvider = factory.Create<TestItem>(typeName: "TestItem");

// Build LINQ query with full expression support
var queryCommand = dataProvider.Query()
    .Where(i => i.PublicMessage.Contains("Public"))
    .OrderBy(i => i.CreatedDateTimeOffset)
    .Skip(10)
    .Take(20);

// Option 1: Async streaming enumeration (memory efficient)
using var asyncResults = queryCommand.ToAsyncDisposableEnumerable();
await foreach (var item in asyncResults)
{
    // Process items as they're loaded
}

// Option 2: Materialized collection (random access)
using var materializedResults = await queryCommand.ToDisposableEnumerableAsync(cancellationToken);
foreach (var item in materializedResults)
{
    // All items pre-loaded into memory
}

// Soft-deleted items automatically filtered out
// All query results are immutable
```

### Batch Operations

```csharp
using Trelnex.Core.Data;

// Create data provider
var dataProvider = InMemoryDataProviderFactory.Create<TestItem>(
    typeName: "TestItem",
    commandOperations: CommandOperations.All);

// Create atomic batch command
var batchCommand = dataProvider.Batch();

// Create individual commands (must share same partition key)
var createCommand1 = dataProvider.Create("id1", partitionKey);
createCommand1.Item.PublicMessage = "Batch Item 1";

var createCommand2 = dataProvider.Create("id2", partitionKey);
createCommand2.Item.PublicMessage = "Batch Item 2";

// Add commands to batch
batchCommand.Add(createCommand1);
batchCommand.Add(createCommand2);

// Optional validation before execution
var validationResults = await batchCommand.ValidateAsync(cancellationToken);
if (validationResults.Any(result => !result.IsValid))
{
    // Handle validation failures
    return;
}

// Execute batch atomically
var batchResults = await batchCommand.SaveAsync(cancellationToken: cancellationToken);

// All operations succeed or all are rolled back
// Each result contains saved item and event data
```

## Creating Custom Data Providers

To implement your own data store, create a custom data provider by inheriting from `DataProvider<TItem>`:

```csharp
/// <summary>
/// Custom data provider implementation for your specific data store
/// </summary>
public class CustomDataProvider<TItem>
    : DataProvider<TItem>
    where TItem : BaseItem, new()
{
    public CustomDataProvider(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null)
        : base(typeName, itemValidator, commandOperations)
    {
    }

    /// <summary>
    /// Create queryable for LINQ operations
    /// </summary>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Return IQueryable for your data store
        // This enables LINQ expressions to be translated to your query language
    }

    /// <summary>
    /// Execute the built query and return results
    /// </summary>
    protected override IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // Execute the query against your data store
        // Apply any necessary translations and return materialized results
    }

    /// <summary>
    /// Read a single item by id and partition key
    /// </summary>
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        // Retrieve item from your data store
        // Return null if not found or soft-deleted
    }

    /// <summary>
    /// Save multiple items atomically with full ACID semantics
    /// </summary>
    protected override async Task<SaveResult<TItem>[]> SaveBatchAsync(
        SaveRequest<TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Implement atomic batch save with transaction support
        // Generate events for each operation
        // Handle optimistic concurrency with ETag validation
        // Return results with saved items and events
    }
}

/// <summary>
/// Factory for creating custom data provider instances
/// </summary>
public class CustomDataProviderFactory : IDataProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CustomDataProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<DataProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return DataProviderFactoryStatus.Ready;
    }

    public IDataProvider<TItem> Create<TItem>(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null)
        where TItem : BaseItem, new()
    {
        var validator = itemValidator ?? GetValidator<TItem>();

        return new CustomDataProvider<TItem>(
            typeName,
            itemValidator: validator,
            commandOperations: commandOperations ?? CommandOperations.Read);
    }

    private IValidator<TItem>? GetValidator<TItem>()
    {
        // Resolve validator from DI container or create composite validator
        return _serviceProvider.GetService<IValidator<TItem>>();
    }
}
```

## Tracking Changes with the TrackAttribute

The system provides sophisticated change tracking using JSON Pointer paths (RFC 6901) to capture precise property changes. Changes are detected through deep JSON comparison and recorded with old and new values for complete audit trails.

To enable change tracking on properties, decorate them with the `[Track]` attribute:

```csharp
public interface ITestItem
{
    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }

    TestSettings Settings { get; set; }
}

public class TestSettings
{
    [Track]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Track]
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class TestItem : BaseItem, ITestItem
{
    [Track]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = string.Empty;

    // Changes will NOT be tracked for privateMessage (no [Track] attribute)
    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = string.Empty;

    [Track]
    [JsonPropertyName("settings")]
    public TestSettings Settings { get; set; } = new();
}
```

### Change Tracking Features

The change tracking system provides comprehensive monitoring of data modifications:

- **JSON Pointer Paths** - Each change is recorded with a precise JSON Pointer path (RFC 6901) such as `/settings/value` or `/items/0/quantity`
- **Deep Object Tracking** - Changes to nested objects and their properties are automatically tracked using recursive JSON comparison
- **Thread-Safe Operations** - All change detection uses semaphores and thread-safe patterns for concurrent access
- **Value Capture** - Both old and new values are captured as `dynamic?` types and stored for complete audit trails
- **Complex Type Support** - Arrays, lists, dictionaries, and custom objects are fully supported
- **Efficient Comparison** - Uses JsonNode-based deep comparison for optimal performance
- **DoNotTrack Attribute** - Use `[DoNotTrack]` to explicitly exclude properties from tracking

### Hierarchical Tracking Rules

For nested objects and collections, tracking follows these rules:

1. **Parent Property** - The containing property must have `[Track]` to enable tracking of the nested object
2. **Child Properties** - Individual properties within nested objects must also have `[Track]` to be included in change detection
3. **Deep Traversal** - The system recursively traverses the object graph using JSON serialization to detect changes
4. **Path Resolution** - JSON Pointer paths are automatically generated for all tracked nested properties

If a parent property lacks `[Track]`, no changes within that object are tracked, regardless of child property attributes.

### Change Event Auditing

When properties decorated with `[Track]` are modified, the system:

1. **Captures Change Details** - Records the JSON Pointer path, old value, and new value for each modification
2. **Generates Property Changes** - Creates an array of `PropertyChange` records with complete audit information
3. **Thread-Safe Processing** - Uses semaphores to ensure thread-safe change detection and recording
4. **Event Integration** - Includes property changes in `ItemEvent` records with W3C trace context
5. **Immutable Events** - All event data is immutable once generated to prevent tampering

**Example Property Change:**
```json
{
  "propertyName": "/settings/value",
  "oldValue": "old configuration",
  "newValue": "new configuration"
}
```

**Event Structure:**
```json
{
  "id": "event-id",
  "partitionKey": "partition-key",
  "relatedId": "item-id",
  "relatedTypeName": "TestItem",
  "saveAction": "Update",
  "changes": [...], // Array of PropertyChange objects
  "traceContext": "00-trace-id-span-id-01",
  "createdDateTimeOffset": "2024-01-01T12:00:00Z"
}
```

### Security and Privacy Considerations

**Important:** Property change tracking captures and persists the actual values (both old and new) in the audit trail. This provides detailed auditing capabilities but may expose sensitive information:

- **Data Exposure** - Old and new values are stored in plain text in the event log
- **Compliance Risk** - May conflict with data privacy regulations (GDPR, CCPA) for sensitive data
- **Access Control** - Event logs may be accessible to administrators or auditors

**Recommendations:**
- Use `[Track]` judiciously on sensitive properties
- Consider omitting the attribute from properties containing PII, credentials, or confidential data
- Implement appropriate access controls for event logs and audit trails

## Field-Level Encryption (Future Feature)

The library is designed to support field-level encryption capabilities through pluggable `IBlockCipherService` implementations. This feature is planned for future releases and will provide:

- **AES-GCM Encryption** - Authenticated encryption with associated data (AEAD)
- **Transparent Operation** - Automatic encryption/decryption during serialization
- **Security Attributes** - Property-level encryption markers
- **LINQ Compatibility** - Full query support on unencrypted properties

*Note: Encryption features are not currently implemented in the codebase.*

## Optimistic Concurrency Control

The system implements robust optimistic concurrency control using ETags:

### ETag Management

1. **ETag Generation** - Each item receives a unique ETag when created or updated
2. **Automatic Validation** - ETags are automatically checked during update operations
3. **Conflict Detection** - Mismatched ETags indicate concurrent modifications
4. **Failure Handling** - Concurrency violations throw exceptions with detailed error information
5. **Version Tracking** - ETags provide implicit versioning for all items

### Concurrency Flow

```csharp
// Read item (includes current ETag)
using var readResult = await dataProvider.ReadAsync(id, partitionKey);
var currentETag = readResult.Item.ETag;

// Create update command (ETag captured)
using var updateCommand = await dataProvider.UpdateAsync(id, partitionKey);

// Modify properties
updateCommand.Item.SomeProperty = "new value";

// Save with automatic ETag validation
try
{
    using var result = await updateCommand.SaveAsync(requestContext);
    // Success: new ETag generated automatically
}
catch (CommandException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
{
    // Handle concurrency conflict
    // Another process modified the item
}
```

### Thread Safety

All concurrency operations are thread-safe and work correctly in multi-threaded environments, making the system suitable for high-concurrency scenarios.
