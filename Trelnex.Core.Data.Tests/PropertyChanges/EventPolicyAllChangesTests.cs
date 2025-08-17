using System.Diagnostics;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

public abstract partial class EventPolicyTests
{
    [Test]
    [Description("Tests that create commands generate changes for properties without [NoTrack] when EventPolicy is AllChanges")]
    public async Task EventPolicy_AllChanges_CreateCommand()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create,
            eventPolicy: EventPolicy.AllChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";
        createCommand.Item.TrackMessage = "TrackMessage #1";
        createCommand.Item.DoNotTrackMessage = "DoNotTrackMessage #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(1));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].Changes![0].PropertyName,
                Is.EqualTo("/message"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Message #1"));

            Assert.That(
                events[0].Changes![1].PropertyName,
                Is.EqualTo("/trackMessage"));

            Assert.That(
                events[0].Changes![1].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![1].NewValue!.GetString(),
                Is.EqualTo("TrackMessage #1"));
        }
    }

    [Test]
    [Description("Tests that create commands generate encrypted changes for properties with [Encrypt]")]
    public async Task EventPolicy_AllChanges_CreateCommand_WithEncryption()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create the AesGcmCipher instance with the defined secret.
        var cipherConfiguration = new AesGcmCipherConfiguration
        {
            Secret = "3d45c4b2-dc1c-42ef-8b5f-efe348407214"
        };

        var cipher = new AesGcmCipher(cipherConfiguration);

        // Create the BlockCipherService with the cipher.
        var blockCipherService = new BlockCipherService(cipher);

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create,
            eventPolicy: EventPolicy.AllChanges,
            blockCipherService: blockCipherService);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.EncryptedMessage = "EncryptedMessage #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(1));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(1));

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
                Is.EqualTo("EncryptedMessage #1"));
        }
    }

    [Test]
    [Description("Tests that delete commands generate changes for properties without [NoTrack] when EventPolicy is AllChanges")]
    public async Task EventPolicy_AllChanges_DeleteCommand()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Delete,
            eventPolicy: EventPolicy.AllChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";
        createCommand.Item.TrackMessage = "TrackMessage #1";
        createCommand.Item.DoNotTrackMessage = "DoNotTrackMessage #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get a delete command for the same item
        using var deleteCommand = await dataProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the deletion
        await deleteCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].Changes![0].PropertyName,
                Is.EqualTo("/message"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Message #1"));

            Assert.That(
                events[0].Changes![1].PropertyName,
                Is.EqualTo("/trackMessage"));

            Assert.That(
                events[0].Changes![1].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![1].NewValue!.GetString(),
                Is.EqualTo("TrackMessage #1"));

            Assert.That(
                events[1].SaveAction,
                Is.EqualTo(SaveAction.DELETED));

            Assert.That(
                events[1].Changes,
                Is.Null);
        }
    }

    [Test]
    [Description("Tests that delete commands generate encrypted changes for properties with [Encrypt]")]
    public async Task EventPolicy_AllChanges_DeleteCommand_WithEncryption()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create the AesGcmCipher instance with the defined secret.
        var cipherConfiguration = new AesGcmCipherConfiguration
        {
            Secret = "d1bab44b-aad6-4c5e-9de9-ed5dd8a882d4"
        };

        var cipher = new AesGcmCipher(cipherConfiguration);

        // Create the BlockCipherService with the cipher.
        var blockCipherService = new BlockCipherService(cipher);

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Delete,
            eventPolicy: EventPolicy.AllChanges,
            blockCipherService: blockCipherService);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.EncryptedMessage = "EncryptedMessage #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get a delete command for the same item
        using var deleteCommand = await dataProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the deletion
        await deleteCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(1));

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
                Is.EqualTo("EncryptedMessage #1"));

            Assert.That(
                events[1].SaveAction,
                Is.EqualTo(SaveAction.DELETED));

            Assert.That(
                events[1].Changes,
                Is.Null);
        }
    }

    [Test]
    [Description("Tests that update commands generate changes for properties without [NoTrack] when EventPolicy is AllChanges")]
    public async Task EventPolicy_AllChanges_UpdateCommand()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update,
            eventPolicy: EventPolicy.AllChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";
        createCommand.Item.TrackMessage = "TrackMessage #1";
        createCommand.Item.DoNotTrackMessage = "DoNotTrackMessage #1";

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
        updateCommand.Item.Message = "Message #2";
        updateCommand.Item.TrackMessage = "TrackMessage #2";
        updateCommand.Item.DoNotTrackMessage = "DoNotTrackMessage #2";

        // Save the updated state
        await updateCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].Changes![0].PropertyName,
                Is.EqualTo("/message"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Message #1"));

            Assert.That(
                events[0].Changes![1].PropertyName,
                Is.EqualTo("/trackMessage"));

            Assert.That(
                events[0].Changes![1].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![1].NewValue!.GetString(),
                Is.EqualTo("TrackMessage #1"));

            Assert.That(
                events[1].SaveAction,
                Is.EqualTo(SaveAction.UPDATED));

            Assert.That(
                events[1].Changes,
                Has.Length.EqualTo(2));

            Assert.That(
                events[1].Changes![0].PropertyName,
                Is.EqualTo("/message"));

            Assert.That(
                events[1].Changes![0].OldValue!.GetString(),
                Is.EqualTo("Message #1"));

            Assert.That(
                events[1].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Message #2"));

            Assert.That(
                events[1].Changes![1].PropertyName,
                Is.EqualTo("/trackMessage"));

            Assert.That(
                events[1].Changes![1].OldValue!.GetString(),
                Is.EqualTo("TrackMessage #1"));

            Assert.That(
                events[1].Changes![1].NewValue!.GetString(),
                Is.EqualTo("TrackMessage #2"));
        }
    }

    [Test]
    [Description("Tests that update commands generate encrypted changes for properties with [Encrypt]")]
    public async Task EventPolicy_AllChanges_UpdateCommand_WithEncryption()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Create the AesGcmCipher instance with the defined secret.
        var cipherConfiguration = new AesGcmCipherConfiguration
        {
            Secret = "32b8bbdc-e003-4be8-aa6b-64a9221c5b87"
        };

        var cipher = new AesGcmCipher(cipherConfiguration);

        // Create the BlockCipherService with the cipher.
        var blockCipherService = new BlockCipherService(cipher);

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update,
            eventPolicy: EventPolicy.AllChanges,
            blockCipherService: blockCipherService);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.EncryptedMessage = "EncryptedMessage #1";

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
        updateCommand.Item.EncryptedMessage = "EncryptedMessage #2";

        // Save the updated state
        await updateCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(1));

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
                Is.EqualTo("EncryptedMessage #1"));

            Assert.That(
                events[1].SaveAction,
                Is.EqualTo(SaveAction.UPDATED));

            Assert.That(
                events[1].Changes,
                Has.Length.EqualTo(1));

            Assert.That(
                events[1].Changes![0].PropertyName,
                Is.EqualTo("/encryptedMessage"));

            var plaintextOld1 = EncryptedJsonService.DecryptFromBase64<string>(
                events[1].Changes![0].OldValue!.GetString(),
                blockCipherService);

            Assert.That(
                plaintextOld1,
                Is.EqualTo("EncryptedMessage #1"));

            var plaintextNew1 = EncryptedJsonService.DecryptFromBase64<string>(
                events[1].Changes![0].NewValue!.GetString(),
                blockCipherService);

            Assert.That(
                plaintextNew1,
                Is.EqualTo("EncryptedMessage #2"));
        }
    }

    [Test]
    [Description("Tests that create commands generate changes for properties without [NoTrack] when EventPolicy is null (AllChanges)")]
    public async Task EventPolicy_Default_CreateCommand()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create,
            eventPolicy: EventPolicy.AllChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";
        createCommand.Item.TrackMessage = "TrackMessage #1";
        createCommand.Item.DoNotTrackMessage = "DoNotTrackMessage #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(1));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].Changes![0].PropertyName,
                Is.EqualTo("/message"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Message #1"));

            Assert.That(
                events[0].Changes![1].PropertyName,
                Is.EqualTo("/trackMessage"));

            Assert.That(
                events[0].Changes![1].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![1].NewValue!.GetString(),
                Is.EqualTo("TrackMessage #1"));
        }
    }

    [Description("Tests that delete commands generate changes for properties without [NoTrack] when EventPolicy is null (AllChanges)")]
    public async Task EventPolicy_Default_DeleteCommand()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Delete,
            eventPolicy: EventPolicy.AllChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";
        createCommand.Item.TrackMessage = "TrackMessage #1";
        createCommand.Item.DoNotTrackMessage = "DoNotTrackMessage #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get a delete command for the same item
        using var deleteCommand = await dataProvider.DeleteAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.Item, Is.Not.Null);

        // Save the deletion
        await deleteCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].Changes![0].PropertyName,
                Is.EqualTo("/message"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Message #1"));

            Assert.That(
                events[0].Changes![1].PropertyName,
                Is.EqualTo("/trackMessage"));

            Assert.That(
                events[0].Changes![1].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![1].NewValue!.GetString(),
                Is.EqualTo("TrackMessage #1"));

            Assert.That(
                events[1].SaveAction,
                Is.EqualTo(SaveAction.DELETED));

            Assert.That(
                events[1].Changes,
                Is.Null);
        }
    }

    [Test]
    [Description("Tests that update commands generate changes for properties without [NoTrack] when EventPolicy is null (AllChanges)")]
    public async Task EventPolicy_Default_UpdateCommand()
    {
        var activityListener = new ActivityListener
        {
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ShouldListenTo = _ => true
        };

        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource(nameof(EventPolicyTests));
        using var activity = activitySource.StartActivity();

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        var startDateTimeOffset = DateTimeOffset.UtcNow;

        // Get a data provider for our test item type
        var dataProvider = GetDataProvider(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update,
            eventPolicy: EventPolicy.AllChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.Message = "Message #1";
        createCommand.Item.TrackMessage = "TrackMessage #1";
        createCommand.Item.DoNotTrackMessage = "DoNotTrackMessage #1";

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
        updateCommand.Item.Message = "Message #2";
        updateCommand.Item.TrackMessage = "TrackMessage #2";
        updateCommand.Item.DoNotTrackMessage = "DoNotTrackMessage #2";

        // Save the updated state
        await updateCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = GetItemEvents(id, partitionKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                events,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].SaveAction,
                Is.EqualTo(SaveAction.CREATED));

            Assert.That(
                events[0].Changes,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].Changes![0].PropertyName,
                Is.EqualTo("/message"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Message #1"));

            Assert.That(
                events[0].Changes![1].PropertyName,
                Is.EqualTo("/trackMessage"));

            Assert.That(
                events[0].Changes![1].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![1].NewValue!.GetString(),
                Is.EqualTo("TrackMessage #1"));

            Assert.That(
                events[1].SaveAction,
                Is.EqualTo(SaveAction.UPDATED));

            Assert.That(
                events[1].Changes,
                Has.Length.EqualTo(2));

            Assert.That(
                events[1].Changes![0].PropertyName,
                Is.EqualTo("/message"));

            Assert.That(
                events[1].Changes![0].OldValue!.GetString(),
                Is.EqualTo("Message #1"));

            Assert.That(
                events[1].Changes![0].NewValue!.GetString(),
                Is.EqualTo("Message #2"));

            Assert.That(
                events[1].Changes![1].PropertyName,
                Is.EqualTo("/trackMessage"));

            Assert.That(
                events[1].Changes![1].OldValue!.GetString(),
                Is.EqualTo("TrackMessage #1"));

            Assert.That(
                events[1].Changes![1].NewValue!.GetString(),
                Is.EqualTo("TrackMessage #2"));
        }
    }
}
