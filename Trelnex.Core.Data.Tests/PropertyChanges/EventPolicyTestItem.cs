using System.Text.Json.Serialization;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.PropertyChanges;

public record EventPolicyTestItem : BaseItem
{
    [Track]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [DoNotTrack]
    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;

    [Encrypt]
    [JsonPropertyName("optionalMessage")]
    public string? OptionalMessage { get; set; }
}
