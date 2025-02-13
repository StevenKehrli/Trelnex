using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// A class to identify the caller identity through an AWS sigv4 (region and headers).
/// </summary>
public class CallerIdentitySignature
{
    private static readonly AbstractValidator<CallerIdentitySignature> _validator = new CallerIdentitySignatureValidator();

    /// <summary>
    /// Decodes the <see cref="CallerIdentitySignature"/> that is json serialized and base64 encoded.
    /// </summary>
    /// <param name="clientSecret">The <see cref="CallerIdentitySignature"/> that is json serialized and base64 encoded.</param>
    /// <returns>The decoded <see cref="CallerIdentitySignature"/>.</returns>
    public static CallerIdentitySignature Decode(
        string clientSecret)
    {
        try
        {
            // basee64 decode to json
            var jsonBytes = Convert.FromBase64String(clientSecret);
            var json = Encoding.UTF8.GetString(jsonBytes);

            // deserialize to the context
            return JsonSerializer.Deserialize<CallerIdentitySignature>(json)!;
        }
        catch (Exception ex) when (ex is FormatException || ex is JsonException)
        {
            throw new HttpStatusCodeException(HttpStatusCode.BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Encodes the <see cref="CallerIdentitySignature"/> to a json serialized and base64 encoded string.
    /// </summary>
    /// <returns>The json serialized and base64 encoded string.</returns>
    public string Encode()
    {
        // serialize to json
        var json = JsonSerializer.Serialize(this);

        // base64 encode the json
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(jsonBytes);

        return base64;
    }

    [JsonPropertyName("region")]
    public string Region { get; init; } = null!;

    [JsonPropertyName("headers")]
    public IDictionary<string, string> Headers { get; init; } = null!;

    /// <summary>
    /// Validates the <see cref="CallerIdentitySignature"/>.
    /// </summary>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate()
    {
        return _validator.Validate(this);
    }

    private class CallerIdentitySignatureValidator : AbstractValidator<CallerIdentitySignature>
    {
        public CallerIdentitySignatureValidator()
        {
            RuleFor(x => x.Region)
                .NotEmpty()
                .OverridePropertyName("region")
                .WithMessage("region is required.");

            RuleFor(x => x.Region)
                .Must(region => RegionEndpoint.EnumerableAllRegions.FirstOrDefault(re => re.SystemName == region) != null)
                .OverridePropertyName("region")
                .WithMessage("region is invalid.");

            RuleFor(x => x.Headers)
                .NotEmpty()
                .OverridePropertyName("headers")
                .WithMessage("headers is required.");
        }
    }
}
