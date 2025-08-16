using System.Text.Json.Serialization;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.Events;

internal record TestItem : BaseItem
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
    [Encrypt]
    [JsonPropertyName("encryptedMessage")]
    public string EncryptedMessage { get; set; } = null!;
}
