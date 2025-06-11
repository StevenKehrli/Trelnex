using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.CommandProviders;

public abstract partial class CommandProviderTests
{
    [Test]
    [Description("Tests update command save operation")]
    public async Task UpdateCommand_SaveAsync()
    {
        var id = "7dded065-d204-4913-97ad-591e382baba5";
        var partitionKey = "48953713-d269-42c1-b803-593f8c027aef";

        // Track start time for timing assertions
        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create a command for creating a test item
        using var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create an update command for the item
        using var updateCommand = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Update message on the item
        updateCommand.Item.PublicMessage = "Public Message #2";
        updateCommand.Item.PrivateMessage = "Private Message #2";

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
        using var createCommand = _commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";

        // Save the create command
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Create two update commands for the same item
        using var updateCommand1 = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand1, Is.Not.Null);
        Assert.That(updateCommand1!.Item, Is.Not.Null);

        // Update message on the first update command
        updateCommand1.Item.PublicMessage = "Public Message #2";
        updateCommand1.Item.PrivateMessage = "Private Message #2";

        using var updateCommand2 = await _commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand2, Is.Not.Null);
        Assert.That(updateCommand2!.Item, Is.Not.Null);

        // Update message on the second update command
        updateCommand2.Item.PublicMessage = "Public Message #2";
        updateCommand2.Item.PrivateMessage = "Private Message #2";

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
