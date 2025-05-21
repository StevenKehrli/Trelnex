using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.CommandProviders;

[Category("CommandProviders")]
public abstract class CommandProviderTests
{
    protected ICommandProvider<ITestItem> _commandProvider = null!;

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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand = _commandProvider.Batch();
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
                    Assert.Multiple(() =>
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
                    });
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
        var createCommand1 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        // Save the initial item
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for a new item with unique id
        var createCommand2 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set values on the new item
        createCommand2.Item.Message = "Message #2";

        // Create another command with the same id as the initial item (will conflict)
        var createCommand3 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set values on the conflicting item
        createCommand3.Item.Message = "Message #3";

        // Create a batch command and add our create commands to it
        var batchCommand = _commandProvider.Batch();
        batchCommand.Add(createCommand2);
        batchCommand.Add(createCommand3);

        // Save the batch command and capture the result
        var saved = await batchCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // Create a query command to verify results
        var queryCommand = _commandProvider.Query();

        // Execute query and get results (should return just the initial item)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

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
                    Assert.Multiple(() =>
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
                    });
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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand1 = _commandProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create delete commands for both items
        var deleteCommand1 = await _commandProvider.DeleteAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(deleteCommand1, Is.Not.Null);
        Assert.That(deleteCommand1!.Item, Is.Not.Null);

        var deleteCommand2 = await _commandProvider.DeleteAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(deleteCommand2, Is.Not.Null);
        Assert.That(deleteCommand2!.Item, Is.Not.Null);

        // Create a batch command for delete operations
        var batchCommand2 = _commandProvider.Batch();
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
                    Assert.Multiple(() =>
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
                    });
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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand1 = _commandProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the second item (will be saved first)
        var deleteCommand1 = await _commandProvider.DeleteAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(deleteCommand1, Is.Not.Null);
        Assert.That(deleteCommand1!.Item, Is.Not.Null);

        // Create a delete command for the first item
        var deleteCommand2 = await _commandProvider.DeleteAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(deleteCommand2, Is.Not.Null);
        Assert.That(deleteCommand2!.Item, Is.Not.Null);

        // Create another delete command for the second item (will conflict)
        var deleteCommand3 = await _commandProvider.DeleteAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(deleteCommand3, Is.Not.Null);
        Assert.That(deleteCommand3!.Item, Is.Not.Null);

        // Save the first delete command for the second item
        await deleteCommand1.SaveAsync(
            cancellationToken: default);

        // Create a batch command for delete operations (one will succeed, one will fail due to precondition)
        var batchCommand2 = _commandProvider.Batch();
        batchCommand2.Add(deleteCommand2);
        batchCommand2.Add(deleteCommand3);

        // Save the batch command and capture the result
        var saved = await batchCommand2.SaveAsync(
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // Create a query command to verify results
        var queryCommand = _commandProvider.Query();

        // Execute query and get results (should return just the first item)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

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
                    Assert.Multiple(() =>
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
                    });
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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand1 = _commandProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create update commands for both items
        var updateCommand1 = await _commandProvider.UpdateAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        // Update message on first item
        updateCommand1.Item.Message = "Message #3";

        var updateCommand2 = await _commandProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        // Update message on second item
        updateCommand2.Item.Message = "Message #4";

        // Create a batch command for update operations
        var batchCommand2 = _commandProvider.Batch();
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
                    Assert.Multiple(() =>
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
                    });
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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Create a batch command and add our create commands to it
        var batchCommand1 = _commandProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command and capture the result
        var created = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create a update command for the second item (will be saved first)
        var updateCommand1 = await _commandProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        // Update message on the second item
        updateCommand1.Item.Message = "Message #0";

        // Create a update command for the first item
        var updateCommand2 = await _commandProvider.UpdateAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        // Update message on the first item
        updateCommand2.Item.Message = "Message #3";

        // Create another update command for the second item (will conflict)
        var updateCommand3 = await _commandProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand3, Is.Not.Null);
        Assert.That(updateCommand3!.Item, Is.Not.Null);

        // Update message on the second item again (will conflict with updateCommand1)
        updateCommand3.Item.Message = "Message #4";

        // Save the first update command for the second item
        await updateCommand1.SaveAsync(
            cancellationToken: default);

        // Create a batch command for update operations (one will succeed, one will fail due to precondition)
        var batchCommand2 = _commandProvider.Batch();
        batchCommand2.Add(updateCommand2);
        batchCommand2.Add(updateCommand3);

        // Save the batch command and capture the result
        var saved = await batchCommand2.SaveAsync(
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // Create a query command to verify results
        var queryCommand = _commandProvider.Query();

        // Execute query and get results (should return the first item and updated second item)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

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
                    Assert.Multiple(() =>
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

                        // Verify created date equals updated date for the first item (not updated)
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("read.[0].Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("read.[0].Item.UpdatedDateTimeOffset")));

                        // Verify created date not equal to updated date for the second item (was updated)
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("read.[1].Item.CreatedDateTimeOffset"),
                            Is.Not.EqualTo(fieldOption.Field<DateTimeOffset>("read.[1].Item.UpdatedDateTimeOffset")));

                        // Verify ETags are present
                        Assert.That(
                            fieldOption.Fields<string>("read.[*].Item.ETag"),
                            Has.All.Not.Default);
                    });
                }));
    }

    [Test]
    [Description("Tests create command with a conflict")]
    public async Task CreateCommand_Conflict()
    {
        var id = "8f522008-b431-4b63-93c2-c39eab3db43d";
        var partitionKey = "52fe466c-52aa-4daf-8e16-a93b26680510";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        var createCommand1 = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand1.Item.Message = "Message #1";

        // Save the command and capture the result
        var created1 = await createCommand1.SaveAsync(
            cancellationToken: default);

        Assert.That(created1, Is.Not.Null);

        // Create another command with the same id (will conflict)
        var createCommand2 = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set the same message on the second command
        createCommand2.Item.Message = "Message #1";

        // Attempt to save the second command (should throw a CommandException)
        var ex = Assert.ThrowsAsync<CommandException>(
            async () => await createCommand2.SaveAsync(
                cancellationToken: default))!;

        // Create object for snapshot matching
        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        // Verify the exception using snapshot matching
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests create command save operation")]
    public async Task CreateCommand_SaveAsync()
    {
        var id = "2a4cb3ec-6624-4fc6-abc4-6a5db019f8f9";
        var partitionKey = "b297ff5b-2ab5-4b8d-9dfd-57d2e1d8c3d2";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";

        // Save the command and capture the result
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            created,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.CreatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.UpdatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date equals updated date
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("Item.UpdatedDateTimeOffset")));

                        // Verify ETag is present
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    });
                }));
    }

    [Test]
    [Description("Tests delete command save operation")]
    public async Task DeleteCommand_SaveAsync()
    {
        var id = "f8829dac-56f6-4448-829a-fac886aefb1b";
        var partitionKey = "fbc8502a-38ee-4edb-8a2d-485888af5bd3";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the item
        var deleteCommand = await _commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the delete command and capture the result
        var deleted = await deleteCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(deleted, Is.Not.Null);

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            deleted!,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.CreatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.UpdatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date equals updated date
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("Item.UpdatedDateTimeOffset")));

                        // Verify deleted date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.DeletedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify ETag is present
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    });
                }));
    }

    [Test]
    [Description("Tests delete command when precondition fails")]
    public async Task DeleteCommand_PreconditionFailed()
    {
        var id = "9ea4df8a-57ae-4897-9bd0-099eb01d669e";
        var partitionKey = "a3791462-fe7c-487a-83fa-2c9b587582ca";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create two delete commands for the same item
        var deleteCommand1 = await _commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand1, Is.Not.Null);
        Assert.That(deleteCommand1!.Item, Is.Not.Null);

        var deleteCommand2 = await _commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand2, Is.Not.Null);
        Assert.That(deleteCommand2!.Item, Is.Not.Null);

        // Save the first delete command and capture the result
        var deleted = await deleteCommand1.SaveAsync(
            cancellationToken: default);

        Assert.That(deleted, Is.Not.Null);

        // Attempt to save the second delete command (should throw a CommandException)
        var ex = Assert.ThrowsAsync<CommandException>(
            async () => await deleteCommand2.SaveAsync(
                cancellationToken: default))!;

        // Create object for snapshot matching
        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        // Verify the exception using snapshot matching
        Snapshot.Match(o);
    }

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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with ordering
        var queryCommand = _commandProvider.Query();
        queryCommand.OrderBy(i => i.Message);

        // Execute query and get results (should return items in ascending order)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with descending order
        var queryCommand = _commandProvider.Query();
        queryCommand.OrderByDescending(i => i.Message);

        // Execute query and get results (should return second item first)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with skip operation
        var queryCommand = _commandProvider.Query();
        queryCommand.OrderBy(i => i.Message).Skip(1);

        // Execute query and get results (should return only the second item)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with take limit
        var queryCommand = _commandProvider.Query();
        queryCommand.OrderBy(i => i.Message).Take(1);

        // Execute query and get results (should return only the first item)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

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
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command with filtering
        var queryCommand = _commandProvider.Query();
        queryCommand.Where(i => i.Message == "Message #1");

        // Execute query and get results (should return only the first item)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

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
    [Description("Tests query command without modifiers")]
    public async Task QueryCommand_ToAsyncEnumerable()
    {
        var id1 = "3fca6d8a-75c1-491a-9178-90343551364a";
        var partitionKey1 = "81dc4acd-dcbe-4d5f-a36f-21a35f158b2c";

        var id2 = "648de92a-b7e8-41c5-a5d2-bdf0cc65d67c";
        var partitionKey2 = "e36f287e-188d-4a74-9db7-dab74282b5dd";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating the first test item
        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        // Set initial values on the first test item
        createCommand1.Item.Message = "Message #1";

        // Save the first create command
        await createCommand1.SaveAsync(
            cancellationToken: default);

        // Create a command for creating the second test item
        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        // Set initial values on the second test item
        createCommand2.Item.Message = "Message #2";

        // Save the second create command
        await createCommand2.SaveAsync(
            cancellationToken: default);

        // Create a query command
        var queryCommand = _commandProvider.Query();

        // Execute query and get results (should return both items)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        // Verify the results using snapshot matching
        Snapshot.Match(
            read,
            matchOptions => matchOptions
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
        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";

        // Save the create command and capture the result
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the item
        var deleteCommand = await _commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the delete command
        await deleteCommand.SaveAsync(
            cancellationToken: default);

        // Create a query command
        var queryCommand = _commandProvider.Query();

        // Execute query and get results (should return no items)
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        // Verify the empty result using snapshot matching
        Snapshot.Match(read);
    }

    [Test]
    [Description("Tests read command operation")]
    public async Task ReadCommand_ReadAsync()
    {
        var id = "a8cf4bc4-745a-471c-8fb1-5d4e124bbde2";
        var partitionKey = "2b541bfd-4605-48f9-b1bc-5aba5f64cd24";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Read the item using the command provider
        var read = await _commandProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(read, Is.Not.Null);

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            read!,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.CreatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.UpdatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date equals updated date
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("Item.UpdatedDateTimeOffset")));

                        // Verify ETag is present
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    });
                }));
    }

    [Test]
    [Description("Tests read command when item is not found")]
    public async Task ReadCommand_NotFound()
    {
        var id = "040e17ef-b29f-4be0-885c-6e3609169743";
        var partitionKey = "802398b0-892a-49c5-8310-48212b4817a0";

        // Attempt to read a non-existent item
        var read = await _commandProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        // Verify that no item was found
        Assert.That(read, Is.Null);
    }

    [Test]
    [Description("Tests update command save operation")]
    public async Task UpdateCommand_SaveAsync()
    {
        var id = "7dded065-d204-4913-97ad-591e382baba5";
        var partitionKey = "48953713-d269-42c1-b803-593f8c027aef";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create an update command for the item
        var updateCommand = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Update message on the item
        updateCommand.Item.Message = "Message #2";

        // Save the update command and capture the result
        var updated = await updateCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);

        // Verify the result using snapshot matching with assertions
        Snapshot.Match(
            updated!,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify created date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.CreatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify updated date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.UpdatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify created date not equal to updated date
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.CreatedDateTimeOffset"),
                            Is.Not.EqualTo(fieldOption.Field<DateTimeOffset>("Item.UpdatedDateTimeOffset")));

                        // Verify ETag is present
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    });
                }));
    }

    [Test]
    [Description("Tests update command when precondition fails")]
    public async Task UpdateCommand_PreconditionFailed()
    {
        var id = "e9086db7-9d2d-41de-948e-c04c967133d8";
        var partitionKey = "2d723fdc-99f7-4c4b-a7ee-683b4e5bd2a7";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create two update commands for the same item
        var updateCommand1 = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        // Update message on the first update command
        updateCommand1.Item.Message = "Message #2";

        var updateCommand2 = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        // Update message on the second update command
        updateCommand2.Item.Message = "Message #2";

        // Save the first update command and capture the result
        var updated = await updateCommand1.SaveAsync(
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);

        // Attempt to save the second update command (should throw a CommandException)
        var ex = Assert.ThrowsAsync<CommandException>(
            async () => await updateCommand2.SaveAsync(
                cancellationToken: default))!;

        // Create object for snapshot matching
        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        // Verify the exception using snapshot matching
        Snapshot.Match(o);
    }
}
