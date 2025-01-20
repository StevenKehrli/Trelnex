using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.CommandProviders;

public abstract class CommandProviderTests
{
    protected ICommandProvider<ITestItem> _commandProvider = null!;

    [Test]
    public async Task CreateCommand_SaveAsync()
    {
        var id = "2a4cb3ec-6624-4fc6-abc4-6a5db019f8f9";
        var partitionKey = "b297ff5b-2ab5-4b8d-9dfd-57d2e1d8c3d2";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.Message = "Message #1";

        // save it and read it back
        var created = await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);

        Snapshot.Match(
            created,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.CreatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.UpdatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("Item.UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task CreateCommand_Conflict()
    {
        var id = "8f522008-b431-4b63-93c2-c39eab3db43d";
        var partitionKey = "52fe466c-52aa-4daf-8e16-a93b26680510";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand1.Item.Message = "Message #1";

        // save it and read it back
        var created1 = await createCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(created1, Is.Not.Null);

        var createCommand2 = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand2.Item.Message = "Message #1";

        // save it again
        var ex = Assert.ThrowsAsync<CommandException>(
            async () => await createCommand2.SaveAsync(
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
    public async Task DeleteCommand_SaveAsync()
    {
        var id = "f8829dac-56f6-4448-829a-fac886aefb1b";
        var partitionKey = "fbc8502a-38ee-4edb-8a2d-485888af5bd3";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.Message = "Message #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var deleteCommand = await _commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // save it and read it back
        var deleted = await deleteCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(deleted, Is.Not.Null);

        Snapshot.Match(
            deleted!,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.CreatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.UpdatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("Item.UpdatedDate")));

                        // deletedDate
                        Assert.That(
                            fieldOption.Field<DateTime?>("Item.DeletedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task DeleteCommand_PreconditionFailed()
    {
        var id = "9ea4df8a-57ae-4897-9bd0-099eb01d669e";
        var partitionKey = "a3791462-fe7c-487a-83fa-2c9b587582ca";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.Message = "Message #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

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

        // save it and read it back
        var deleted = await deleteCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(deleted, Is.Not.Null);

        // save it again
        var ex = Assert.ThrowsAsync<CommandException>(
            async () => await deleteCommand2.SaveAsync(
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
    public async Task ReadCommand_ReadAsync()
    {
        var id = "a8cf4bc4-745a-471c-8fb1-5d4e124bbde2";
        var partitionKey = "2b541bfd-4605-48f9-b1bc-5aba5f64cd24";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.Message = "Message #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var read = await _commandProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(read, Is.Not.Null);

        Snapshot.Match(
            read!,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.CreatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.UpdatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("Item.UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task ReadCommand_NotFound()
    {
        var id = "040e17ef-b29f-4be0-885c-6e3609169743";
        var partitionKey = "802398b0-892a-49c5-8310-48212b4817a0";

        var read = await _commandProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(read, Is.Null);
    }

    [Test]
    public async Task UpdateCommandSave_SaveAsync()
    {
        var id = "7dded065-d204-4913-97ad-591e382baba5";
        var partitionKey = "48953713-d269-42c1-b803-593f8c027aef";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.Message = "Message #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var updateCommand = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        updateCommand.Item.Message = "Message #2";

        // save it and read it back
        var updated = await updateCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);

        Snapshot.Match(
            updated!,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.CreatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.UpdatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // createdDate != updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("Item.CreatedDate"),
                            Is.Not.EqualTo(fieldOption.Field<DateTime>("Item.UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    });
                }));
    }


    [Test]
    public async Task UpdateCommandSave_PreconditionFailed()
    {
        var id = "e9086db7-9d2d-41de-948e-c04c967133d8";
        var partitionKey = "2d723fdc-99f7-4c4b-a7ee-683b4e5bd2a7";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.Message = "Message #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var updateCommand1 = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        updateCommand1.Item.Message = "Message #2";

        var updateCommand2 = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        updateCommand2.Item.Message = "Message #2";

        // save it and read it back
        var updated = await updateCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);

        // save it again
        var ex = Assert.ThrowsAsync<CommandException>(
            async () => await updateCommand2.SaveAsync(
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
    public async Task BatchCommand_SaveAsync_Create()
    {
        var id1 = "267e3d9e-55be-4030-be5a-793bd5f59147";
        var id2 = "a18c881a-2268-4651-b4c2-2c5ac992f0f3";
        var partitionKey = "355bff90-f0b6-4a50-8b49-ba9882820390";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.Message = "Message #2";

        var batchCommand = _commandProvider.Batch();

        batchCommand.Add(createCommand1);
        batchCommand.Add(createCommand2);

        // save the batch and read it back
        var created = await batchCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);

        Snapshot.Match(
            created,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("[*].ReadResult.Item.CreatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("[*].ReadResult.Item.UpdatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].ReadResult.Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("[0].ReadResult.Item.UpdatedDate")));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].ReadResult.Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("[1].ReadResult.Item.UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Fields<string>("[*].ReadResult.Item.ETag"),
                            Has.All.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task BatchCommand_SaveAsync_CreateConflict()
    {
        var id1 = "2d861ba9-ba32-45c7-bfdf-2c08c70c30e2";
        var id2 = "a0ac8a58-2d76-40d2-9f14-1dd5543e8429";
        var partitionKey = "ae9d5360-1e01-480a-9465-4da82ee882b7";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand0 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand0.Item.Message = "Message #0";

        await createCommand0.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.Message = "Message #2";

        var batchCommand = _commandProvider.Batch();

        batchCommand.Add(createCommand1);
        batchCommand.Add(createCommand2);

        // save the batch and read it back
        var saved = await batchCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // query
        var queryCommand = _commandProvider.Query();

        // should return just #0
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        var o = new
        {
            saved,
            read
        };

        Snapshot.Match(
            o,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("read.[*].Item.CreatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("read.[*].Item.UpdatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("read.[0].Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("read.[0].Item.UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Fields<string>("read.[*].Item.ETag"),
                            Has.All.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task BatchCommand_SaveAsync_Update()
    {
        var id1 = "e89a2934-b506-4cc7-bf03-4148b3a10ace";
        var id2 = "7058e816-5913-4fc7-92ae-d3bf14c7c9b4";
        var partitionKey = "0a7bc917-5670-41cf-87b7-7d97d51ecb82";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.Message = "Message #2";

        var batchCommand1 = _commandProvider.Batch();

        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // save the batch and read it back
        var created = await batchCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var updateCommand1 = await _commandProvider.UpdateAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        updateCommand1.Item.Message = "Message #3";

        var updateCommand2 = await _commandProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        updateCommand2.Item.Message = "Message #4";

        var batchCommand2 = _commandProvider.Batch();

        batchCommand2.Add(updateCommand1);
        batchCommand2.Add(updateCommand2);

        // save the batch and read it back
        var updated = await batchCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(updated, Is.Not.Null);

        Snapshot.Match(
            updated,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("[*].ReadResult.Item.CreatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("[*].ReadResult.Item.UpdatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].ReadResult.Item.CreatedDate"),
                            Is.Not.EqualTo(fieldOption.Field<DateTime>("[0].ReadResult.Item.UpdatedDate")));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].ReadResult.Item.CreatedDate"),
                            Is.Not.EqualTo(fieldOption.Field<DateTime>("[1].ReadResult.Item.UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Fields<string>("[*].ReadResult.Item.ETag"),
                            Has.All.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task BatchCommand_SaveAsync_UpdatePreconditionFailed()
    {
        var id1 = "3107d2ff-9f33-4718-b180-f7b65223fba8";
        var id2 = "5f969f1c-c115-4982-949b-4e4987197dfe";
        var partitionKey = "90eabddb-a71a-4ccb-aa1a-062590597fb2";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.Message = "Message #2";

        var batchCommand1 = _commandProvider.Batch();

        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // save the batch and read it back
        var created = await batchCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var updateCommand0 = await _commandProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand0, Is.Not.Null);
        Assert.That(updateCommand0!.Item, Is.Not.Null);

        updateCommand0.Item.Message = "Message #0";

        var updateCommand1 = await _commandProvider.UpdateAsync(
            id: id1,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        updateCommand1.Item.Message = "Message #3";

        var updateCommand2 = await _commandProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        updateCommand2.Item.Message = "Message #4";

        await updateCommand0.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var batchCommand2 = _commandProvider.Batch();

        batchCommand2.Add(updateCommand1);
        batchCommand2.Add(updateCommand2);

        // save the batch and read it back
        var saved = await batchCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // query
        var queryCommand = _commandProvider.Query();

        // should return #1 and #0
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        var o = new
        {
            saved,
            read
        };

        Snapshot.Match(
            o,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("read.[*].Item.CreatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("read.[*].Item.UpdatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("read.[0].Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("read.[0].Item.UpdatedDate")));

                        // createdDate != updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("read.[1].Item.CreatedDate"),
                            Is.Not.EqualTo(fieldOption.Field<DateTime>("read.[1].Item.UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Fields<string>("read.[*].Item.ETag"),
                            Has.All.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task BatchCommand_SaveAsync_Delete()
    {
        var id1 = "6704a3e2-1757-4540-aa07-16f36a567ce6";
        var id2 = "41981704-8fc4-4da1-b12a-e913f1dfa0dc";
        var partitionKey = "75e5d19c-e809-49ed-a7ba-a89e96217ed3";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.Message = "Message #2";

        var batchCommand1 = _commandProvider.Batch();

        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // save the batch and read it back
        var created = await batchCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

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

        var batchCommand2 = _commandProvider.Batch();

        batchCommand2.Add(deleteCommand1);
        batchCommand2.Add(deleteCommand2);

        // save the batch and read it back
        var deleted = await batchCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(deleted, Is.Not.Null);

        Snapshot.Match(
            deleted,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("[*].ReadResult.Item.CreatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("[*].ReadResult.Item.UpdatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].ReadResult.Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("[0].ReadResult.Item.UpdatedDate")));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].ReadResult.Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("[1].ReadResult.Item.UpdatedDate")));

                        // deletedDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("[*].ReadResult.Item.DeletedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // _eTag
                        Assert.That(
                            fieldOption.Fields<string>("[*].ReadResult.Item.ETag"),
                            Has.All.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task BatchCommand_SaveAsync_DeletePreconditionFailed()
    {
        var id1 = "0ffc3bd2-b33f-4902-a4bf-b804d8aa01e9";
        var id2 = "1786f299-b363-48b8-9142-dc28eeebe9c4";
        var partitionKey = "c25361d6-3d14-452d-a99d-22ef7c85d70e";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        createCommand1.Item.Message = "Message #1";

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.Message = "Message #2";

        var batchCommand1 = _commandProvider.Batch();

        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // save the batch and read it back
        var created = await batchCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var deleteCommand0 = await _commandProvider.DeleteAsync(
            id: id2,
            partitionKey: partitionKey);

        Assert.That(deleteCommand0, Is.Not.Null);
        Assert.That(deleteCommand0!.Item, Is.Not.Null);

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

        await deleteCommand0.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var batchCommand2 = _commandProvider.Batch();

        batchCommand2.Add(deleteCommand1);
        batchCommand2.Add(deleteCommand2);

        // save the batch and read it back
        var saved = await batchCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        Assert.That(saved, Is.Not.Null);

        // query
        var queryCommand = _commandProvider.Query();

        // should return just #1
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        var o = new
        {
            saved,
            read
        };

        Snapshot.Match(
            o,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // createdDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("read.[*].Item.CreatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Fields<DateTime>("read,[*].Item.UpdatedDate"),
                            Has.All.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("read.[0].Item.CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("read.[0].Item.UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Fields<string>("read.[*].Item.ETag"),
                            Has.All.Not.Default);
                    });
                }));
    }

    [Test]
    public async Task QueryCommand_ToAsyncEnumerable()
    {
        var id1 = "3fca6d8a-75c1-491a-9178-90343551364a";
        var partitionKey1 = "81dc4acd-dcbe-4d5f-a36f-21a35f158b2c";

        var id2 = "648de92a-b7e8-41c5-a5d2-bdf0cc65d67c";
        var partitionKey2 = "e36f287e-188d-4a74-9db7-dab74282b5dd";

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        createCommand1.Item.Message = "Message #1";

        // save it
        await createCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        createCommand2.Item.Message = "Message #2";

        // save it
        await createCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // query
        var queryCommand = _commandProvider.Query();

        // should return both items
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.CreatedDate")
                .IgnoreField("**.UpdatedDate")
                .IgnoreField("**.ETag"));
    }

    [Test]
    public async Task QueryCommand_ToAsyncEnumerable_ItemIsDeleted()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.Message = "Message #1";

        // save it and read it back
        var created = (await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default))!;

        var deleteCommand = await _commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        // save it
        await deleteCommand!.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // query
        var queryCommand = _commandProvider.Query();

        // should return no items
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        Snapshot.Match(read);
    }

    [Test]
    public async Task QueryCommand_ToAsyncEnumerable_OrderBy()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        createCommand1.Item.Message = "Message #1";

        // save it
        await createCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        createCommand2.Item.Message = "Message #2";

        // save it
        await createCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // query
        var queryCommand = _commandProvider.Query();
        queryCommand.OrderBy(i => i.Message);

        // should return first item
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDate")
                .IgnoreField("**.UpdatedDate")
                .IgnoreField("**.ETag"));
    }

    [Test]
    public async Task QueryCommand_ToAsyncEnumerable_OrderByDescending()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        createCommand1.Item.Message = "Message #1";

        // save it
        await createCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        createCommand2.Item.Message = "Message #2";

        // save it
        await createCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // query
        var queryCommand = _commandProvider.Query();
        queryCommand.OrderByDescending(i => i.Message);

        // should return second item first
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDate")
                .IgnoreField("**.UpdatedDate")
                .IgnoreField("**.ETag"));
    }

    [Test]
    public async Task QueryCommand_ToAsyncEnumerable_Skip()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        createCommand1.Item.Message = "Message #1";

        // save it
        await createCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        createCommand2.Item.Message = "Message #2";

        // save it
        await createCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // query
        var queryCommand = _commandProvider.Query();
        queryCommand.OrderBy(i => i.Message).Skip(1);

        // should return second item
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDate")
                .IgnoreField("**.UpdatedDate")
                .IgnoreField("**.ETag"));
    }


    [Test]
    public async Task QueryCommand_ToAsyncEnumerable_Take()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        createCommand1.Item.Message = "Message #1";

        // save it
        await createCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        createCommand2.Item.Message = "Message #2";

        // save it
        await createCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // query
        var queryCommand = _commandProvider.Query();
        queryCommand.OrderBy(i => i.Message).Take(1);

        // should return first item
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDate")
                .IgnoreField("**.UpdatedDate")
                .IgnoreField("**.ETag"));
    }

    [Test]
    public async Task QueryCommand_ToAsyncEnumerable_Where()
    {
        var id1 = Guid.NewGuid().ToString();
        var partitionKey1 = Guid.NewGuid().ToString();

        var id2 = Guid.NewGuid().ToString();
        var partitionKey2 = Guid.NewGuid().ToString();

        var requestContext = TestRequestContext.Create();

        var createCommand1 = _commandProvider.Create(
            id: id1,
            partitionKey: partitionKey1);

        createCommand1.Item.Message = "Message #1";

        // save it
        await createCommand1.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var createCommand2 = _commandProvider.Create(
            id: id2,
            partitionKey: partitionKey2);

        createCommand2.Item.Message = "Message #2";

        // save it
        await createCommand2.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // query
        var queryCommand = _commandProvider.Query();
        queryCommand.Where(i => i.Message == "Message #1");

        // should return first item
        var read = await queryCommand.ToAsyncEnumerable().ToArrayAsync();

        Snapshot.Match(
            read,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDate")
                .IgnoreField("**.UpdatedDate")
                .IgnoreField("**.ETag"));
    }
}
