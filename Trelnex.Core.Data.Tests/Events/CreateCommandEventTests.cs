using System.Diagnostics;
using Snapshooter.NUnit;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.Events;

[Category("Events")]
public class CreateCommandEventTests
{
    [Test]
    [Description("Tests that create commands generate proper events with correct change tracking")]
    public async Task CreateCommandEvent()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(CreateCommandEventTests));
        using var activity = activitySource.StartActivity();

        var id = "569d9c11-f66f-46b9-98be-ff0cff833475";
        var partitionKey = "05f393b2-72af-409e-9186-0679773e9c55";

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";
        createCommand.Item.EncryptedMessage = "Encrypted #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = (dataProvider as InMemoryDataProvider<TestItem>)!.GetEvents();

        // Verify the changes in the events
        // Snapshooter does a poor job of the serialization of dynamic
        // so we do explicit checks of the changes array
        using (Assert.EnterMultipleScope())
        {
            // Check event changes (create event)
            Assert.That(
                events[0].Changes!,
                Has.Length.EqualTo(3));

            Assert.That(
                events[0].Changes![0].PropertyName,
                Is.EqualTo("/encryptedMessage"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Encrypted #1"));

            Assert.That(
                events[0].Changes![1].PropertyName,
                Is.EqualTo("/publicId"));

            Assert.That(
                events[0].Changes![1].OldValue!.GetInt32(),
                Is.EqualTo(0));

            Assert.That(
                events[0].Changes![1].NewValue!.GetInt32(),
                Is.EqualTo(1));

            Assert.That(
                events[0].Changes![2].PropertyName,
                Is.EqualTo("/publicMessage"));

            Assert.That(
                events[0].Changes![2].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![2].NewValue!.GetString(),
                Is.EqualTo("Public #1"));
        }

        // Use Snapshooter to verify the event structure
        // Ignoring the Changes field as it's verified separately above
        Snapshot.Match(
            events,
            matchOptions => matchOptions
                .IgnoreField("**.Changes")
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;
                        var eventId = $@"EVENT^{id}^00000001";

                        // Verify event properties (create event)
                        // id
                        Assert.That(
                            fieldOption.Field<string>("[0].Id"),
                            Is.EqualTo(eventId));

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
                    }
                }));
    }

    [Test]
    [Description("Tests that create commands generate proper events with correct change tracking and encryption")]
    public async Task CreateCommandEvent_WithEncryption()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(CreateCommandEventTests));
        using var activity = activitySource.StartActivity();

        var id = "569d9c11-f66f-46b9-98be-ff0cff833475";
        var partitionKey = "05f393b2-72af-409e-9186-0679773e9c55";

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create our in-memory data provider factory
        var factory = await InMemoryDataProviderFactory.Create();

        // Create the AesGcmCipher instance with the defined secret.
        var cipherConfiguration = new AesGcmCipherConfiguration
        {
            Secret = "8bf0320a-86b0-4fa8-8179-f385c1f5c480"
        };

        var cipher = new AesGcmCipher(cipherConfiguration);

        // Create the BlockCipherService with the cipher.
        var blockCipherService = new BlockCipherService(cipher);

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create,
            blockCipherService: blockCipherService);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";
        createCommand.Item.EncryptedMessage = "Encrypted #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = (dataProvider as InMemoryDataProvider<TestItem>)!.GetEvents();

        // Verify the changes in the events
        // Snapshooter does a poor job of the serialization of dynamic
        // so we do explicit checks of the changes array
        using (Assert.EnterMultipleScope())
        {
            // Check event changes (create event)
            Assert.That(
                events[0].Changes!,
                Has.Length.EqualTo(3));

            Assert.That(
                events[0].Changes![0].PropertyName,
                Is.EqualTo("/encryptedMessage"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            var plaintextNew0 = EncryptedJsonService.DecryptFromBase64<string>(
                events[0].Changes![0].NewValue!.GetString(),
                blockCipherService);

            Assert.That(
                plaintextNew0,
                Is.EqualTo("Encrypted #1"));

            Assert.That(
                events[0].Changes![1].PropertyName,
                Is.EqualTo("/publicId"));

            Assert.That(
                events[0].Changes![1].OldValue!.GetInt32(),
                Is.EqualTo(0));

            Assert.That(
                events[0].Changes![1].NewValue!.GetInt32(),
                Is.EqualTo(1));

            Assert.That(
                events[0].Changes![2].PropertyName,
                Is.EqualTo("/publicMessage"));

            Assert.That(
                events[0].Changes![2].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![2].NewValue!.GetString(),
                Is.EqualTo("Public #1"));
        }

        // Use Snapshooter to verify the event structure
        // Ignoring the Changes field as it's verified separately above
        Snapshot.Match(
            events,
            matchOptions => matchOptions
                .IgnoreField("**.Changes")
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        var currentDateTimeOffset = DateTimeOffset.UtcNow;
                        var eventId = $@"EVENT^{id}^00000001";

                        // Verify event properties (create event)
                        // id
                        Assert.That(
                            fieldOption.Field<string>("[0].Id"),
                            Is.EqualTo(eventId));

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
                    }
                }));
    }
}
