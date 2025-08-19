using System.Text.Json.Serialization;

namespace Trelnex.Core.Data.Tests.TypeNameRules;

internal record TestItem : BaseItem
{
    [Track]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;
}
