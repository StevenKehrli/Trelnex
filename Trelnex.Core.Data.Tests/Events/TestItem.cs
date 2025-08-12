using System.Text.Json.Serialization;

namespace Trelnex.Core.Data.Tests.Events;

internal interface ITestItem : IBaseItem
{
    int PublicId { get; set; }

    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }
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
}
