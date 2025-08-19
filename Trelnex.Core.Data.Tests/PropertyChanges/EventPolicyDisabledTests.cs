using System.Diagnostics;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

public abstract partial class EventPolicyTests
{
    [Test]
    [Description("Tests that create commands do not generate events when EventPolicy is Disabled")]
    public async Task EventPolicy_Disabled_CreateCommand()
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
            eventPolicy: EventPolicy.Disabled);

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

        Assert.That(
            events,
            Is.Empty,
            "No events should be generated when EventPolicy is Disabled");
    }

    [Test]
    [Description("Tests that delete commands do not generate events when EventPolicy is Disabled")]
    public async Task EventPolicy_Disabled_DeleteCommand()
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
            eventPolicy: EventPolicy.Disabled);

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

        Assert.That(
            events,
            Is.Empty,
            "No events should be generated when EventPolicy is Disabled");
    }

    [Test]
    [Description("Tests that update commands do not generate events when EventPolicy is Disabled")]
    public async Task EventPolicy_Disabled_UpdateCommand()
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
            eventPolicy: EventPolicy.Disabled);

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

        Assert.That(
            events,
            Is.Empty,
            "No events should be generated when EventPolicy is Disabled");
    }
}
