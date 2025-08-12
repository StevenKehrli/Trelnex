using System.Text.Json.Serialization;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

internal interface ITestItem : IBaseItem
{
    int PublicId { get; set; }

    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }

    // Tracked parent with tracked children - should track all changes
    TrackedSettings TrackedSettingsWithAttribute { get; set; }

    // Untracked parent with tracked children - should track nothing
    UntrackedSettings UntrackedSettingsWithAttribute { get; set; }

    // Tracked parent without attribute - should track nothing even though children have TrackChange
    TrackedSettings TrackedSettingsWithoutAttribute { get; set; }

    TrackedSettings[] TrackedSettingsArray { get; set; }

    Dictionary<string, TrackedSettings> TrackedSettingsDictionary { get; set; }
}

internal record TestItem : BaseItem, ITestItem
{
    [TrackChange]
    [JsonPropertyName("publicId")]
    public int PublicId { get; set; }

    [TrackChange]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;

    [TrackChange]
    [JsonPropertyName("trackedSettingsWithAttribute")]
    public TrackedSettings TrackedSettingsWithAttribute { get; set; } = null!;

    [TrackChange]
    [JsonPropertyName("untrackedSettingsWithAttribute")]
    public UntrackedSettings UntrackedSettingsWithAttribute { get; set; } = null!;

    // Note: No TrackChange attribute here
    [JsonPropertyName("trackedSettingsWithoutAttribute")]
    public TrackedSettings TrackedSettingsWithoutAttribute { get; set; } = null!;

    [TrackChange]
    [JsonPropertyName("trackedSettingsArray")]
    public TrackedSettings[] TrackedSettingsArray { get; set; } = null!;

    [TrackChange]
    [JsonPropertyName("trackedSettingsDictionary")]
    public Dictionary<string, TrackedSettings> TrackedSettingsDictionary { get; set; } = null!;
}

// Settings class where all properties have TrackChange
internal class TrackedSettings
{
    [TrackChange]
    [JsonPropertyName("settingId")]
    public int SettingId { get; set; }

    [TrackChange]
    [JsonPropertyName("primaryValue")]
    public string PrimaryValue { get; set; } = null!;

    [TrackChange]
    [JsonPropertyName("secondaryValue")]
    public string SecondaryValue { get; set; } = null!;
}

// Settings class where no properties have TrackChange
internal class UntrackedSettings
{
    [JsonPropertyName("settingId")]
    public int SettingId { get; set; }

    [JsonPropertyName("primaryValue")]
    public string PrimaryValue { get; set; } = null!;

    [JsonPropertyName("secondaryValue")]
    public string SecondaryValue { get; set; } = null!;
}
