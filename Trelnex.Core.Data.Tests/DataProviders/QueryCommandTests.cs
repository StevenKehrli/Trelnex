using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.DataProviders;

public abstract partial class DataProviderTests
{
    [Test]
    [Description("Tests query command with ordering")]
    public async Task QueryCommand_ToAsyncEnumerable_OrderBy()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating the first test item
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with ordering
        var queryCommand = _dataProvider.Query();
        queryCommand.OrderBy(i => i.PublicMessage);

        // Execute query and get results (should return items in ascending order)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Verify the ordered results using snapshot matching
        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDateTimeOffset")
                .IgnoreField("**.UpdatedDateTimeOffset")
                .IgnoreField("**.ETag"));
    }

    [Test]
    [Description("Tests query command with descending ordering")]
    public async Task QueryCommand_ToAsyncEnumerable_OrderByDescending()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating the first test item
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with descending order
        var queryCommand = _dataProvider.Query();
        queryCommand.OrderByDescending(i => i.PublicMessage);

        // Execute query and get results (should return second item first)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Verify the ordered results using snapshot matching
        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDateTimeOffset")
                .IgnoreField("**.UpdatedDateTimeOffset")
                .IgnoreField("**.ETag"));
    }

    [Test]
    [Description("Tests query command with skip operation")]
    public async Task QueryCommand_ToAsyncEnumerable_Skip()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating the first test item
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with skip operation
        var queryCommand = _dataProvider.Query();
        queryCommand.OrderBy(i => i.PublicMessage).Skip(1);

        // Execute query and get results (should return only the second item)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Verify the skipped results using snapshot matching
        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDateTimeOffset")
                .IgnoreField("**.UpdatedDateTimeOffset")
                .IgnoreField("**.ETag"));
    }

    [Test]
    [Description("Tests query command with take operation")]
    public async Task QueryCommand_ToAsyncEnumerable_Take()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating the first test item
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with take limit
        var queryCommand = _dataProvider.Query();
        queryCommand.OrderBy(i => i.PublicMessage).Take(1);

        // Execute query and get results (should return only the first item)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Verify the limited results using snapshot matching
        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDateTimeOffset")
                .IgnoreField("**.UpdatedDateTimeOffset")
                .IgnoreField("**.ETag"));
    }

    [Test]
    [Description("Tests query command with filtering")]
    public async Task QueryCommand_ToAsyncEnumerable_Where()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating the first test item
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with filtering
        var queryCommand = _dataProvider.Query();
        queryCommand.Where(i => i.PublicMessage == "Public Message #1");

        // Execute query and get results (should return only the first item)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Verify the filtered results using snapshot matching
        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDateTimeOffset")
                .IgnoreField("**.UpdatedDateTimeOffset")
                .IgnoreField("**.ETag"));
    }

    [Test]
    [Description("Tests query command when an item is deleted")]
    public async Task QueryCommand_ToAsyncEnumerable_ItemIsDeleted()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        using var createCommand = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";

        // Save the create command and capture the result
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the item
        using var deleteCommand = await _dataProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the delete command
        await deleteCommand.SaveAsync(
            cancellationToken: default);

        // Create a query command
        var queryCommand = _dataProvider.Query();

        // Execute query and get results (should return no items)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Verify the empty result using snapshot matching
        Snapshot.Match(read);
    }
}
