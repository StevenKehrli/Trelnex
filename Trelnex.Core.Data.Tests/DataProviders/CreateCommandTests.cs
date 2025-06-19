using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.DataProviders;

public abstract partial class DataProviderTests
{
    [Test]
    [Description("Tests create command with a conflict")]
    public async Task CreateCommand_Conflict()
    {
        var id = "8f522008-b431-4b63-93c2-c39eab3db43d";
        var partitionKey = "52fe466c-52aa-4daf-8e16-a93b26680510";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        using var createCommand1 = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand1.Item.PublicMessage = "Public Message #1";
        createCommand1.Item.PrivateMessage = "Private Message #1";

        // Save the command and capture the result
        var created1 = await createCommand1.SaveAsync(
            cancellationToken: default);

        Assert.That(created1, Is.Not.Null);

        // Create another command with the same id (will conflict)
        using var createCommand2 = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set the same message on the second command
        createCommand2.Item.PublicMessage = "Public Message #1";
        createCommand2.Item.PrivateMessage = "Private Message #1";

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
    [Description("Tests create command save operation with optional message")]
    public async Task CreateCommand_OptionalMessage()
    {
        var id = "2a4cb3ec-6624-4fc6-abc4-6a5db019f8f9";
        var partitionKey = "b297ff5b-2ab5-4b8d-9dfd-57d2e1d8c3d2";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        using var createCommand = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";
        createCommand.Item.OptionalMessage = "Optional Message #1";

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
                    using (Assert.EnterMultipleScope())
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
                    }
                }));
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
        using var createCommand = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";

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
                    using (Assert.EnterMultipleScope())
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
                    }
                }));
    }
}
