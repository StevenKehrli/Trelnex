namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a security provider that manages authentication schemes and requirements.
/// </summary>
/// <remarks>
/// The security provider acts as a registry for authentication configuration, serving two main purposes:
/// <list type="number">
///   <item>
///     Providing security definitions to the <see cref="SecurityFilter"/> for Swagger documentation,
///     documenting the available authentication schemes
///   </item>
///   <item>
///     Providing security requirements to the <see cref="AuthorizeFilter"/> for Swagger operations,
///     documenting the specific authentication requirements for each endpoint
///   </item>
/// </list>
///
/// This centralized approach ensures consistency between actual authentication
/// requirements and their documentation in the API specification.
/// </remarks>
public interface ISecurityProvider
{
    /// <summary>
    /// Gets all registered security definitions that describe authentication schemes.
    /// </summary>
    /// <returns>A collection of security definitions for the application.</returns>
    /// <remarks>
    /// Security definitions describe the available authentication schemes (like JWT Bearer)
    /// along with their audience and scope requirements. The Swagger UI uses these
    /// definitions to provide authentication options in its interface.
    /// </remarks>
    public IEnumerable<ISecurityDefinition> GetSecurityDefinitions();

    /// <summary>
    /// Gets a specific security requirement by its policy name.
    /// </summary>
    /// <param name="policy">The policy name to look up.</param>
    /// <returns>The security requirement associated with the specified policy.</returns>
    /// <remarks>
    /// Security requirements define the specific authentication and authorization
    /// conditions needed for a particular endpoint or operation. They include:
    /// <list type="bullet">
    ///   <item>The authentication scheme to use</item>
    ///   <item>Required audience and scope values</item>
    ///   <item>Specific roles that must be present in the token</item>
    /// </list>
    ///
    /// The <see cref="AuthorizeFilter"/> uses these requirements to annotate
    /// API operations in the Swagger documentation with their security needs.
    /// </remarks>
    public ISecurityRequirement GetSecurityRequirement(
        string policy);
}

/// <summary>
/// Implements the security provider that manages authentication schemes and requirements.
/// </summary>
/// <remarks>
/// This class maintains collections of security definitions and requirements,
/// providing a centralized registry for authentication configuration.
/// </remarks>
internal class SecurityProvider : ISecurityProvider
{
    /// <summary>
    /// The collection of security definitions registered with this provider.
    /// </summary>
    private readonly List<ISecurityDefinition> _securityDefinitions = [];

    /// <summary>
    /// The dictionary of security requirements indexed by policy name.
    /// </summary>
    private readonly Dictionary<string, ISecurityRequirement> _securityRequirements = [];

    /// <inheritdoc/>
    public IEnumerable<ISecurityDefinition> GetSecurityDefinitions()
    {
        return _securityDefinitions.AsEnumerable();
    }

    /// <inheritdoc/>
    public ISecurityRequirement GetSecurityRequirement(
        string policy)
    {
        return _securityRequirements[policy];
    }

    /// <summary>
    /// Registers a security definition with this provider.
    /// </summary>
    /// <param name="securityDefinition">The security definition to register.</param>
    /// <remarks>
    /// Security definitions are added during application startup when configuring
    /// authentication schemes like JWT Bearer or Microsoft Identity.
    /// </remarks>
    internal void AddSecurityDefinition(
        ISecurityDefinition securityDefinition)
    {
        _securityDefinitions.Add(securityDefinition);
    }

    /// <summary>
    /// Registers a security requirement with this provider.
    /// </summary>
    /// <param name="securityRequirement">The security requirement to register.</param>
    /// <remarks>
    /// Security requirements are typically added when registering permission policies
    /// through the <see cref="PoliciesBuilder"/>.
    /// </remarks>
    internal void AddSecurityRequirement(
        ISecurityRequirement securityRequirement)
    {
        _securityRequirements.Add(securityRequirement.Policy, securityRequirement);
    }

    /// <summary>
    /// Retrieves security requirements matching specific authentication criteria.
    /// </summary>
    /// <param name="jwtBearerScheme">The JWT bearer scheme to match.</param>
    /// <param name="audience">The audience to match.</param>
    /// <param name="scope">The scope to match.</param>
    /// <returns>An array of matching security requirements.</returns>
    /// <remarks>
    /// This method filters security requirements by their authentication parameters,
    /// returning only those that match the specified scheme, audience, and scope.
    /// It's used internally for advanced filtering of security requirements.
    /// </remarks>
    internal ISecurityRequirement[] GetSecurityRequirements(
        string jwtBearerScheme,
        string audience,
        string scope)
    {
        return _securityRequirements
            .Where(kvp => kvp.Value.JwtBearerScheme == jwtBearerScheme &&
                         kvp.Value.Audience == audience &&
                         kvp.Value.Scope == scope)
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .ToArray();
    }
}
