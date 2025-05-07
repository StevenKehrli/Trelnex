namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a security definition that specifies authentication requirements.
/// </summary>
/// <remarks>
/// Describes the authentication scheme, audience, and scope required for API access.
/// </remarks>
public interface ISecurityDefinition
{
    /// <summary>
    /// Gets the JWT bearer token authentication scheme name.
    /// </summary>
    string JwtBearerScheme { get; }

    /// <summary>
    /// Gets the required audience value that must be present in valid JWT tokens.
    /// </summary>
    string Audience { get; }

    /// <summary>
    /// Gets the required scope value that must be present in valid JWT tokens.
    /// </summary>
    string Scope { get; }
}

/// <summary>
/// Implements a security definition for JWT bearer token authentication.
/// </summary>
/// <param name="jwtBearerScheme">The JWT bearer authentication scheme name.</param>
/// <param name="audience">The required audience claim value for valid tokens.</param>
/// <param name="scope">The required scope claim value for valid tokens.</param>
internal class SecurityDefinition(
    string jwtBearerScheme,
    string audience,
    string scope) : ISecurityDefinition
{
    #region Public Properties

    /// <inheritdoc/>
    public string JwtBearerScheme => jwtBearerScheme;

    /// <inheritdoc/>
    public string Audience => audience;

    /// <inheritdoc/>
    public string Scope => scope;

    #endregion
}
