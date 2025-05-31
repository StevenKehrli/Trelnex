using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Trelnex.Auth.Amazon.Endpoints.Token;

/// <summary>
/// Represents the form data sent to the OAuth 2.0 token endpoint.
/// </summary>
/// <remarks>
/// This class maps to the standard OAuth 2.0 client credentials grant type form parameters.
/// It follows the RFC 6749 specification for the OAuth 2.0 Authorization Framework.
/// All properties are required and will be validated before processing the token request.
/// </remarks>
internal record GetTokenForm
{
    #region Private Fields

    /// <summary>
    /// The validator instance used to validate token requests.
    /// </summary>
    private static readonly AbstractValidator<GetTokenForm> _validator = new GetTokenFormValidator();

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the OAuth 2.0 grant type requested by the client.
    /// </summary>
    /// <remarks>
    /// Must be "client_credentials" as this implementation only supports the client credentials flow.
    /// </remarks>
    [FromForm(Name = "grant_type")]
    public required string GrantType { get; init; }

    /// <summary>
    /// Gets the client identifier issued to the client during the registration process.
    /// </summary>
    /// <remarks>
    /// Used to identify the client making the request. Must match a registered client in the system.
    /// </remarks>
    [FromForm(Name = "client_id")]
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the client secret issued to the client during the registration process.
    /// </summary>
    /// <remarks>
    /// Used to authenticate the client. Must match the secret associated with the client ID.
    /// </remarks>
    [FromForm(Name = "client_secret")]
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Gets the requested scope of access.
    /// </summary>
    /// <remarks>
    /// A space-delimited list of scope values indicating the required access permissions.
    /// The authorization server will validate these scopes against the client's allowed scopes.
    /// </remarks>
    [FromForm(Name = "scope")]
    public required string Scope { get; init; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Validates the token request form data.
    /// </summary>
    /// <returns>The validation result indicating whether the form data is valid.</returns>
    /// <remarks>
    /// Validates that all required fields are present and properly formatted according to
    /// OAuth 2.0 client credentials grant requirements.
    /// </remarks>
    public ValidationResult Validate()
    {
        // Validate the GetTokenForm instance using the GetTokenFormValidator.
        return _validator.Validate(this);
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Validator for the OAuth 2.0 token request form data.
    /// </summary>
    /// <remarks>
    /// Implements validation rules for the client credentials grant type according to RFC 6749.
    /// Enforces that all required parameters are present and correctly formatted.
    /// </remarks>
    private class GetTokenFormValidator : AbstractValidator<GetTokenForm>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetTokenFormValidator"/> class.
        /// </summary>
        public GetTokenFormValidator()
        {
            // Validate grant_type (must be "client_credentials").
            RuleFor(x => x.GrantType)
                .NotEmpty()
                .Equal("client_credentials")
                .OverridePropertyName("grant_type")
                .WithMessage("grant_type must be client_credentials.");

            // Validate client_id (required).
            RuleFor(x => x.ClientId)
                .NotEmpty()
                .OverridePropertyName("client_id")
                .WithMessage("client_id is required.");

            // Validate client_secret (required).
            RuleFor(x => x.ClientSecret)
                .NotEmpty()
                .OverridePropertyName("client_secret")
                .WithMessage("client_secret is required.");

            // Validate scope (required).
            RuleFor(x => x.Scope)
                .NotEmpty()
                .OverridePropertyName("scope")
                .WithMessage("scope is required.");
        }
    }

    #endregion
}
