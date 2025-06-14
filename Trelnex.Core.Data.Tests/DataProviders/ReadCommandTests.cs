using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.DataProviders;

public abstract partial class DataProviderTests
{
    [Test]
    [Description("Tests read command operation")]
    public async Task ReadCommand_ReadAsync()
    {
        var id = "a8cf4bc4-745a-471c-8fb1-5d4e124bbde2";
        var partitionKey = "2b541bfd-4605-48f9-b1bc-5aba5f64cd24";

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

        // Read the item using the data provider
        using var read = await _dataProvider.ReadAsync(
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
        using var read = await _dataProvider.ReadAsync(
            id: id,
            partitionKey: partitionKey);

        // Verify that no item was found
        Assert.That(read, Is.Null);
    }
}
