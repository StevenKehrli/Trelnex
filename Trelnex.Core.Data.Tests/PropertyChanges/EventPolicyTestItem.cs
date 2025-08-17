using System.Text.Json.Serialization;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

public record EventPolicyTestItem : BaseItem
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [Track]
    [JsonPropertyName("trackMessage")]
    public string TrackMessage { get; set; } = null!;

    [DoNotTrack]
    [JsonPropertyName("doNotTrackMessage")]
    public string DoNotTrackMessage { get; set; } = null!;

    [Encrypt]
    [JsonPropertyName("encryptedMessage")]
    public string EncryptedMessage { get; set; } = null!;
}
