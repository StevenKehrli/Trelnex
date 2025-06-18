using System.Text.Json.Serialization;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Tests.Encryption;

internal class TestItem
{
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [Encrypt]
    [JsonPropertyName("encryptMessage")]
    public string EncryptMessage { get; set; } = null!;

    [Encrypt]
    [JsonPropertyName("encryptInt")]
    public int EncryptInt { get; set; }

    [Encrypt]
    [JsonPropertyName("encryptGuid")]
    public Guid EncryptGuid { get; set; }

    [Encrypt]
    [JsonPropertyName("encryptDateTimeOffset")]
    public DateTimeOffset EncryptDateTimeOffset { get; set; }

    [JsonPropertyName("encryptNestedTestItem")]
    public NestedTestItem EncryptNestedTestItem { get; set; } = null!;

    public class NestedTestItem
    {
        [JsonPropertyName("nestedPublicMessage")]
        public string NestedPublicMessage { get; set; } = null!;

        [Encrypt]
        [JsonPropertyName("nestedEncryptMessage")]
        public string NestedEncryptMessage { get; set; } = null!;

        [Encrypt]
        [JsonPropertyName("nestedEncryptInt")]
        public int NestedEncryptInt { get; set; }

        [Encrypt]
        [JsonPropertyName("nestedEncryptGuid")]
        public Guid NestedEncryptGuid { get; set; }

        [Encrypt]
        [JsonPropertyName("nestedEncryptDateTimeOffset")]
        public DateTimeOffset NestedEncryptDateTimeOffset { get; set; }
    }
}
