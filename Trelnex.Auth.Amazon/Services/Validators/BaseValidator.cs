namespace Trelnex.Auth.Amazon.Services.Validators;

/// <summary>
/// Base class for all validators used in the RBAC system, providing common validation patterns.
/// </summary>
/// <remarks>
/// This abstract class defines standard regular expression patterns used across multiple validators
/// in the Role-Based Access Control (RBAC) system. By centralizing these patterns, it ensures
/// consistent validation rules throughout the application and simplifies maintenance.
///
/// Validators derived from this class implement specific validation logic while inheriting
/// these common pattern definitions. The patterns are designed to enforce naming conventions
/// for resources, roles, and scopes that ensure compatibility with OAuth 2.0 and OpenID Connect
/// standards while maintaining readability and security.
/// </remarks>
internal abstract class BaseValidator
{
    #region Protected Constants

    /// <summary>
    /// The regular expression pattern for validating resource names.
    /// </summary>
    /// <remarks>
    /// Resource names in the RBAC system must conform to URI formats typically used in OAuth 2.0
    /// and OpenID Connect specifications. Valid formats include:
    ///
    /// - API identifiers: api://amazon.auth.trelnex.com
    /// - HTTP URLs: http://service.example.com
    /// - URN identifiers: urn://example.service.resource
    ///
    /// The pattern enforces that resource names:
    /// - Start with a valid scheme (api, http, or urn)
    /// - Include the "://" separator
    /// - Contain only lowercase alphanumeric characters, dots, hyphens, and forward slashes
    /// - End with at least one alphanumeric character
    ///
    /// This named capture group pattern extracts the resourceName portion for validation.
    /// </remarks>
    protected const string _regexResourceName = @"(?<resourceName>(api|http|urn):\/\/[a-z0-9\.\/-]*[a-z0-9]+)";

    /// <summary>
    /// The regular expression pattern for validating role names.
    /// </summary>
    /// <remarks>
    /// Role names in the RBAC system must follow a consistent pattern to ensure clarity and
    /// prevent security issues. Valid examples include:
    ///
    /// - service.read
    /// - admin.full-access
    /// - reporting.view-only
    ///
    /// The pattern enforces that role names:
    /// - Contain only lowercase alphanumeric characters, dots, and hyphens
    /// - Follow a recommended format of "{category}.{permission-level}"
    ///
    /// This named capture group pattern extracts the roleName portion for validation.
    /// </remarks>
    protected const string _regexRoleName = @"(?<roleName>[a-z0-9\.-]+)";

    /// <summary>
    /// The regular expression pattern for validating scope names.
    /// </summary>
    /// <remarks>
    /// Scope names in the RBAC system define authorization boundaries and must follow
    /// a consistent pattern. Valid examples include:
    ///
    /// - .default (the standard default scope)
    /// - production
    /// - us-west
    /// - finance.reports
    ///
    /// The pattern enforces that scope names:
    /// - Contain only lowercase alphanumeric characters, dots, and hyphens
    /// - Follow a recommended format reflecting their logical organization
    ///
    /// This named capture group pattern extracts the scopeName portion for validation.
    /// </remarks>
    protected const string _regexScopeName = @"(?<scopeName>[a-z0-9\.-]+)";

    #endregion
}
