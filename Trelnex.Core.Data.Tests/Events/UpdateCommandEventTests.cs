using System.Diagnostics;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Events;

[Category("Events")]
public class UpdateCommandEventTests
{
    [Test]
    [Description("Tests that update commands generate proper events with correct change tracking")]
    public async Task UpdateCommandEvent()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(CreateCommandEventTests));
        using var activity = activitySource.StartActivity();

        var id = "404d6b21-f7ba-48c4-813c-7d3b5bf4f549";
        var partitionKey = "d9a7a840-ce5c-43c9-9839-a8432068b197";

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get an update command for the same item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        // Update the test item values
        updateCommand.Item.PublicId = 2;
        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // Save the updated state
        await updateCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = (dataProvider as InMemoryDataProvider<ITestItem, TestItem>)!.GetEvents();

        // Verify the changes in the events
        // Snapshooter does a poor job of the serialization of dynamic
        // so we do explicit checks of the changes array
        Assert.Multiple(() =>
        {
            // Check first event changes (create event)
            Assert.That(
                events[0].Changes!,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].Changes![0].OldValue!.GetInt32(),
                Is.EqualTo(0));

            Assert.That(
                events[0].Changes![0].NewValue!.GetInt32(),
                Is.EqualTo(1));

            Assert.That(
                events[0].Changes![1].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![1].NewValue!.GetString(),
                Is.EqualTo("Public #1"));

            // Check second event changes (update event)
            Assert.That(
                events[1].Changes![0].OldValue!.GetInt32(),
                Is.EqualTo(1));

            Assert.That(
                events[1].Changes![0].NewValue!.GetInt32(),
                Is.EqualTo(2));

            Assert.That(
                events[1].Changes![1].OldValue!.GetString(),
                Is.EqualTo("Public #1"));

            Assert.That(
                events[1].Changes![1].NewValue!.GetString(),
                Is.EqualTo("Public #2"));
        });

        // Use Snapshooter to verify the event structure
        // Ignoring the Changes field as it's verified separately above
        Snapshot.Match(
            events,
            matchOptions => matchOptions
                .IgnoreField("**.Changes")
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;

                        // Verify first event properties (create event)
                        // id
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Id"),
                            Is.Not.Default);

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[0].CreatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[0].UpdatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[0].CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("[0].UpdatedDateTimeOffset")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<Guid>("[0].ETag"),
                            Is.Not.Default);

                        // traceContext
                        Assert.That(
                            fieldOption.Field<string>("[0].TraceContext"),
                            Does.Match("00-[0-9a-f]{32}-[0-9a-f]{16}-01"));

                        // traceId
                        Assert.That(
                            fieldOption.Field<string>("[0].TraceId"),
                            Does.Match("[0-9a-f]{32}"));

                        // spanId
                        Assert.That(
                            fieldOption.Field<string>("[0].SpanId"),
                            Does.Match("[0-9a-f]{16}"));

                        // Verify second event properties (update event)
                        // id
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Id"),
                            Is.Not.Default);

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[1].CreatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[1].UpdatedDateTimeOffset"),
                            Is.InRange(startDateTimeOffset, currentDateTimeOffset));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTimeOffset>("[1].CreatedDateTimeOffset"),
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("[1].UpdatedDateTimeOffset")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<Guid>("[1].ETag"),
                            Is.Not.Default);

                        // Verify context values consistency between events
                        // traceContext
                        Assert.That(
                            fieldOption.Field<string>("[1].TraceContext"),
                            Is.EqualTo(fieldOption.Field<string>("[0].TraceContext")));

                        // traceId
                        Assert.That(
                            fieldOption.Field<string>("[1].TraceId"),
                            Is.EqualTo(fieldOption.Field<string>("[0].TraceId")));

                        // spanId
                        Assert.That(
                            fieldOption.Field<string>("[1].SpanId"),
                            Is.EqualTo(fieldOption.Field<string>("[0].SpanId")));
                    });
                }));
    }
}
