namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for an authorization policy that enforces role-based security.
/// </summary>
/// <remarks>
/// A permission policy defines the role requirements needed to authorize a request to an API endpoint.
/// Each policy encapsulates a set of roles that a user must possess to access protected resources.
///
/// Permission policies are applied to endpoints using the <c>RequirePermission&lt;T&gt;()</c> extension
/// method, where T is an implementation of <see cref="IPermissionPolicy"/>.
///
/// Example usage:
/// <code>
/// app.MapGet("/secure-endpoint", () => "Secure content")
///    .RequirePermission&lt;AdminOnlyPolicy&gt;();
/// </code>
/// </remarks>
public interface IPermissionPolicy
{
    /// <summary>
    /// Gets the array of roles required by this policy.
    /// </summary>
    /// <value>
    /// An array of role names that a user must have at least one of to be authorized.
    /// </value>
    /// <remarks>
    /// When applying authorization, the user must possess at least one of these roles
    /// to be granted access to the protected resource. An empty array would effectively
    /// deny access to all users.
    /// </remarks>
    public string[] RequiredRoles { get; }
}

/// <summary>
/// Provides utility methods for working with permission policies.
/// </summary>
/// <remarks>
/// This class contains internal helper methods for handling permission policies
/// and generating unique policy names for policy registration.
/// </remarks>
internal static class PermissionPolicy
{
    /// <summary>
    /// Gets the unique name for a permission policy type.
    /// </summary>
    /// <typeparam name="T">The permission policy type.</typeparam>
    /// <returns>A unique string identifier for the policy derived from its type name.</returns>
    /// <remarks>
    /// Uses the full type name as a unique identifier for policy registration in the
    /// authorization system. This ensures that each policy type has a distinct name.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the full name of the type cannot be determined.
    /// </exception>
    public static string Name<T>() where T : IPermissionPolicy
    {
        return typeof(T).FullName ?? throw new ArgumentException("Could not determine type name for permission policy.");
    }
}
