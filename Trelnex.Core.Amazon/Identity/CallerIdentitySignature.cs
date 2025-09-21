using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using FluentValidation;
using FluentValidation.Results;
using Trelnex.Core.Exceptions;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Represents an AWS SigV4 signature for caller identity authentication.
/// </summary>
/// <remarks>
/// Encapsulates the region and headers needed to validate AWS caller identity via SigV4.
/// Can be serialized and base64-encoded for transmission.
/// </remarks>
public class CallerIdentitySignature
{
    #region Private Fields

    /// <summary>
    /// Validator for <see cref="CallerIdentitySignature"/>.
    /// </summary>
    private static readonly AbstractValidator<CallerIdentitySignature> _validator = new CallerIdentitySignatureValidator();

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets the AWS region for the signature.
    /// </summary>
    [JsonPropertyName("region")]
    public string Region { get; init; } = null!;

    /// <summary>
    /// Gets or sets the AWS SigV4 signature headers.
    /// </summary>
    [JsonPropertyName("headers")]
    public IDictionary<string, string> Headers { get; init; } = null!;

    #endregion

    #region Public Methods

    /// <summary>
    /// Decodes a base64-encoded, JSON-serialized <see cref="CallerIdentitySignature"/>.
    /// </summary>
    /// <param name="clientSecret">The base64-encoded JSON string to decode.</param>
    /// <returns>A deserialized <see cref="CallerIdentitySignature"/> object.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when the input cannot be decoded or deserialized.</exception>
    /// <remarks>
    /// Converts a base64-encoded JSON string back into a <see cref="CallerIdentitySignature"/> object.
    /// </remarks>
    public static CallerIdentitySignature Decode(
        string clientSecret)
    {
        try
        {
            // Decode the base64 string to JSON
            var jsonBytes = Convert.FromBase64String(clientSecret);
            var json = Encoding.UTF8.GetString(jsonBytes);

            // Deserialize from JSON to CallerIdentitySignature
            return JsonSerializer.Deserialize<CallerIdentitySignature>(json)!;
        }
        catch (Exception ex) when (ex is FormatException || ex is JsonException)
        {
            throw new HttpStatusCodeException(HttpStatusCode.BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Encodes this <see cref="CallerIdentitySignature"/> as a base64-encoded JSON string.
    /// </summary>
    /// <returns>A base64-encoded JSON representation of this signature.</returns>
    /// <remarks>
    /// Suitable for use as a client secret in OAuth2 flows.
    /// </remarks>
    public string Encode()
    {
        // Serialize to JSON
        var json = JsonSerializer.Serialize(this);

        // Base64 encode the JSON
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(jsonBytes);

        return base64;
    }

    /// <summary>
    /// Validates this <see cref="CallerIdentitySignature"/> instance.
    /// </summary>
    /// <returns>The validation result indicating success or failure.</returns>
    /// <remarks>
    /// Validates that the region is present and valid, and that headers are provided.
    /// </remarks>
    public ValidationResult Validate()
    {
        return _validator.Validate(this);
    }

    #endregion

    #region CallerIdentitySignatureValidator

    /// <summary>
    /// Validator for <see cref="CallerIdentitySignature"/> instances.
    /// </summary>
    /// <remarks>
    /// Validates the region and headers.
    /// </remarks>
    private class CallerIdentitySignatureValidator : AbstractValidator<CallerIdentitySignature>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallerIdentitySignatureValidator"/> class.
        /// </summary>
        public CallerIdentitySignatureValidator()
        {
            // Validate that the region is not empty
            RuleFor(x => x.Region)
                .NotEmpty()
                .OverridePropertyName("region")
                .WithMessage("region is required.");

            // Validate that the region is a valid AWS region
            RuleFor(x => x.Region)
                .Must(region => RegionEndpoint.EnumerableAllRegions.FirstOrDefault(re => re.SystemName == region) != null)
                .OverridePropertyName("region")
                .WithMessage("region is invalid.");

            // Validate that headers are provided
            RuleFor(x => x.Headers)
                .NotEmpty()
                .OverridePropertyName("headers")
                .WithMessage("headers is required.");
        }
    }

    #endregion
}
