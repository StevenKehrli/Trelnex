namespace Trelnex.Auth.Amazon.Services.RBAC;

/// <summary>
/// Provides standardized formatting methods and constants for RBAC entity names and identifiers.
/// </summary>
/// <remarks>
/// This class centralizes the formatting logic for DynamoDB keys and subject names used in the RBAC system.
/// It ensures consistent naming patterns across all RBAC entities including principals, resources, roles, scopes,
/// and their assignments.
/// </remarks>
internal static class ItemName
{
    /// <summary>
    /// The marker prefix used to identify principal-related entries.
    /// </summary>
    public const string PRINCIPAL_MARKER = "PRINCIPAL#";

    /// <summary>
    /// The marker prefix used to identify resource-related entries.
    /// </summary>
    public const string RESOURCE_MARKER = "RESOURCE#";

    /// <summary>
    /// The marker prefix used to identify role assignment entries.
    /// </summary>
    public const string ROLEASSIGNMENT_MARKER = "ROLEASSIGNMENT##";

    /// <summary>
    /// The marker prefix used to identify role-related entries.
    /// </summary>
    public const string ROLE_MARKER = "ROLE#";

    /// <summary>
    /// The marker prefix used to identify root-level entries.
    /// </summary>
    public const string ROOT_MARKER = "ROOT#";

    /// <summary>
    /// The marker prefix used to identify scope assignment entries.
    /// </summary>
    public const string SCOPEASSIGNMENT_MARKER = "SCOPEASSIGNMENT##";

    /// <summary>
    /// The marker prefix used to identify scope-related entries.
    /// </summary>
    public const string SCOPE_MARKER = "SCOPE#";

    /// <summary>
    /// Formats a principal identifier for use as a DynamoDB entity name.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal, such as "arn:aws:iam::123456789012:user/john".</param>
    /// <returns>A formatted entity name with the principal marker prefix.</returns>
    /// <example>
    /// Returns: "PRINCIPAL#arn:aws:iam::123456789012:user/john"
    /// </example>
    public static string FormatPrincipal(
        string principalId)
    {
        return $"{PRINCIPAL_MARKER}{principalId}";
    }

    /// <summary>
    /// Formats a resource name for use as a DynamoDB entity name.
    /// </summary>
    /// <param name="resourceName">The name of the resource, such as "api://amazon.auth.trelnex.com".</param>
    /// <returns>A formatted entity name with the resource marker prefix.</returns>
    /// <example>
    /// Returns: "RESOURCE#api://amazon.auth.trelnex.com"
    /// </example>
    public static string FormatResource(
        string resourceName)
    {
        return $"{RESOURCE_MARKER}{resourceName}";
    }

    /// <summary>
    /// Formats a role assignment name optimized for principal-based queries.
    /// </summary>
    /// <param name="roleName">The name of the role, such as "rbac.create".</param>
    /// <param name="principalId">The unique identifier of the principal (optional for query prefixes).</param>
    /// <returns>A formatted subject name for DynamoDB queries by principal.</returns>
    /// <example>
    /// Returns: "ROLEASSIGNMENT##ROLE#rbac.create##PRINCIPAL#arn:aws:iam::123456789012:user/john"
    /// </example>
    public static string FormatRoleAssignmentByPrincipal(
        string roleName,
        string? principalId = null)
    {
        var rolePart = FormatRole(roleName);
        var principalPart = FormatPrincipal(principalId ?? string.Empty);
        return $"{ROLEASSIGNMENT_MARKER}{rolePart}##{principalPart}";
    }

    /// <summary>
    /// Formats a role assignment name optimized for role-based queries.
    /// </summary>
    /// <param name="resourceName">The name of the resource, such as "api://amazon.auth.trelnex.com".</param>
    /// <param name="roleName">The name of the role (optional for query prefixes).</param>
    /// <returns>A formatted subject name for DynamoDB queries by role.</returns>
    /// <example>
    /// Returns: "ROLEASSIGNMENT##RESOURCE#api://amazon.auth.trelnex.com##ROLE#rbac.create"
    /// </example>
    public static string FormatRoleAssignmentByRole(
        string resourceName,
        string? roleName = null)
    {
        var resourcePart = FormatResource(resourceName);
        var rolePart = FormatRole(roleName ?? string.Empty);
        return $"{ROLEASSIGNMENT_MARKER}{resourcePart}##{rolePart}";
    }

    /// <summary>
    /// Formats a role name for use as a DynamoDB entity name.
    /// </summary>
    /// <param name="roleName">The name of the role, such as "rbac.create" or "rbac.read".</param>
    /// <returns>A formatted entity name with the role marker prefix.</returns>
    /// <example>
    /// Returns: "ROLE#rbac.create"
    /// </example>
    public static string FormatRole(
        string roleName)
    {
        return $"{ROLE_MARKER}{roleName}";
    }

    /// <summary>
    /// Formats a scope assignment name optimized for principal-based queries.
    /// </summary>
    /// <param name="scopeName">The name of the scope, such as "rbac".</param>
    /// <param name="principalId">The unique identifier of the principal (optional for query prefixes).</param>
    /// <returns>A formatted subject name for DynamoDB queries by principal.</returns>
    /// <example>
    /// Returns: "SCOPEASSIGNMENT##SCOPE#rbac##PRINCIPAL#arn:aws:iam::123456789012:user/john"
    /// </example>
    public static string FormatScopeAssignmentByPrincipal(
        string scopeName,
        string? principalId = null)
    {
        var scopePart = FormatScope(scopeName);
        var principalPart = FormatPrincipal(principalId ?? string.Empty);
        return $"{SCOPEASSIGNMENT_MARKER}{scopePart}##{principalPart}";
    }

    /// <summary>
    /// Formats a scope assignment name optimized for scope-based queries.
    /// </summary>
    /// <param name="resourceName">The name of the resource, such as "api://amazon.auth.trelnex.com".</param>
    /// <param name="scopeName">The name of the scope (optional for query prefixes).</param>
    /// <returns>A formatted subject name for DynamoDB queries by scope.</returns>
    /// <example>
    /// Returns: "SCOPEASSIGNMENT##RESOURCE#api://amazon.auth.trelnex.com##SCOPE#rbac"
    /// </example>
    public static string FormatScopeAssignmentByScope(
        string resourceName,
        string? scopeName = null)
    {
        var resourcePart = FormatResource(resourceName);
        var scopePart = FormatScope(scopeName ?? string.Empty);
        return $"{SCOPEASSIGNMENT_MARKER}{resourcePart}##{scopePart}";
    }

    /// <summary>
    /// Formats a scope name for use as a DynamoDB entity name.
    /// </summary>
    /// <param name="scopeName">The name of the scope, such as "rbac".</param>
    /// <returns>A formatted entity name with the scope marker prefix.</returns>
    /// <example>
    /// Returns: "SCOPE#rbac"
    /// </example>
    public static string FormatScope(
        string scopeName)
    {
        return $"{SCOPE_MARKER}{scopeName}";
    }
}
