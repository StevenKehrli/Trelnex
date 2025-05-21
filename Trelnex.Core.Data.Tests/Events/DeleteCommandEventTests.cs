using System.Diagnostics;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Events;

[Category("Events")]
public class DeleteCommandEventTests
{
    [Test]
    [Description("Tests that delete commands generate proper events with correct change tracking")]
    public async Task DeleteCommandEvent()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(CreateCommandEventTests));
        using var activity = activitySource.StartActivity();

        var id = "bc4971e4-6dae-45ba-b3ee-43c036e0d957";
        var partitionKey = "60525a8e-b084-4a37-b461-d08330760ef2";

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type with delete operations
        var commandProvider = factory.Create<ITestItem, TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Delete);

        // Create a new command to create our test item
        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get a delete command for the same item
        var deleteCommand = await commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the deletion
        await deleteCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the command provider
        var events = (commandProvider as InMemoryCommandProvider<ITestItem, TestItem>)!.GetEvents();

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

            // Check second event changes (delete event)
            // Delete events don't have changes
            Assert.That(
                events[1].Changes!,
                Is.Null);
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

                        // Verify second event properties (delete event)
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
                            Is.EqualTo(fieldOption.Field<DateTimeOffset>("[1].CreatedDateTimeOffset")));

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
