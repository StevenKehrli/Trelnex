using System.Text.Json.Serialization;
using FluentValidation;

namespace Trelnex.Core.Data.Tests.CommandProviders;

public interface ITestItem : IBaseItem
{
    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }
}

internal class TestItem : BaseItem, ITestItem
{
    [TrackChange]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [Encrypt]
    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;

    public static IValidator<TestItem> Validator { get; } = new TestItemValidator();

    private class TestItemValidator : AbstractValidator<TestItem>
    {
        public TestItemValidator()
        {
        }
    }
}
