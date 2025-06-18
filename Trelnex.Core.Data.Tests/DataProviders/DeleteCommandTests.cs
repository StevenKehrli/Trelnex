using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.DataProviders;

public abstract partial class DataProviderTests
{
    [Test]
    [Description("Tests delete command save operation")]
    public async Task DeleteCommand_SaveAsync()
    {
        var id = "f8829dac-56f6-4448-829a-fac886aefb1b";
        var partitionKey = "fbc8502a-38ee-4edb-8a2d-485888af5bd3";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        using var createCommand = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create a delete command for the item
        using var deleteCommand = await _dataProvider.DeleteAsync(
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

                        // Verify deleted date is within expected time range
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("Item.DeletedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // Verify ETag is present
                        Assert.That(
                            fieldOption.Field<string>("Item.ETag"),
                            Is.Not.Default);
                    }
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
        using var createCommand = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create two delete commands for the same item
        using var deleteCommand1 = await _dataProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand1, Is.Not.Null);
        Assert.That(deleteCommand1!.Item, Is.Not.Null);

        using var deleteCommand2 = await _dataProvider.DeleteAsync(
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
}
