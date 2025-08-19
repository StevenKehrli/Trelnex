using System.Diagnostics;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

public abstract partial class EventPolicyTests
{
    [Test]
    [Description("Tests that create commands generate changes for properties with [Track] when EventPolicy is OnlyTrackAttributeChanges")]
    public async Task EventPolicy_OnlyTrackAttributeChanges_CreateCommand()
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
            eventPolicy: EventPolicy.OnlyTrackAttributeChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "PublicMessage #1";
        createCommand.Item.PrivateMessage = "PrivateMessage #1";
        createCommand.Item.OptionalMessage = "OptionalMessage #1";

        // Save the initial state
        await createCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = await GetItemEventsAsync(id, partitionKey);

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
                Is.EqualTo("/publicMessage"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("PublicMessage #1"));
        }
    }

    [Test]
    [Description("Tests that delete commands generate changes for properties with [Track] when EventPolicy is OnlyTrackAttributeChanges")]
    public async Task EventPolicy_OnlyTrackAttributeChanges_DeleteCommand()
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
            eventPolicy: EventPolicy.OnlyTrackAttributeChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "PublicMessage #1";
        createCommand.Item.PrivateMessage = "PrivateMessage #1";
        createCommand.Item.OptionalMessage = "OptionalMessage #1";

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
        var events = await GetItemEventsAsync(id, partitionKey);

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
                Is.EqualTo("/publicMessage"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("PublicMessage #1"));

            Assert.That(
                events[1].SaveAction,
                Is.EqualTo(SaveAction.DELETED));

            Assert.That(
                events[1].Changes,
                Is.Null);
        }
    }

    [Test]
    [Description("Tests that update commands generate changes for properties with [Track] when EventPolicy is OnlyTrackAttributeChanges")]
    public async Task EventPolicy_OnlyTrackAttributeChanges_UpdateCommand()
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
            eventPolicy: EventPolicy.OnlyTrackAttributeChanges);

        // Create a new command to create our test item
        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "PublicMessage #1";
        createCommand.Item.PrivateMessage = "PrivateMessage #1";
        createCommand.Item.OptionalMessage = "OptionalMessage #1";

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
        updateCommand.Item.PublicMessage = "PublicMessage #2";
        updateCommand.Item.PrivateMessage = "PrivateMessage #2";
        updateCommand.Item.OptionalMessage = "OptionalMessage #2";

        // Save the updated state
        await updateCommand.SaveAsync(
            cancellationToken: default);

        // Get the events from the data provider
        var events = await GetItemEventsAsync(id, partitionKey);

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
                Is.EqualTo("/publicMessage"));

            Assert.That(
                events[0].Changes![0].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![0].NewValue!.GetString(),
                Is.EqualTo("PublicMessage #1"));

            Assert.That(
                events[1].SaveAction,
                Is.EqualTo(SaveAction.UPDATED));

            Assert.That(
                events[1].Changes,
                Has.Length.EqualTo(1));

            Assert.That(
                events[1].Changes![0].PropertyName,
                Is.EqualTo("/publicMessage"));

            Assert.That(
                events[1].Changes![0].OldValue!.GetString(),
                Is.EqualTo("PublicMessage #1"));

            Assert.That(
                events[1].Changes![0].NewValue!.GetString(),
                Is.EqualTo("PublicMessage #2"));
        }
    }
}
