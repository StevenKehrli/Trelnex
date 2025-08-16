using System.Text.Json.Serialization;
using FluentValidation;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data.Tests.DataProviders;

public record TestItem : BaseItem
{
    [Track]
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
