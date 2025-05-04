using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Events;

[Category("Events")]
public class DeleteCommandEventTests
{
    [Test]
    [Description("Tests that delete commands generate proper events with correct change tracking")]
    public async Task DeleteCommandEvent()
    {
        var id = "bc4971e4-6dae-45ba-b3ee-43c036e0d957";
        var partitionKey = "60525a8e-b084-4a37-b461-d08330760ef2";

        var startDateTime = DateTime.UtcNow;

        // Create test request context
        var requestContext = TestRequestContext.Create();

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
            requestContext: requestContext,
            cancellationToken: default);

        // Get a delete command for the same item
        var deleteCommand = await commandProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the deletion
        await deleteCommand.SaveAsync(
            requestContext: requestContext,
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
                        var currentDateTime = DateTime.UtcNow;

                        // Verify first event properties (create event)
                        // id
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Id"),
                            Is.Not.Default);

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].CreatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].UpdatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("[0].UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<Guid>("[0].ETag"),
                            Is.Not.Default);

                        // context.objectId
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.ObjectId"),
                            Is.Not.Default);

                        // context.httpTraceIdentifier
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.HttpTraceIdentifier"),
                            Is.Not.Default);

                        // context.httpRequestPath
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.HttpRequestPath"),
                            Is.Not.Default);

                        // Verify second event properties (delete event)
                        // id
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Id"),
                            Is.Not.Default);

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].CreatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].UpdatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("[1].CreatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<Guid>("[1].ETag"),
                            Is.Not.Default);

                        // Verify context values consistency between events
                        // context.objectId
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Context.ObjectId"),
                            Is.Not.Default);

                        // context.httpTraceIdentifier
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Context.HttpTraceIdentifier"),
                            Is.Not.Default);

                        // context.httpRequestPath
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Context.HttpRequestPath"),
                            Is.Not.Default);

                        // context values should be the same between events
                        // context.objectId
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.ObjectId"),
                            Is.EqualTo(fieldOption.Field<Guid>("[1].Context.ObjectId")));

                        // context.httpTraceIdentifier
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.HttpTraceIdentifier"),
                            Is.EqualTo(fieldOption.Field<Guid>("[1].Context.HttpTraceIdentifier")));

                        // context.httpRequestPath
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.HttpRequestPath"),
                            Is.EqualTo(fieldOption.Field<Guid>("[1].Context.HttpRequestPath")));
                    });
                }));
    }
}
