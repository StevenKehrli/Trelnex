using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Trelnex.Auth.Amazon.Endpoints.Token;

internal record GetTokenForm
{
    private static readonly AbstractValidator<GetTokenForm> _validator = new GetTokenFormValidator();

    [FromForm(Name = "grant_type")]
    public required string GrantType { get; init; }

    [FromForm(Name = "client_id")]
    public required string ClientId { get; init; }

    [FromForm(Name = "client_secret")]
    public required string ClientSecret { get; init; }

    [FromForm(Name = "scope")]
    public required string Scope { get; init; }

    /// <summary>
    /// Validates the <see cref="GetTokenForm"/>.
    /// </summary>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate()
    {
        return _validator.Validate(this);
    }

    private class GetTokenFormValidator : AbstractValidator<GetTokenForm>
    {
        public GetTokenFormValidator()
        {
            RuleFor(x => x.GrantType)
                .NotEmpty()
                .Equal("client_credentials")
                .OverridePropertyName("grant_type")
                .WithMessage("grant_type must be client_credentials.");

            RuleFor(x => x.ClientId)
                .NotEmpty()
                .OverridePropertyName("client_id")
                .WithMessage("client_id is required.");

            RuleFor(x => x.ClientSecret)
                .NotEmpty()
                .OverridePropertyName("client_secret")
                .WithMessage("client_secret is required.");

            RuleFor(x => x.Scope)
                .NotEmpty()
                .OverridePropertyName("scope")
                .WithMessage("scope is required.");
        }
    }
}
