using Microsoft.AspNetCore.Builder;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Provides extension methods for applying permission-based authorization to API endpoints.
/// </summary>
/// <remarks>
/// Simplifies applying permission policies to minimal API endpoints.
/// </remarks>
public static class PermissionsExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Requires a specific permission policy for authorization on an API endpoint.
    /// </summary>
    /// <typeparam name="T">The permission policy type that defines the required access rights.</typeparam>
    /// <param name="routeHandlerBuilder">The route handler builder being configured.</param>
    /// <returns>The route handler builder with the permission policy applied.</returns>
    public static RouteHandlerBuilder RequirePermission<T>(
        this RouteHandlerBuilder routeHandlerBuilder) where T : IPermissionPolicy
    {
        // Applies the authorization requirement based on the specified permission policy.
        return routeHandlerBuilder.RequireAuthorization(PermissionPolicy.Name<T>());
    }

    #endregion
}
