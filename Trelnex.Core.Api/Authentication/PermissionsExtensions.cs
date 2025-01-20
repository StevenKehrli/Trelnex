using Microsoft.AspNetCore.Builder;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Extension method to require a permission policy to authorize a request to an endpoint.
/// </summary>
public static class PermissionsExtensions
{
    /// <summary>
    /// Require a permission policy to authorize a request to an endpoint.
    /// </summary>
    /// <param name="rhb">The <see cref="RouteHandlerBuilder"/> to add the permission policy to.</param>
    /// <typeparam name="T">The <see cref="IPermissionPolicy"/>.</typeparam>
    /// <returns>The <see cref="RouteHandlerBuilder"/>.</returns>
    public static RouteHandlerBuilder RequirePermission<T>(
        this RouteHandlerBuilder rhb) where T : IPermissionPolicy
    {
        return rhb.RequireAuthorization(PermissionPolicy.Name<T>());
    }
}
