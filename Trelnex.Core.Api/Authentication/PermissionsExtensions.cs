using Microsoft.AspNetCore.Builder;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Provides extension methods for applying permission-based authorization to API endpoints.
/// </summary>
/// <remarks>
/// These extensions simplify the application of strongly-typed permission policies to
/// minimal API endpoints using a clean, fluent syntax. Permissions are tied to policy
/// names resolved at runtime from permission policy types.
/// </remarks>
public static class PermissionsExtensions
{
    /// <summary>
    /// Requires a specific permission policy for authorization on an API endpoint.
    /// </summary>
    /// <typeparam name="T">The permission policy type that defines the required access rights.</typeparam>
    /// <param name="rhb">The route handler builder being configured.</param>
    /// <returns>The route handler builder with the permission policy applied.</returns>
    /// <remarks>
    /// This method provides a strongly-typed way to apply authorization policies to minimal API endpoints.
    /// It automatically derives the correct policy name from the permission policy type and applies it
    /// to the endpoint.
    ///
    /// Usage example:
    /// <code>
    /// app.MapGet("/secure-data", () => "This data is protected")
    ///    .RequirePermission&lt;ReadDataPermission&gt;();
    /// </code>
    ///
    /// This approach enforces compile-time checking of permission policies and improves
    /// code maintainability by centralizing permission definitions.
    /// </remarks>
    public static RouteHandlerBuilder RequirePermission<T>(
        this RouteHandlerBuilder rhb) where T : IPermissionPolicy
    {
        return rhb.RequireAuthorization(PermissionPolicy.Name<T>());
    }
}
