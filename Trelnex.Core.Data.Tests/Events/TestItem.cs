using System.Text.Json.Serialization;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.Events;

internal interface ITestItem : IBaseItem
{
    int PublicId { get; set; }

    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }

    string EncryptedMessage { get; set; }
}

internal record TestItem : BaseItem, ITestItem
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
