namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for an authorization policy that enforces role-based security.
/// </summary>
/// <remarks>
/// Defines the role requirements needed to authorize a request.
/// </remarks>
public interface IPermissionPolicy
{
    /// <summary>
    /// Gets the array of roles required by this policy.
    /// </summary>
    /// <value>
    /// An array of role names that a user must have at least one of to be authorized.
    /// </value>
    string[] RequiredRoles { get; }
}

/// <summary>
/// Provides utility methods for working with permission policies.
/// </summary>
internal static class PermissionPolicy
{
    /// <summary>
    /// Gets the unique name for a permission policy type.
    /// </summary>
    /// <typeparam name="T">The permission policy type. This should be an <see cref="IPermissionPolicy"/> implementation.</typeparam>
    /// <returns>A unique string identifier for the policy, derived from its fully qualified type name.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the full name of the type cannot be determined, indicating a configuration issue.
    /// </exception>
    public static string Name<T>() where T : IPermissionPolicy
    {
        return typeof(T).FullName ?? throw new ArgumentException("Could not determine type name for permission policy.");
    }
}
