using System.Security.Claims;
using Microsoft.Identity.Web;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Provides contextual information about the current user.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique object ID of the authenticated user.
    /// </summary>
    /// <value>The object ID, or null if the user is not authenticated.</value>
    string? ObjectId { get; }

    /// <summary>
    /// Checks if the current user has the specified permission.
    /// </summary>
    /// <typeparam name="T">The type of permission policy to check.</typeparam>
    /// <returns><c>true</c> if the user has the permission; otherwise, <c>false</c>.</returns>
    bool HasPermission<T>() where T : IPermissionPolicy;
}

/// <summary>
/// Represents contextual information about the current user, encapsulating their claims principal and authorized policies.
/// Implements the <see cref="IUserContext"/> interface.
/// </summary>
internal class UserContext(
    ClaimsPrincipal? user,
    string[] authorizedPolicies) : IUserContext
{
    #region Public Properties

    /// <inheritdoc />
    public string? ObjectId => user?.GetObjectId();

    #endregion

    #region Internal Properties

    /// <summary>
    /// Gets a value indicating whether the user is authorized based on the authorized policies.
    /// </summary>
    /// <value><c>true</c> if the user is authorized; otherwise, <c>false</c>.</value>
    internal bool IsAuthorized => authorizedPolicies.Length > 0;

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public bool HasPermission<T>() where T : IPermissionPolicy
    {
        return authorizedPolicies.Contains(PermissionPolicy.Name<T>());
    }

    #endregion
}
