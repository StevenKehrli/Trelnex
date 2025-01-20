using System.Text.Json.Serialization;

namespace Trelnex.Core.Data.Tests.TypeNameRules;

internal interface ITestItem : IBaseItem
{
    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }
}

internal class TestItem : BaseItem, ITestItem
{
    [TrackChange]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;
}
