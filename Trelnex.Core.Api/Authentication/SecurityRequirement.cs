namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a security requirement that specifies authorization conditions.
/// </summary>
/// <remarks>
/// A security requirement describes the specific authentication and authorization
/// requirements for a particular operation or endpoint. It includes information about:
/// <list type="bullet">
///   <item>The authentication scheme to use (JWT Bearer)</item>
///   <item>The required audience and scope claims</item>
///   <item>The specific roles required for access</item>
///   <item>The policy name that encapsulates these requirements</item>
/// </list>
///
/// These requirements are used by the <see cref="AuthorizeFilter"/> to generate
/// appropriate security documentation in the OpenAPI specification, allowing
/// API consumers to understand what credentials they need for each endpoint.
/// </remarks>
public interface ISecurityRequirement
{
    /// <summary>
    /// Gets the JWT bearer token scheme used for authentication.
    /// </summary>
    /// <remarks>
    /// This identifies which authentication handler should process the token.
    /// It corresponds to the scheme name configured in the application's authentication setup.
    /// </remarks>
    public string JwtBearerScheme { get; }

    /// <summary>
    /// Gets the required audience claim value that must be present in the JWT token.
    /// </summary>
    /// <remarks>
    /// The audience identifies the intended recipient of the token, ensuring
    /// that tokens issued for other services cannot be used with this API.
    /// </remarks>
    public string Audience { get; }

    /// <summary>
    /// Gets the required scope claim value that must be present in the JWT token.
    /// </summary>
    /// <remarks>
    /// The scope represents the general permission level needed to access the API.
    /// All tokens must include this scope, regardless of the specific operation being performed.
    /// </remarks>
    public string Scope { get; }

    /// <summary>
    /// Gets the authorization policy name associated with this security requirement.
    /// </summary>
    /// <remarks>
    /// The policy name is a unique identifier used to look up this security requirement
    /// and is also used to configure the ASP.NET Core authorization policy.
    /// </remarks>
    public string Policy { get; }

    /// <summary>
    /// Gets the array of role claims that must be present in the JWT token.
    /// </summary>
    /// <remarks>
    /// These roles represent the specific permissions needed for the operation.
    /// A user must have all the listed roles to access endpoints protected by this requirement.
    /// </remarks>
    public string[] RequiredRoles { get; }
}

/// <summary>
/// Implements a security requirement for JWT bearer token authentication.
/// </summary>
/// <remarks>
/// This class stores the complete set of authentication and authorization
/// requirements for a specific operation or endpoint, used both for actual
/// authorization enforcement and for OpenAPI documentation.
/// </remarks>
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
}
