namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a security requirement that specifies authorization conditions.
/// </summary>
/// <remarks>
/// Describes authentication and authorization requirements for an operation or endpoint.
/// </remarks>
public interface ISecurityRequirement
{
    /// <summary>
    /// Gets the JWT bearer token scheme used for authentication.
    /// </summary>
    string JwtBearerScheme { get; }

    /// <summary>
    /// Gets the required audience claim value that must be present in the JWT token.
    /// </summary>
    string Audience { get; }

    /// <summary>
    /// Gets the required scope claim value that must be present in the JWT token.
    /// </summary>
    string Scope { get; }

    /// <summary>
    /// Gets the authorization policy name associated with this security requirement.
    /// </summary>
    string Policy { get; }

    /// <summary>
    /// Gets the array of role claims that must be present in the JWT token.
    /// </summary>
    string[] RequiredRoles { get; }
}

/// <summary>
/// Implements a security requirement for JWT bearer token authentication.
/// </summary>
/// <param name="jwtBearerScheme">The JWT bearer authentication scheme name.</param>
/// <param name="audience">The required audience claim value.</param>
/// <param name="scope">The required scope claim value.</param>
/// <param name="policy">The policy name for this requirement.</param>
/// <param name="requiredRoles">The array of roles required for access.</param>
internal class SecurityRequirement(
    string jwtBearerScheme,
    string audience,
    string scope,
    string policy,
    string[] requiredRoles) : ISecurityRequirement
{
    #region Public Properties

    /// <inheritdoc/>
    public string JwtBearerScheme => jwtBearerScheme;

    /// <inheritdoc/>
    public string Audience => audience;

    /// <inheritdoc/>
    public string Scope => scope;

    /// <inheritdoc/>
    public string Policy => policy;

    /// <inheritdoc/>
    public string[] RequiredRoles => requiredRoles;

    #endregion
}
