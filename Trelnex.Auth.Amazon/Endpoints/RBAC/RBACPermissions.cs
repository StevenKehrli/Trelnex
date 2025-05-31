using Trelnex.Core.Api.Authentication;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Defines permission settings and authorization policies for the Role-Based Access Control (RBAC) endpoints.
/// </summary>
/// <remarks>
/// This class configures JWT bearer authentication specifically for RBAC operations,
/// defining the required roles for create, read, update, and delete operations.
/// It extends the base JwtBearerPermission class to provide RBAC-specific authorization.
/// </remarks>
internal class RBACPermission : JwtBearerPermission
{
    #region Protected Properties

    /// <summary>
    /// Gets the configuration section name for RBAC JWT bearer authentication settings.
    /// </summary>
    /// <remarks>
    /// This property specifies where in the application configuration the JWT settings
    /// for RBAC operations are stored.
    /// </remarks>
    protected override string ConfigSectionName => "Auth:trelnex-api-rbac";

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the authentication scheme name for RBAC JWT bearer authentication.
    /// </summary>
    /// <remarks>
    /// This scheme name is used to identify RBAC-specific JWT token validation
    /// and distinguish it from other JWT authentication mechanisms in the application.
    /// </remarks>
    public override string JwtBearerScheme => "Bearer.trelnex-api-rbac";

    #endregion

    #region Public Methods

    /// <summary>
    /// Configures authorization policies for RBAC operations.
    /// </summary>
    /// <param name="policiesBuilder">The builder for configuring authorization policies.</param>
    /// <remarks>
    /// Adds the four standard RBAC policies (Create, Read, Update, Delete) to the
    /// application's authorization system. Each policy requires a specific role.
    /// </remarks>
    public override void AddAuthorization(
        IPoliciesBuilder policiesBuilder)
    {
        // Add the four standard RBAC policies (Create, Read, Update, Delete) to the application's authorization system.
        policiesBuilder
            .AddPolicy<RBACCreatePolicy>()
            .AddPolicy<RBACReadPolicy>()
            .AddPolicy<RBACUpdatePolicy>()
            .AddPolicy<RBACDeletePolicy>();
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Defines the authorization policy for RBAC creation operations.
    /// </summary>
    /// <remarks>
    /// This policy requires the "rbac.create" role, which grants permission
    /// to create new resources, roles, and scopes in the RBAC system.
    /// </remarks>
    public class RBACCreatePolicy : IPermissionPolicy
    {
        /// <summary>
        /// Gets the roles required for RBAC creation operations.
        /// </summary>
        public string[] RequiredRoles => ["rbac.create"];
    }

    /// <summary>
    /// Defines the authorization policy for RBAC read operations.
    /// </summary>
    /// <remarks>
    /// This policy requires the "rbac.read" role, which grants permission
    /// to view resources, roles, scopes, and role assignments in the RBAC system.
    /// </remarks>
    public class RBACReadPolicy : IPermissionPolicy
    {
        /// <summary>
        /// Gets the roles required for RBAC read operations.
        /// </summary>
        public string[] RequiredRoles => ["rbac.read"];
    }

    /// <summary>
    /// Defines the authorization policy for RBAC update operations.
    /// </summary>
    /// <remarks>
    /// This policy requires the "rbac.update" role, which grants permission
    /// to modify existing resources, roles, and scopes in the RBAC system.
    /// This includes granting and revoking role memberships.
    /// </remarks>
    public class RBACUpdatePolicy : IPermissionPolicy
    {
        /// <summary>
        /// Gets the roles required for RBAC update operations.
        /// </summary>
        public string[] RequiredRoles => ["rbac.update"];
    }

    /// <summary>
    /// Defines the authorization policy for RBAC deletion operations.
    /// </summary>
    /// <remarks>
    /// This policy requires the "rbac.delete" role, which grants permission
    /// to delete resources, roles, scopes, and principals from the RBAC system.
    /// </remarks>
    public class RBACDeletePolicy : IPermissionPolicy
    {
        /// <summary>
        /// Gets the roles required for RBAC deletion operations.
        /// </summary>
        public string[] RequiredRoles => ["rbac.delete"];
    }

    #endregion
}
