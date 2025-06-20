using System.Text.Json.Serialization;
using FluentValidation;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.DataProviders;

public interface ITestItem : IBaseItem
{
    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }

    string? OptionalMessage { get; set; }
}

internal class TestItem : BaseItem, ITestItem
{
    [TrackChange]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [Encrypt]
    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;

    [Encrypt]
    [JsonPropertyName("optionalMessage")]
    public string? OptionalMessage { get; set; }

    public static IValidator<TestItem> Validator { get; } = new TestItemValidator();

    private class TestItemValidator : AbstractValidator<TestItem>
    {
        public TestItemValidator()
        {
        }
    }
}
