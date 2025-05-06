namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a security definition that specifies authentication requirements.
/// </summary>
/// <remarks>
/// A security definition describes the authentication scheme, audience, and scope
/// required for API access. It is used by:
/// <list type="bullet">
///   <item>The Swagger <see cref="SecurityFilter"/> to document authentication requirements</item>
///   <item>Authorization policy configuration to enforce token validation rules</item>
///   <item>Security requirement definitions to specify role-based permissions</item>
/// </list>
///
/// Security definitions provide a consistent way to configure both the API's actual
/// authentication requirements and their documentation in the OpenAPI specification.
/// </remarks>
public interface ISecurityDefinition
{
    /// <summary>
    /// Gets the JWT bearer token authentication scheme name.
    /// </summary>
    /// <remarks>
    /// This identifies the authentication handler that will process incoming tokens.
    /// Common values include "Bearer" for standard JWT authentication or custom
    /// scheme names for specific identity providers.
    /// </remarks>
    public string JwtBearerScheme { get; }

    /// <summary>
    /// Gets the required audience value that must be present in valid JWT tokens.
    /// </summary>
    /// <remarks>
    /// The audience claim identifies the intended recipient of the token.
    /// It is validated during token authentication to ensure the token
    /// was issued specifically for this API.
    /// </remarks>
    public string Audience { get; }

    /// <summary>
    /// Gets the required scope value that must be present in valid JWT tokens.
    /// </summary>
    /// <remarks>
    /// The scope identifies the set of permissions that the token grants.
    /// All requests must include a token with this scope to access protected endpoints,
    /// regardless of their specific role requirements.
    /// </remarks>
    public string Scope { get; }
}

/// <summary>
/// Implements a security definition for JWT bearer token authentication.
/// </summary>
/// <remarks>
/// This class provides the concrete implementation of security definition properties
/// used throughout the authentication and authorization system.
/// </remarks>
/// <param name="jwtBearerScheme">The JWT bearer authentication scheme name.</param>
/// <param name="audience">The required audience claim value for valid tokens.</param>
/// <param name="scope">The required scope claim value for valid tokens.</param>
internal class SecurityDefinition(
    string jwtBearerScheme,
    string audience,
    string scope) : ISecurityDefinition
{
    /// <inheritdoc/>
    public string JwtBearerScheme => jwtBearerScheme;

    /// <inheritdoc/>
    public string Audience => audience;

    /// <inheritdoc/>
    public string Scope => scope;
}
