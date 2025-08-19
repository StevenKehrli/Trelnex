using System.Text.Json.Serialization;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

internal record PolicyChangesTestItem : BaseItem
{
    [Track]
    [JsonPropertyName("publicId")]
    public int PublicId { get; set; }

    [Track]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;

    [Track]
    [JsonPropertyName("trackedSettingsWithAttribute")]
    public TrackedSettings TrackedSettingsWithAttribute { get; set; } = null!;

    [Track]
    [JsonPropertyName("untrackedSettingsWithAttribute")]
    public UntrackedSettings UntrackedSettingsWithAttribute { get; set; } = null!;

    // Note: No Track attribute here
    [JsonPropertyName("trackedSettingsWithoutAttribute")]
    public TrackedSettings TrackedSettingsWithoutAttribute { get; set; } = null!;

    [Track]
    [JsonPropertyName("trackedSettingsArray")]
    public TrackedSettings[] TrackedSettingsArray { get; set; } = null!;

    [Track]
    [JsonPropertyName("trackedSettingsDictionary")]
    public Dictionary<string, TrackedSettings> TrackedSettingsDictionary { get; set; } = null!;
}

// Settings class where all properties have Track
internal class TrackedSettings
{
    [Track]
    [JsonPropertyName("settingId")]
    public int SettingId { get; set; }

    [Track]
    [JsonPropertyName("primaryValue")]
    public string PrimaryValue { get; set; } = null!;

    [Track]
    [JsonPropertyName("secondaryValue")]
    public string SecondaryValue { get; set; } = null!;
}

// Settings class where no properties have Track
internal class UntrackedSettings
{
    [JsonPropertyName("settingId")]
    public int SettingId { get; set; }

    [JsonPropertyName("primaryValue")]
    public string PrimaryValue { get; set; } = null!;

    [JsonPropertyName("secondaryValue")]
    public string SecondaryValue { get; set; } = null!;
}
