namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a security definition.
/// </summary>
/// <remark>
/// The security definition is used by the Swagger <see cref="SecurityFilter"/>
/// to add the <see cref="OpenApiSecurityScheme"/> to the Swagger documentation.
/// </remark>
public interface ISecurityDefinition
{
    /// <summary>
    /// The JWT bearer token scheme.
    /// </summary>
    public string JwtBearerScheme { get; }

    /// <summary>
    /// The required audience of the JWT bearer token.
    /// </summary>
    public string Audience { get; }

    /// <summary>
    /// The required scope of the JWT bearer token.
    /// </summary>
    public string Scope { get; }
}

/// <summary>
/// Initializes a new instance of the <see cref="SecurityDefinition"/>.
/// </summary>
/// <param name="jwtBearerScheme">Specifies the JWT bearer token scheme.</param>
/// <param name="audience">Specifies the required audience of the JWT bearer token.</param>
/// <param name="scope">Specifies the required audience of the JWT bearer token.</param>
internal class SecurityDefinition(
    string jwtBearerScheme,
    string audience,
    string scope) : ISecurityDefinition
{
    public string JwtBearerScheme => jwtBearerScheme;
    public string Audience => audience;
    public string Scope => scope;
}
