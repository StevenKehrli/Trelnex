using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Commands;

[Category("Commands")]
public class BatchCommandSaveTests
{
    [Test]
    [Description("Tests that the results returned from saving a batch of create commands is read-only")]
    public async Task BatchCommandSave_Create_SaveAsync_ResultIsReadOnly()
    {
        var partitionKey = Guid.NewGuid().ToString();

        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();

        // Get a data provider for our test item type
        var dataProvider = new InMemoryDataProvider<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.All);

        // Create a new command to create our first test item
        using var createCommand1 = dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public #1";
        createCommand1.Item.PrivateMessage = "Private #1";

        // Create a new command to create our second test item
        using var createCommand2 = dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.PublicMessage = "Public #2";
        createCommand2.Item.PrivateMessage = "Private #2";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand1);
        batchCommand.Add(createCommand2);

        // Save the batch command (which also saves the contained create command)
        var result = await batchCommand.SaveAsync(
            cancellationToken: default);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(2));

            Assert.That(result[0].ReadResult, Is.Not.Null);
            Assert.That(result[0].ReadResult!.Item.Id, Is.EqualTo(id1));
            Assert.That(result[0].ReadResult!.Item.PartitionKey, Is.EqualTo(partitionKey));

            Assert.That(result[1].ReadResult, Is.Not.Null);
            Assert.That(result[1].ReadResult!.Item.Id, Is.EqualTo(id2));
            Assert.That(result[1].ReadResult!.Item.PartitionKey, Is.EqualTo(partitionKey));

            // Attempt to save the first create command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await createCommand1.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");

            // Attempt to save the second create command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await createCommand2.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");

            // Attempt to save the batch command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await batchCommand.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");
        }

        Snapshot.Match(
            result,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDateTimeOffset")
                .IgnoreField("**.UpdatedDateTimeOffset")
                .IgnoreField("**.DeletedDateTimeOffset")
                .IgnoreField("**.ETag"));
    }

    [Test]
    [Description("Tests that the results returned from saving a batch of delete commands is read-only")]
    public async Task BatchCommandSave_Delete_SaveAsync_ResultIsReadOnly()
    {
        var partitionKey = Guid.NewGuid().ToString();

        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();

        // Get a data provider for our test item type
        var dataProvider = new InMemoryDataProvider<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.All);

        // Create a new command to create our first test item
        using var createCommand1 = dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public #1";
        createCommand1.Item.PrivateMessage = "Private #1";

        // Create a new command to create our second test item
        using var createCommand2 = dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.PublicMessage = "Public #2";
        createCommand2.Item.PrivateMessage = "Private #2";

        // Create a batch command and add our create command to it
        var batchCommand1 = dataProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command (which also saves the contained create command)
        var result1 = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create a batch command and add our delete command to id
        var deleteCommand1 = await dataProvider.DeleteAsync(
            id: id1,
            partitionKey: partitionKey);

        var deleteCommand2 = await dataProvider.DeleteAsync(
            id: id2,
            partitionKey: partitionKey);

        var batchCommand2 = dataProvider.Batch();
        batchCommand2.Add(deleteCommand1!);
        batchCommand2.Add(deleteCommand2!);

        var result2 = await batchCommand2.SaveAsync(
            cancellationToken: default);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result2, Is.Not.Null);
            Assert.That(result2, Has.Length.EqualTo(2));

            Assert.That(result2[0].ReadResult, Is.Not.Null);
            Assert.That(result2[0].ReadResult!.Item.Id, Is.EqualTo(id1));
            Assert.That(result2[0].ReadResult!.Item.PartitionKey, Is.EqualTo(partitionKey));

            Assert.That(result2[1].ReadResult, Is.Not.Null);
            Assert.That(result2[1].ReadResult!.Item.Id, Is.EqualTo(id2));
            Assert.That(result2[1].ReadResult!.Item.PartitionKey, Is.EqualTo(partitionKey));

            // Attempt to save the first delete command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await deleteCommand1!.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");

            // Attempt to save the second delete command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await deleteCommand2!.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");

            // Attempt to save the batch command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await batchCommand2.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");
        }

        Snapshot.Match(
            result2,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDateTimeOffset")
                .IgnoreField("**.UpdatedDateTimeOffset")
                .IgnoreField("**.DeletedDateTimeOffset")
                .IgnoreField("**.ETag"));
    }

    [Test]
    [Description("Tests that the results returned from saving a batch command is read-only")]
    public async Task BatchCommandSave_Update_SaveAsync_ResultIsReadOnly()
    {
        var partitionKey = Guid.NewGuid().ToString();

        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();

        // Get a data provider for our test item type
        var dataProvider = new InMemoryDataProvider<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.All);

        // Create a new command to create our first test item
        using var createCommand1 = dataProvider.Create(
            id: id1,
            partitionKey: partitionKey);

        // Set initial values on the first test item
        createCommand1.Item.PublicMessage = "Public #1";
        createCommand1.Item.PrivateMessage = "Private #1";

        // Create a new command to create our second test item
        using var createCommand2 = dataProvider.Create(
            id: id2,
            partitionKey: partitionKey);

        createCommand2.Item.PublicMessage = "Public #2";
        createCommand2.Item.PrivateMessage = "Private #2";

        // Create a batch command and add our create command to it
        var batchCommand1 = dataProvider.Batch();
        batchCommand1.Add(createCommand1);
        batchCommand1.Add(createCommand2);

        // Save the batch command (which also saves the contained create command)
        var result1 = await batchCommand1.SaveAsync(
            cancellationToken: default);

        // Create a batch command and add our update command to id
        var updateCommand1 = await dataProvider.UpdateAsync(
            id: id1,
            partitionKey: partitionKey);

        updateCommand1!.Item.PublicMessage = "Public #3";
        updateCommand1!.Item.PrivateMessage = "Private #3";

        var updateCommand2 = await dataProvider.UpdateAsync(
            id: id2,
            partitionKey: partitionKey);

        updateCommand2!.Item.PublicMessage = "Public #4";
        updateCommand2!.Item.PrivateMessage = "Private #4";

        var batchCommand2 = dataProvider.Batch();
        batchCommand2.Add(updateCommand1);
        batchCommand2.Add(updateCommand2);

        var result2 = await batchCommand2.SaveAsync(
            cancellationToken: default);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result2, Is.Not.Null);
            Assert.That(result2, Has.Length.EqualTo(2));

            Assert.That(result2[0].ReadResult, Is.Not.Null);
            Assert.That(result2[0].ReadResult!.Item.Id, Is.EqualTo(id1));
            Assert.That(result2[0].ReadResult!.Item.PartitionKey, Is.EqualTo(partitionKey));

            Assert.That(result2[1].ReadResult, Is.Not.Null);
            Assert.That(result2[1].ReadResult!.Item.Id, Is.EqualTo(id2));
            Assert.That(result2[1].ReadResult!.Item.PartitionKey, Is.EqualTo(partitionKey));

            // Attempt to save the first update command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await updateCommand1.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");

            // Attempt to save the second update command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await updateCommand2.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");

            // Attempt to save the batch command again, which should throw
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await batchCommand2.SaveAsync(
                    cancellationToken: default),
                "The Command is no longer valid because its SaveAsync method has already been called.");
        }

        Snapshot.Match(
            result2,
            matchOptions => matchOptions
                .IgnoreField("**.Id")
                .IgnoreField("**.PartitionKey")
                .IgnoreField("**.CreatedDateTimeOffset")
                .IgnoreField("**.UpdatedDateTimeOffset")
                .IgnoreField("**.DeletedDateTimeOffset")
                .IgnoreField("**.ETag"));
    }

    [Test]
    [Description("Tests that a batch command and its contained commands cannot be saved more than once")]
    public async Task BatchCommandSave_SaveAsync_WhenAlreadySaved()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Get a data provider for our test item type
        var dataProvider = new InMemoryDataProvider<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.All);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Create a batch command and add our create command to it
        var batchCommand = dataProvider.Batch();
        batchCommand.Add(createCommand);

        // Save the batch command (which also saves the contained create command)
        await batchCommand.SaveAsync(
            cancellationToken: default);

        // Attempt to save the create command again, which should throw
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await createCommand.SaveAsync(
                cancellationToken: default),
            "The Command is no longer valid because its SaveAsync method has already been called.");
    }
}
