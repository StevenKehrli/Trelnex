using System.Text.Json.Serialization;

namespace Trelnex.Core.Data.Tests.Validation;

internal interface ITestItem
{
    int Id { get; set; }

    string Message { get; set; }
}

internal class TestItem : ITestItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
}
