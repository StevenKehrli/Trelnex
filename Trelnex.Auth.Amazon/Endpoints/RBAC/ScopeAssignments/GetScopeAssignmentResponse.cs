using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a response containing information about all principals assigned to a specific scope for a resource.
/// </summary>
/// <remarks>
/// This response provides comprehensive information about which principals (users or services)
/// have been granted a specific scope for a particular resource in the RBAC system. This information
/// is useful for administrative purposes, auditing, and understanding the current authorization state.
/// It allows administrators to see all entities that have a certain scope boundary for a resource.
/// </remarks>
public record GetScopeAssignmentResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which scope assignments are being returned.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the scope assignments are being queried.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the scope for which assignments are being returned.
    /// </summary>
    /// <remarks>
    /// The scope name identifies authorization boundaries in the RBAC system.
    /// Common examples include "Global", "Department", or "Project".
    /// This response shows all principals that have been assigned this scope
    /// for the specified resource.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    /// <summary>
    /// Gets the array of principal identifiers that have been assigned the scope for the resource.
    /// </summary>
    /// <remarks>
    /// Each principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for an entity. These are the subjects that have
    /// been granted the specified scope for the specified resource.
    /// An empty array indicates that no principals have been assigned this scope.
    /// </remarks>
    [JsonPropertyName("principalIds")]
    [SwaggerSchema("The array of principal ids assigned to the scope.", Nullable = false)]
    public required string[] PrincipalIds { get; init; }

    #endregion
}
