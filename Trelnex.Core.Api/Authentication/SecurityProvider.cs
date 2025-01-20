namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a security provider.
/// </summary>
/// <remarks>
/// The security provider is used by the Swagger <see cref="SecurityFilter"/>
/// to get the collection of <see cref="ISecurityDefinition"/> to add to the Swagger documentation.
/// </remarks>
/// <remarks>
/// The security provider is used by the Swagger <see cref="AuthorizeFilter"/>
/// to get the collection of <see cref="ISecurityRequirement"/> to add to the Swagger operation (endpoint) documentation.
/// </remarks>
public interface ISecurityProvider
{
    /// <summary>
    /// Gets the collection of <see cref="ISecurityDefinition"/> for this provider.
    /// </summary>
    /// <remarks>
    /// The security definitions are used by the Swagger <see cref="SecurityFilter"/>
    /// to add the <see cref="OpenApiSecurityScheme"/> to the Swagger documentation.
    /// </remarks>
    /// <returns>The collection of <see cref="ISecurityDefinition"/>.</returns>
    public IEnumerable<ISecurityDefinition> GetSecurityDefinitions();

    /// <summary>
    /// Gets the <see cref="ISecurityRequirement"/> for the specified policy name.
    /// </summary>
    /// <remarks>
    /// The security requirement is used by the Swagger <see cref="AuthorizeFilter"/>
    /// to add the <see cref="OpenApiSecurityRequirement"/> to the Swagger operation (endpoint) documentation.
    /// </remarks>
    /// <param name="policy">The specified policy name.</param>
    /// <returns>The <see cref="ISecurityRequirement"/> for the specified policy name.</returns>
    public ISecurityRequirement GetSecurityRequirement(
        string policy);
}

internal class SecurityProvider : ISecurityProvider
{
    /// <summary>
    /// The collection of <see cref="ISecurityDefinition"/> for this provider.
    /// </summary>
    private readonly List<ISecurityDefinition> _securityDefinitions = [];

    /// <summary>
    /// The collection of <see cref="ISecurityRequirement"/> by policy name.
    /// </summary>
    private readonly Dictionary<string, ISecurityRequirement> _securityRequirements = [];

    /// <summary>
    /// Gets the collection of <see cref="ISecurityDefinition"/> for this provider.
    /// </summary>
    /// <remarks>
    /// The security definitions are used by the Swagger <see cref="SecurityFilter"/>
    /// to add the <see cref="OpenApiSecurityScheme"/> to the Swagger documentation.
    /// </remarks>
    /// <returns>The collection of <see cref="ISecurityDefinition"/>.</returns>
    public IEnumerable<ISecurityDefinition> GetSecurityDefinitions()
    {
        return _securityDefinitions.AsEnumerable();
    }

    /// <summary>
    /// Gets the <see cref="ISecurityRequirement"/> for the specified policy name.
    /// </summary>
    /// <remarks>
    /// The security requirement is used by the Swagger <see cref="AuthorizeFilter"/>
    /// to add the <see cref="OpenApiSecurityRequirement"/> to the Swagger operation (endpoint) documentation.
    /// </remarks>
    /// <param name="policy">The specified policy name.</param>
    /// <returns>The <see cref="ISecurityRequirement"/> for the specified policy name.</returns>
    public ISecurityRequirement GetSecurityRequirement(
        string policy)
    {
        return _securityRequirements[policy];
    }

    /// <summary>
    /// Add the specified <see cref="ISecurityDefinition"/> to this provider.
    /// </summary>
    /// <param name="securityDefinition">The specified <see cref="ISecurityDefinition"/>.</param>
    internal void AddSecurityDefinition(
        ISecurityDefinition securityDefinition)
    {
        _securityDefinitions.Add(securityDefinition);
    }

    /// <summary>
    /// Add the specified <see cref="ISecurityRequirement"/> to this provider.
    /// </summary>
    /// <param name="securityRequirement">The specified <see cref="ISecurityRequirement"/>.</param>
    internal void AddSecurityRequirement(
        ISecurityRequirement securityRequirement)
    {
        _securityRequirements.Add(securityRequirement.Policy, securityRequirement);
    }

    /// <summary>
    /// Gets the collection of <see cref="ISecurityRequirement"/> for the specified permission.
    /// </summary>
    /// <param name="jwtBearerScheme">The JWT bearer token scheme.</param>
    /// <param name="audience">The required audience of the JWT bearer token.</param>
    /// <param name="scope">The required scope of the JWT bearer token.</param>
    /// <returns>the collection of <see cref="ISecurityRequirement"/>.</returns>
    internal ISecurityRequirement[] GetSecurityRequirements(
        string jwtBearerScheme,
        string audience,
        string scope)
    {
        return _securityRequirements
            .Where(kvp => kvp.Value.JwtBearerScheme == jwtBearerScheme && kvp.Value.Audience == audience && kvp.Value.Scope == scope)
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .ToArray();
    }
}
