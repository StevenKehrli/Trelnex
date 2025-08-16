using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

[Category("PropertyChanges")]
public class PropertyChangesTests
{
    [Test]
    [Description("Tests that property changes are tracked correctly when adding an item to an existing array")]
    public async Task PropertyChanges_Array_AddItem()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set an array with initial items
        createCommand.Item.TrackedSettingsArray =
        [
            new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        ];

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // add a new item to the array
        updateCommand!.Item.TrackedSettingsArray =
        [
            updateCommand.Item.TrackedSettingsArray[0],
            updateCommand.Item.TrackedSettingsArray[1],
            new TrackedSettings { SettingId = 103, PrimaryValue = "Primary Value #3", SecondaryValue = "Secondary Value #3" }
        ];

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show addition of new array item: "/trackedSettingsArray/2/settingId", etc.
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when creating a new array with initial items")]
    public async Task PropertyChanges_Array_CreateNew()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set an array with initial items
        createCommand.Item.TrackedSettingsArray =
        [
            new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        ];

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show JSON pointer paths for each array item: "/trackedSettingsArray/0/settingId", etc.
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when modifying properties of an existing array item")]
    public async Task PropertyChanges_Array_ModifyItem()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set an array with initial items
        createCommand.Item.TrackedSettingsArray =
        [
            new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        ];

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // modify properties of the first array item
        updateCommand!.Item.TrackedSettingsArray[0].SettingId = 103;
        updateCommand!.Item.TrackedSettingsArray[0].PrimaryValue = "Primary Value #3";
        updateCommand!.Item.TrackedSettingsArray[0].SecondaryValue = "Secondary Value #3";
        // leave second item unchanged

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show changes only for modified properties: "/trackedSettingsArray/0/primaryValue", "/trackedSettingsArray/0/settingId"
        // but NOT "/trackedSettingsArray/1/*" or "/trackedSettingsArray/0/secondaryValue"
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when removing an item from an array")]
    public async Task PropertyChanges_Array_RemoveItem()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set an array with initial items
        createCommand.Item.TrackedSettingsArray =
        [
            new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" },
            new TrackedSettings { SettingId = 103, PrimaryValue = "Primary Value #3", SecondaryValue = "Secondary Value #3" }
        ];

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // remove the middle item from the array
        updateCommand!.Item.TrackedSettingsArray =
        [
            updateCommand.Item.TrackedSettingsArray[0],
            updateCommand.Item.TrackedSettingsArray[2] // skip item at index 1
        ];

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show removal of item at index 1 and reindexing of remaining items
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when reordering array items")]
    public async Task PropertyChanges_Array_ReorderItems()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set an array with initial items in specific order
        createCommand.Item.TrackedSettingsArray =
        [
            new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" },
            new TrackedSettings { SettingId = 103, PrimaryValue = "Primary Value #3", SecondaryValue = "Secondary Value #3" }
        ];

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // reorder the array items (reverse the order)
        updateCommand!.Item.TrackedSettingsArray =
        [
            updateCommand.Item.TrackedSettingsArray[2], // Third Item -> position 0
            updateCommand.Item.TrackedSettingsArray[1], // Second Item -> position 1
            updateCommand.Item.TrackedSettingsArray[0]  // First Item -> position 2
        ];

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show changes for all array positions reflecting the reordering
        // "/trackedSettingsArray/0/*", "/trackedSettingsArray/1/*", "/trackedSettingsArray/2/*"
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when replacing an entire array")]
    public async Task PropertyChanges_Array_ReplaceEntireArray()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set an array with initial items
        createCommand.Item.TrackedSettingsArray =
        [
            new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        ];

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // replace the entire array with completely new items
        updateCommand!.Item.TrackedSettingsArray =
        [
            new TrackedSettings { SettingId = 201, PrimaryValue = "Replace Primary Value #1", SecondaryValue = "Replace Secondary Value #1" },
            new TrackedSettings { SettingId = 202, PrimaryValue = "Replace Primary Value #2", SecondaryValue = "Replace Secondary Value #2" }
        ];

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show changes for all array positions and addition of new item at index 2
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when setting an existing array to null")]
    public async Task PropertyChanges_Array_SetToNull()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set an array with initial items
        createCommand.Item.TrackedSettingsArray =
        [
            new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        ];

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // set the array to null
        updateCommand!.Item.TrackedSettingsArray = null!;

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show removal changes for all array items with OldValue containing the original items and NewValue as null
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that no property changes are tracked when properties are set to their default values")]
    public async Task PropertyChanges_Basic_NoChanges()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set properties to their default values - should not be tracked as changes
        createCommand.Item.PublicId = 0;
        createCommand.Item.PublicMessage = null!;

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // verify that no properties were changed
        Assert.That(propertyChanges, Is.Null);
    }

    [Test]
    [Description("Tests that property changes are not tracked when properties are set and then reset to their original values")]
    public async Task PropertyChanges_Basic_SetAndReset()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first set properties to new values
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";

        // then reset them back to their default values
        createCommand.Item.PublicId = 0;
        createCommand.Item.PublicMessage = null!;

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // verify that no properties were changed since they were reset to original values
        Assert.That(propertyChanges, Is.Null);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when properties are modified")]
    public async Task PropertyChanges_Basic_SimpleProperties()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set properties to new values that should be tracked
        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";

        // this is intentional - PrivateMessage is not tracked and should not be in the property changes
        createCommand.Item.PrivateMessage = "Private #1";

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when adding an item to an existing dictionary")]
    public async Task PropertyChanges_Dictionary_AddItem()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a dictionary with initial items
        createCommand.Item.TrackedSettingsDictionary = new Dictionary<string, TrackedSettings>
        {
            ["setting1"] = new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            ["setting2"] = new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // add a new item to the dictionary
        updateCommand!.Item.TrackedSettingsDictionary["setting3"] = new TrackedSettings
        {
            SettingId = 103,
            PrimaryValue = "Primary Value #3",
            SecondaryValue = "Secondary Value #3"
        };

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show addition of new dictionary item: "/trackedSettingsDictionary/setting3/settingId", etc.
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when creating a new dictionary with initial items")]
    public async Task PropertyChanges_Dictionary_CreateNew()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set a dictionary with initial items
        createCommand.Item.TrackedSettingsDictionary = new Dictionary<string, TrackedSettings>
        {
            ["setting1"] = new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            ["setting2"] = new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        };

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show JSON pointer paths for each dictionary item: "/trackedSettingsDictionary/setting1/settingId", etc.
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when modifying properties of an existing dictionary item")]
    public async Task PropertyChanges_Dictionary_ModifyItem()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a dictionary with initial items
        createCommand.Item.TrackedSettingsDictionary = new Dictionary<string, TrackedSettings>
        {
            ["setting1"] = new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            ["setting2"] = new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // modify properties of the first dictionary item
        updateCommand!.Item.TrackedSettingsDictionary["setting1"].SettingId = 103;
        updateCommand!.Item.TrackedSettingsDictionary["setting1"].PrimaryValue = "Primary Value #3";
        updateCommand!.Item.TrackedSettingsDictionary["setting1"].SecondaryValue = "Secondary Value #3";
        // leave second item unchanged

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show changes only for modified properties: "/trackedSettingsDictionary/setting1/primaryValue", "/trackedSettingsDictionary/setting1/settingId"
        // but NOT "/trackedSettingsDictionary/setting2/*" or "/trackedSettingsDictionary/setting1/secondaryValue"
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when removing an item from a dictionary")]
    public async Task PropertyChanges_Dictionary_RemoveItem()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a dictionary with initial items
        createCommand.Item.TrackedSettingsDictionary = new Dictionary<string, TrackedSettings>
        {
            ["setting1"] = new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            ["setting2"] = new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" },
            ["setting3"] = new TrackedSettings { SettingId = 103, PrimaryValue = "Primary Value #3", SecondaryValue = "Secondary Value #3" }
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // remove the middle item from the dictionary
        updateCommand!.Item.TrackedSettingsDictionary = new Dictionary<string, TrackedSettings>
        {
            ["setting1"] = updateCommand.Item.TrackedSettingsDictionary["setting1"],
            ["setting3"] = updateCommand.Item.TrackedSettingsDictionary["setting3"]
            // skip setting2
        };

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show removal of setting2: "/trackedSettingsDictionary/setting2/*" with null NewValues
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when replacing an entire dictionary")]
    public async Task PropertyChanges_Dictionary_ReplaceEntireDictionary()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a dictionary with initial items
        createCommand.Item.TrackedSettingsDictionary = new Dictionary<string, TrackedSettings>
        {
            ["setting1"] = new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            ["setting2"] = new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // replace the entire dictionary with completely new items
        updateCommand!.Item.TrackedSettingsDictionary = new Dictionary<string, TrackedSettings>
        {
            ["newSetting1"] = new TrackedSettings { SettingId = 201, PrimaryValue = "Replace Primary Value #1", SecondaryValue = "Replace Secondary Value #1" },
            ["newSetting2"] = new TrackedSettings { SettingId = 202, PrimaryValue = "Replace Primary Value #2", SecondaryValue = "Replace Secondary Value #2" }
        };

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show removal of old keys and addition of new keys
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when setting an existing dictionary to null")]
    public async Task PropertyChanges_Dictionary_SetToNull()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a dictionary with initial items
        createCommand.Item.TrackedSettingsDictionary = new Dictionary<string, TrackedSettings>
        {
            ["setting1"] = new TrackedSettings { SettingId = 101, PrimaryValue = "Primary Value #1", SecondaryValue = "Secondary Value #1" },
            ["setting2"] = new TrackedSettings { SettingId = 102, PrimaryValue = "Primary Value #2", SecondaryValue = "Secondary Value #2" }
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // set the dictionary to null
        updateCommand!.Item.TrackedSettingsDictionary = null!;

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show removal changes for all dictionary items with OldValue containing the original items and NewValue as null
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when a new nested object is created")]
    public async Task PropertyChanges_NestedObject_CreateNew()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set a nested object with initial values
        createCommand.Item.TrackedSettingsWithAttribute = new TrackedSettings
        {
            SettingId = 101,
            PrimaryValue = "Primary Value #1",
            SecondaryValue = "Secondary Value #1"
        };

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show JSON pointer paths for each leaf property: "/trackedSettingsWithAttribute/settingId", etc.
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when modifying an existing property in a nested object")]
    public async Task PropertyChanges_NestedObject_ModifyExistingProperty()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a nested object with initial values
        createCommand.Item.TrackedSettingsWithAttribute = new TrackedSettings
        {
            SettingId = 101,
            PrimaryValue = "Primary Value #1",
            SecondaryValue = "Secondary Value #1"
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // now modify just one property in the existing nested object
        updateCommand!.Item.TrackedSettingsWithAttribute.PrimaryValue = "Primary Value #2";

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show only the changed property: "/trackedSettingsWithAttribute/primaryValue"
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when modifying multiple properties in the same nested object")]
    public async Task PropertyChanges_NestedObject_MultipleProperties()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a nested object with initial values
        createCommand.Item.TrackedSettingsWithAttribute = new TrackedSettings
        {
            SettingId = 101,
            PrimaryValue = "Primary Value #1",
            SecondaryValue = "Secondary Value #1"
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // modify multiple properties in the same nested object
        updateCommand!.Item.TrackedSettingsWithAttribute.SettingId = 201;
        updateCommand.Item.TrackedSettingsWithAttribute.PrimaryValue = "Primary Value #2";
        // intentionally leave SecondaryValue unchanged

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show changes for only the modified properties:
        // "/trackedSettingsWithAttribute/settingId" and "/trackedSettingsWithAttribute/primaryValue"
        // but NOT "/trackedSettingsWithAttribute/secondaryValue"
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when replacing an entire nested object")]
    public async Task PropertyChanges_NestedObject_ReplaceEntireObject()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a nested object with initial values
        createCommand.Item.TrackedSettingsWithAttribute = new TrackedSettings
        {
            SettingId = 101,
            PrimaryValue = "Primary Value #1",
            SecondaryValue = "Secondary Value #1"
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // replace the entire nested object with a completely new one
        updateCommand!.Item.TrackedSettingsWithAttribute = new TrackedSettings
        {
            SettingId = 201,
            PrimaryValue = "Primary Value #2",
            SecondaryValue = "Secondary Value #2"
        };

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show changes for all properties: "/trackedSettingsWithAttribute/settingId",
        // "/trackedSettingsWithAttribute/primaryValue", "/trackedSettingsWithAttribute/secondaryValue"
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that property changes are tracked correctly when setting an existing nested object to null")]
    public async Task PropertyChanges_NestedObject_SetToNull()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create | CommandOperations.Update);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // first, set a nested object with initial values
        createCommand.Item.TrackedSettingsWithAttribute = new TrackedSettings
        {
            SettingId = 101,
            PrimaryValue = "Primary Value #1",
            SecondaryValue = "Secondary Value #1"
        };

        // save the createCommand - this simulates persisting the initial state
        await createCommand.SaveAsync(CancellationToken.None);

        // create an update command to modify the existing item
        using var updateCommand = await dataProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        // set the nested object to null
        updateCommand!.Item.TrackedSettingsWithAttribute = null!;

        // get the property changes from the proxy manager
        var propertyChanges = (updateCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show removal changes for all properties: "/trackedSettingsWithAttribute/settingId",
        // "/trackedSettingsWithAttribute/primaryValue", "/trackedSettingsWithAttribute/secondaryValue"
        // with OldValue containing the original values and NewValue as null
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that properties are tracked correctly when parent has Track and children have Track")]
    public async Task PropertyChanges_TrackHierarchy_TrackedWithAttribute()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set TrackedSettingsWithAttribute - should track all properties since both parent and children have [Track]
        createCommand.Item.TrackedSettingsWithAttribute = new TrackedSettings
        {
            SettingId = 101,
            PrimaryValue = "Primary Value #1",
            SecondaryValue = "Secondary Value #1"
        };

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show all leaf properties: "/trackedSettingsWithAttribute/settingId",
        // "/trackedSettingsWithAttribute/primaryValue", "/trackedSettingsWithAttribute/secondaryValue"
        Snapshot.Match(propertyChanges);
    }

    [Test]
    [Description("Tests that properties are NOT tracked when parent lacks Track even though children have Track")]
    public async Task PropertyChanges_TrackHierarchy_TrackedWithoutAttribute()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set TrackedSettingsWithoutAttribute - should NOT track any properties
        // because parent property lacks [Track] even though children have [Track]
        createCommand.Item.TrackedSettingsWithoutAttribute = new TrackedSettings
        {
            SettingId = 101,
            PrimaryValue = "Primary Value #1",
            SecondaryValue = "Secondary Value #1"
        };

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show NO property changes because parent property is not marked with [Track]
        Assert.That(propertyChanges, Is.Null);
    }

    [Test]
    [Description("Tests that only object assignment is tracked when parent has Track but children lack Track")]
    public async Task PropertyChanges_TrackHierarchy_UntrackedWithAttribute()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // get a data provider for our test item type
        var factory = await InMemoryDataProviderFactory.Create();

        // Get a data provider for our test item type
        var dataProvider = factory.Create<TestItem>(
            typeName: "test-item",
            commandOperations: CommandOperations.Create);

        using var createCommand = dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // set UntrackedSettingsWithAttribute - should track object assignment only
        // because parent has [Track] but children lack [Track]
        createCommand.Item.UntrackedSettingsWithAttribute = new UntrackedSettings
        {
            SettingId = 101,
            PrimaryValue = "Primary Value #1",
            SecondaryValue = "Secondary Value #1"
        };

        // get the property changes from the proxy manager
        var propertyChanges = (createCommand as SaveCommand<TestItem>)!.GetPropertyChanges();

        // use Snapshooter to match the property changes with the expected output
        // should show NO leaf properties because UntrackedSettings properties lack [Track]
        // TrackResolver should filter out properties without [Track]
        Snapshot.Match(propertyChanges);
    }
}
