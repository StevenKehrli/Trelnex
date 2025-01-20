namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a permission policy.
/// </summary>
/// <remarks>
/// A permission policy defines the required roles to authorize a request to an endpoint.
/// </remarks>
/// <remarks>
/// A permission policy is added to the the endpoint by RequirePermission&lt;IPermissionPolicy&gt;()
/// </remarks>
public interface IPermissionPolicy
{
    /// <summary>
    /// The array of roles required by this policy.
    /// </summary>
    public string[] RequiredRoles { get; }
}

/// <summary>
/// Extension method to get the name of a permission policy.
/// </summary>
internal static class PermissionPolicy
{
    /// <summary>
    /// Gets the name of the specified permission policy.
    /// </summary>
    /// <typeparam name="T">The <see cref="IPermissionPolicy"/>.</typeparam>
    /// <returns>The name of the specified permission policy.</returns>
    /// <exception cref="ArgumentException">The exception that is thrown when one the name of the specified permission policy is not found.</exception>
    public static string Name<T>() where T : IPermissionPolicy
    {
        return typeof(T).FullName ?? throw new ArgumentException();
    }
}
