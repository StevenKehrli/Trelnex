using System.Text.Json.Serialization;

namespace Trelnex.Core.Data.Tests.Events;

internal interface IEncryptTestItem : IBaseItem
{
    int PublicId { get; set; }

    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }

    string EncryptMessage { get; set; }
}

internal class EncryptTestItem : BaseItem, IEncryptTestItem
{
    [TrackChange]
    [JsonPropertyName("publicId")]
    public int PublicId { get; set; }

    [TrackChange]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;

    [Encrypt]
    [TrackChange]
    [JsonPropertyName("encryptMessage")]
    public string EncryptMessage { get; set; } = null!;
}
