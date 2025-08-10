using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.DataProviders;

public abstract partial class DataProviderTests
{
    [Test]
    [Description("Tests batch command with create operations")]
    public async Task BatchCommand_SaveAsync_Create()
    {
        var id1 = "267e3d9e-55be-4030-be5a-793bd5f59147";
        var id2 = "a18c881a-2268-4651-b4c2-2c5ac992f0f3";
        var partitionKey = "355bff90-f0b6-4a50-8b49-ba9882820390";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create two commands for creating test items
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand = _dataProvider.Batch();
        batchCommand.Add(createCommand1);
        batchCommand.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            created,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("[*].ReadResult.Item.CreatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("[*].ReadResult.Item.UpdatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date equals updated date for first item
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[0].ReadResult.Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("[0].ReadResult.Item.UpdatedDateTimeOffset")));

                        // Verify created date equals updated date for second item
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[1].ReadResult.Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("[1].ReadResult.Item.UpdatedDateTimeOffset")));

                        // Verify ETags are present
                        Assert.That(
                            fieldOption.Fields<string>("[*].ReadResult.Item.ETag"),
                            Has.All.Not.Default);
                    }
                }));
    }

    [Test]
    [Description("Tests batch command with create operations when a conflict occurs")]
    public async Task BatchCommand_SaveAsync_CreateConflict()
    {
        var id1 = "2d861ba9-ba32-45c7-bfdf-2c08c70c30e2";
        var id2 = "a0ac8a58-2d76-40d2-9f14-1dd5543e8429";
        var partitionKey = "ae9d5360-1e01-480a-9465-4da82ee882b7";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create and save an initial item with id2
        using var createCommand1 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        // Save the initial item
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for a new item with unique id
        using var createCommand2 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set values on the new item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Create another command with the same id as the initial item (will conflict)
        using var createCommand3 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set values on the conflicting item
        createCommand3.Item.PublicMessage = "Public Message #3";
        createCommand3.Item.PrivateMessage = "Private Message #3";

        // Create a batch command and add our create commands to it
        var batchCommand = _dataProvider.Batch();
        batchCommand.Add(createCommand2);
        batchCommand.Add(createCommand3);

        // Save the batch command and capture the result
        var saved = await batchCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // Create a query command to verify results
        var queryCommand = _dataProvider.Query();

        // Execute query and get results (should return just the initial item)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Create object for snapshot matching
        var o = new
        {
            saved,
            read
        };

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            o,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("read.[*].Item.CreatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("read.[*].Item.UpdatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date equals updated date for initial item
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("read.[0].Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("read.[0].Item.UpdatedDateTimeOffset")));

                        // Verify ETags are present
                        Assert.That(
                            fieldOption.Fields<string>("read.[*].Item.ETag"),
                            Has.All.Not.Default);
                    }
                }));
    }

    [Test]
    [Description("Tests batch command with delete operations")]
    public async Task BatchCommand_SaveAsync_Delete()
    {
        var id1 = "6704a3e2-1757-4540-aa07-16f36a567ce6";
        var id2 = "41981704-8fc4-4da1-b12a-e913f1dfa0dc";
        var partitionKey = "75e5d19c-e809-49ed-a7ba-a89e96217ed3";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create two commands for creating test items
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand1 = _dataProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create delete commands for both items
        using var deleteCommand1 = await _dataProvider.DeleteAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(deleteCommand1, Is.Not.Null);
        Assert.That(deleteCommand1!.Item, Is.Not.Null);

        using var deleteCommand2 = await _dataProvider.DeleteAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(deleteCommand2, Is.Not.Null);
        Assert.That(deleteCommand2!.Item, Is.Not.Null);

        // Create a batch command for delete operations
        var batchCommand2 = _dataProvider.Batch();
        batchCommand2.Add(deleteCommand1);
        batchCommand2.Add(deleteCommand2);

        // Save the delete batch command and capture the result
        var deleted = await batchCommand2.SaveAsync(
            cancellationToken: default);

        Assert.That(deleted, Is.Not.Null);

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            deleted,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("[*].ReadResult.Item.CreatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("[*].ReadResult.Item.UpdatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date equals updated date for first item
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[0].ReadResult.Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("[0].ReadResult.Item.UpdatedDateTimeOffset")));

                        // Verify created date equals updated date for second item
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[1].ReadResult.Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("[1].ReadResult.Item.UpdatedDateTimeOffset")));

                        // Verify deleted dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("[*].ReadResult.Item.DeletedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify ETags are present
                        Assert.That(
                            fieldOption.Fields<string>("[*].ReadResult.Item.ETag"),
                            Has.All.Not.Default);
                    }
                }));
    }

    [Test]
    [Description("Tests batch command with delete operations when a precondition fails")]
    public async Task BatchCommand_SaveAsync_DeletePreconditionFailed()
    {
        var id1 = "0ffc3bd2-b33f-4902-a4bf-b804d8aa01e9";
        var id2 = "1786f299-b363-48b8-9142-dc28eeebe9c4";
        var partitionKey = "c25361d6-3d14-452d-a99d-22ef7c85d70e";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create two commands for creating test items
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand1 = _dataProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the second item (will be saved first)
        using var deleteCommand1 = await _dataProvider.DeleteAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(deleteCommand1, Is.Not.Null);
        Assert.That(deleteCommand1!.Item, Is.Not.Null);

        // Create a delete command for the first item
        using var deleteCommand2 = await _dataProvider.DeleteAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(deleteCommand2, Is.Not.Null);
        Assert.That(deleteCommand2!.Item, Is.Not.Null);

        // Create another delete command for the second item (will conflict)
        using var deleteCommand3 = await _dataProvider.DeleteAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(deleteCommand3, Is.Not.Null);
        Assert.That(deleteCommand3!.Item, Is.Not.Null);

        // Save the first delete command for the second item
        await deleteCommand1.SaveAsync(
            cancellationToken: default);

        // Create a batch command for delete operations (one will succeed, one will fail due to precondition)
        var batchCommand2 = _dataProvider.Batch();
        batchCommand2.Add(deleteCommand2);
        batchCommand2.Add(deleteCommand3);

        // Save the batch command and capture the result
        var saved = await batchCommand2.SaveAsync(
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // Create a query command to verify results
        var queryCommand = _dataProvider.Query();

        // Execute query and get results (should return just the first item)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Create object for snapshot matching
        var o = new
        {
            saved,
            read
        };

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            o,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("read.[*].Item.CreatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("read.[*].Item.UpdatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date equals updated date for the remaining item
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("read.[0].Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("read.[0].Item.UpdatedDateTimeOffset")));

                        // Verify ETags are present
                        Assert.That(
                            fieldOption.Fields<string>("read.[*].Item.ETag"),
                            Has.All.Not.Default);
                    }
                }));
    }

    [Test]
    [Description("Tests batch command with update operations")]
    public async Task BatchCommand_SaveAsync_Update()
    {
        var id1 = "e89a2934-b506-4cc7-bf03-4148b3a10ace";
        var id2 = "7058e816-5913-4fc7-92ae-d3bf14c7c9b4";
        var partitionKey = "0a7bc917-5670-41cf-87b7-7d97d51ecb82";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create two commands for creating test items
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand1 = _dataProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create update commands for both items
        using var updateCommand1 = await _dataProvider.UpdateAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        // Update message on first item
        updateCommand1.Item.PublicMessage = "Public Message #3";
        updateCommand1.Item.PrivateMessage = "Private Message #3";

        using var updateCommand2 = await _dataProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        // Update message on second item
        updateCommand2.Item.PublicMessage = "Public Message #4";
        updateCommand2.Item.PrivateMessage = "Private Message #4";

        // Create a batch command for update operations
        var batchCommand2 = _dataProvider.Batch();
        batchCommand2.Add(updateCommand1);
        batchCommand2.Add(updateCommand2);

        // Save the update batch command and capture the result
        var updated = await batchCommand2.SaveAsync(
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            updated,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("[*].ReadResult.Item.CreatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("[*].ReadResult.Item.UpdatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date not equal to updated date for first item
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[0].ReadResult.Item.CreatedDateTimeOffset"),
                            Is.Not.EqualTo(fieldOption.Field<DateTimeOffset>("[0].ReadResult.Item.UpdatedDateTimeOffset")));

                        // Verify created date not equal to updated date for second item
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[1].ReadResult.Item.CreatedDateTimeOffset"),
                            Is.Not.EqualTo(fieldOption.Field<DateTimeOffset>("[1].ReadResult.Item.UpdatedDateTimeOffset")));

                        // Verify ETags are present
                        Assert.That(
                            fieldOption.Fields<string>("[*].ReadResult.Item.ETag"),
                            Has.All.Not.Default);
                    }
                }));
    }

    [Test]
    [Description("Tests batch command with update operations when a precondition fails")]
    public async Task BatchCommand_SaveAsync_UpdatePreconditionFailed()
    {
        var id1 = "3107d2ff-9f33-4718-b180-f7b65223fba8";
        var id2 = "5f969f1c-c115-4982-949b-4e4987197dfe";
        var partitionKey = "90eabddb-a71a-4ccb-aa1a-062590597fb2";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create two commands for creating test items
        using var createCommand1 = _dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        using var createCommand2 = _dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.PublicMessage = "Public Message #2";
        createCommand2.Item.PrivateMessage = "Private Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand1 = _dataProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create a update command for the second item (will be saved first)
        using var updateCommand1 = await _dataProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        // Update message on the second item
        updateCommand1.Item.PublicMessage = "Public Message #0";
        updateCommand1.Item.PrivateMessage = "Private Message #0";

        // Create a update command for the first item
        using var updateCommand2 = await _dataProvider.UpdateAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        // Update message on the first item
        updateCommand2.Item.PublicMessage = "Public Message #3";
        updateCommand2.Item.PrivateMessage = "Private Message #3";

        // Create another update command for the second item (will conflict)
        using var updateCommand3 = await _dataProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand3, Is.Not.Null);
        Assert.That(updateCommand3!.Item, Is.Not.Null);

        // Update message on the second item again (will conflict with updateCommand1)
        updateCommand3.Item.PublicMessage = "Public Message #4";
        updateCommand3.Item.PrivateMessage = "Private Message #4";

        // Save the first update command for the second item
        await updateCommand1.SaveAsync(
            cancellationToken: default);

        // Create a batch command for update operations (one will succeed, one will fail due to precondition)
        var batchCommand2 = _dataProvider.Batch();
        batchCommand2.Add(updateCommand2);
        batchCommand2.Add(updateCommand3);

        // Save the batch command and capture the result
        var saved = await batchCommand2.SaveAsync(
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // Create a query command to verify results
        var queryCommand = _dataProvider.Query();

        // Execute query and get results (should return the first item and updated second item)
        using var read = await queryCommand.ToDisposableEnumerableAsync();

        // Create object for snapshot matching
        var o = new
        {
            saved,
            read
        };

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            o,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("read.[*].Item.CreatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated dates are within expected time range
                        Assert.That(
                            fieldOption.Fields<DateTimeOffset>("read.[*].Item.UpdatedDateTimeOffset"),
                            Has.All.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify ETags are present
                        Assert.That(
                            fieldOption.Fields<string>("read.[*].Item.ETag"),
                            Has.All.Not.Default);
                    }
                }));
    }
}
