namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a security provider that manages authentication schemes.
/// </summary>
/// <remarks>
/// Acts as a registry for authentication configuration.
/// </remarks>
public interface ISecurityProvider
{
    /// <summary>
    /// Gets all registered security definitions that describe authentication schemes.
    /// </summary>
    /// <returns>A collection of security definitions for the application.</returns>
    IEnumerable<ISecurityDefinition> GetSecurityDefinitions();

    /// <summary>
    /// Gets a specific security requirement by its policy name.
    /// </summary>
    /// <param name="policy">The policy name to look up.</param>
    /// <returns>The security requirement associated with the specified policy.</returns>
    ISecurityRequirement GetSecurityRequirement(
        string policy);
}

/// <summary>
/// Implements the security provider that manages authentication schemes and requirements.
/// </summary>
/// <remarks>
/// Maintains collections of security definitions and requirements.
/// </remarks>
internal class SecurityProvider : ISecurityProvider
{
    #region Private Fields

    /// <summary>
    /// The collection of security definitions registered with this provider.
    /// </summary>
    private readonly List<ISecurityDefinition> _securityDefinitions = [];

    /// <summary>
    /// The dictionary of security requirements indexed by policy name.
    /// </summary>
    private readonly Dictionary<string, ISecurityRequirement> _securityRequirements = [];

    #endregion

    #region Public Methods

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

    #endregion

    #region Internal Methods

    /// <summary>
    /// Registers a security definition with this provider.
    /// </summary>
    /// <param name="securityDefinition">The security definition to register.</param>
    internal void AddSecurityDefinition(
        ISecurityDefinition securityDefinition)
    {
        _securityDefinitions.Add(securityDefinition);
    }

    /// <summary>
    /// Registers a security requirement with this provider.
    /// </summary>
    /// <param name="securityRequirement">The security requirement to register.</param>
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
    internal ISecurityRequirement[] GetSecurityRequirements(
        string jwtBearerScheme,
        string audience,
        string scope)
    {
        // Filter security requirements based on the provided authentication criteria, order them by policy name, and return them as an array.
        return _securityRequirements
            .Where(kvp =>
                kvp.Value.JwtBearerScheme == jwtBearerScheme &&
                kvp.Value.Audience == audience &&
                kvp.Value.Scope == scope)
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .ToArray();
    }

    #endregion
}
