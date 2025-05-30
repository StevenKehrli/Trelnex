using System.Net;
using Trelnex.Auth.Amazon.Services.RBAC.Models;
using Trelnex.Core;
using Trelnex.Core.Observability;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal partial class RBACRepository
{
    /// <inheritdoc/>
    [TraceMethod]
    public async Task DeletePrincipalAsync(
        string principalId,
        CancellationToken cancellationToken = default)
    {
        // Start the delete scope assignments operation.
        var deleteScopeAssignmentsTask = DeleteScopeAssignmentsByPrincipalAsync(
            principalId: principalId,
            cancellationToken: cancellationToken);

        // Start the delete role assignments operation.
        var deleteRoleAssignmentsTask = DeleteRoleAssignmentsByPrincipalAsync(
            principalId: principalId,
            cancellationToken: cancellationToken);

        // Wait for all operations to complete.
        await Task.WhenAll(deleteScopeAssignmentsTask, deleteRoleAssignmentsTask);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task<PrincipalAccess> GetPrincipalAccessAsync(
        string principalId,
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Verify the resource exists before retrieving principal access.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName}' not found.");

        // Start both assignment retrieval operations concurrently.
        var scopeAssignmentsTask = GetScopesForPrincipalAsync(
            principalId: principalId,
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        var roleAssignmentsTask = GetRolesForPrincipalAsync(
            principalId: principalId,
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        // Wait for both operations to complete.
        await Task.WhenAll(scopeAssignmentsTask, roleAssignmentsTask);

        // Get the scope assignments for the principal.
        var scopeNames = scopeAssignmentsTask.Result;

        // Only include role assignments if the principal has scope assignments.
        var roleNames = (scopeNames.Length > 0)
            ? roleAssignmentsTask.Result
            : [];

        // Return the combined principal access information.
        return new PrincipalAccess
        {
            PrincipalId = principalId,
            ResourceName = normalizedResourceName!,
            ScopeNames = scopeNames,
            RoleNames = roleNames
        };
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task<PrincipalAccess> GetPrincipalAccessAsync(
        string principalId,
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Validate the scope name.
        (var vrScopeName, var normalizedScopeName) =
            _scopeNameValidator.Validate(
                scopeName);

        vrScopeName.ValidateOrThrow("scopeName");

        // Verify the resource exists before retrieving principal access.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName}' not found.");

        // Verify the scope exists before retrieving principal access.
        if (_scopeNameValidator.IsDefault(normalizedScopeName!) is false)
        {
            _ = await GetScopeAsync(
                resourceName: normalizedResourceName!,
                scopeName: normalizedScopeName!,
                cancellationToken: cancellationToken)
                ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Scope '{normalizedScopeName}' not found.");
        }

        // Start both assignment retrieval operations concurrently.
            var scopeAssignmentsTask = GetScopesForPrincipalAsync(
            principalId: principalId,
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        var roleAssignmentsTask = GetRolesForPrincipalAsync(
            principalId: principalId,
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        // Wait for both operations to complete.
        await Task.WhenAll(scopeAssignmentsTask, roleAssignmentsTask);

        // Filter scope assignments based on whether the scope name is default or specific.
        var scopeNames = _scopeNameValidator.IsDefault(normalizedScopeName!)
            ? scopeAssignmentsTask.Result
            : scopeAssignmentsTask.Result.Where(s => s == normalizedScopeName!).ToArray();

        // Only include role assignments if the principal has matching scope assignments.
        var roleNames = scopeNames.Length > 0
            ? roleAssignmentsTask.Result
            : [];

        // Return the filtered principal access information.
        return new PrincipalAccess
        {
            PrincipalId = principalId,
            ResourceName = normalizedResourceName!,
            ScopeNames = scopeNames,
            RoleNames = roleNames
        };
    }
}
