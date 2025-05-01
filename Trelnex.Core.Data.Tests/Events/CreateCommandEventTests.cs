using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Events;

[Category("Events")]
public class CreateCommandEventTests
{
    [Test]
    [Description("Tests that create commands generate proper events with correct change tracking")]
    public async Task CreateCommandEvent()
    {
        var id = "569d9c11-f66f-46b9-98be-ff0cff833475";
        var partitionKey = "05f393b2-72af-409e-9186-0679773e9c55";

        var startDateTime = DateTime.UtcNow;

        // Create test request context
        var requestContext = TestRequestContext.Create();

        // Create our in-memory command provider factory
        var factory = await InMemoryCommandProviderFactory.Create();

        // Get a command provider for our test item type
        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: "test-item");

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

        // Get the events from the command provider
        var events = (commandProvider as InMemoryCommandProvider<ITestItem, TestItem>)!.GetEvents();

        // Verify the changes in the events
        // Snapshooter does a poor job of the serialization of dynamic
        // so we do explicit checks of the changes array
        Assert.Multiple(() =>
        {
            // Check event changes (create event)
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

                        // Verify event properties (create event)
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

                        // Verify context values
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
                    });
                }));
    }
}
