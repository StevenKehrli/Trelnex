using System.Net;
using Trelnex.Auth.Amazon.Services.RBAC.RoleAssignments;
using Trelnex.Core.Exceptions;
using Trelnex.Core.Observability;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal partial class RBACRepository
{
    #region Public Methods

    /// <inheritdoc/>
    [TraceMethod]
    public async Task CreateRoleAssignmentAsync(
        string resourceName,
        string roleName,
        string principalId,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Validate the role name.
        (var vrRoleName, var normalizedRoleName) =
            _roleNameValidator.Validate(
                roleName);

        vrRoleName.ValidateOrThrow("roleName");

        // Verify the resource exists before creating the role assignment.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName!}' not found.");

        // Verify the role exists before creating the role assignment.
        _ = await GetRoleAsync(
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Role '{normalizedRoleName!}' not found.");

        // Create the role assignment items for storage.
        var byPrincipalItem = new ByPrincipalItem(
            principalId: principalId,
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!);

        var byRoleItem = new ByRoleItem(
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!,
            principalId: principalId);

        // Store the role assignment items in DynamoDB.
        await CreateAsync<BaseItem>(
            items: [byPrincipalItem, byRoleItem],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task DeleteRoleAssignmentAsync(
        string resourceName,
        string roleName,
        string principalId,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Validate the role name.
        (var vrRoleName, var normalizedRoleName) =
            _roleNameValidator.Validate(
                roleName);

        vrRoleName.ValidateOrThrow("roleName");

        // Create the role assignment items to be deleted.
        var byPrincipalItem = new ByPrincipalItem(
            principalId: principalId,
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!);

        var byRoleItem = new ByRoleItem(
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!,
            principalId: principalId);

        // Delete the role assignment items from DynamoDB.
        await DeleteAsync<BaseItem>(
            items: [byPrincipalItem, byRoleItem],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task<string[]> GetPrincipalsForRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Validate the role name.
        (var vrRoleName, var normalizedRoleName) =
            _roleNameValidator.Validate(
                roleName);

        vrRoleName.ValidateOrThrow("roleName");

        // Verify the resource exists before creating the role assignment.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName!}' not found.");

        // Verify the role exists before creating the role assignment.
        _ = await GetRoleAsync(
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Role '{normalizedRoleName!}' not found.");

        // Create a query request to find all role assignments for the role.
        var queryRequest = ByRoleItem.CreateQueryRequest(
            tableName: _tableName,
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!);

        // Execute the query to retrieve all role assignment items.
        var byRoleItems = await QueryAsync(
            queryRequest,
            ByRoleItem.FromAttributeMap,
            cancellationToken);

        // Extract principal IDs and return them in alphabetical order.
        return byRoleItems
            .Select(item => item.PrincipalId)
            .Order()
            .ToArray();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Deletes all role assignments for a specified principal.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [TraceMethod]
    private async Task DeleteRoleAssignmentsByPrincipalAsync(
        string principalId,
        CancellationToken cancellationToken = default)
    {
        // Create a query request to find all role assignments for the resource.
        var queryRequest = ByPrincipalItem.CreateQueryRequest(
            tableName: _tableName,
            principalId: principalId);

        // Execute the query to retrieve all role assignment items.
        var byPrincipalItems = await QueryAsync(
            queryRequest,
            ByPrincipalItem.FromAttributeMap,
            cancellationToken);

        // Exit early if no role assignments exist.
        if (byPrincipalItems.Length == 0) return;

        // Create corresponding ByRoleItem instances for deletion.
        var byRoleItems = byPrincipalItems
            .Select(item => new ByRoleItem(
                resourceName: item.ResourceName,
                roleName: item.RoleName,
                principalId: item.PrincipalId))
            .ToArray();

        // Combine all items for deletion.
        var deleteItems = new BaseItem[byPrincipalItems.Length + byRoleItems.Length];
        Array.Copy(byPrincipalItems, 0, deleteItems, 0, byPrincipalItems.Length);
        Array.Copy(byRoleItems, 0, deleteItems, byPrincipalItems.Length, byRoleItems.Length);

        // Delete all role assignment items.
        await DeleteAsync(
            items: deleteItems,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes all role assignments for a specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [TraceMethod]
    private async Task DeleteRoleAssignmentsByResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Create a query request to find all role assignments for the resource.
        var queryRequest = ByRoleItem.CreateQueryRequest(
            tableName: _tableName,
            resourceName: resourceName);

        // Execute the query to retrieve all role assignment items.
        var byRoleItems = await QueryAsync(
            queryRequest,
            ByRoleItem.FromAttributeMap,
            cancellationToken);

        // Exit early if no role assignments exist.
        if (byRoleItems.Length == 0) return;

        // Create corresponding ByPrincipalItem instances for deletion.
        var byPrincipalItems = byRoleItems
            .Select(item => new ByPrincipalItem(
                principalId: item.PrincipalId,
                resourceName: item.ResourceName,
                roleName: item.RoleName))
            .ToArray();

        // Combine all items for deletion.
        var deleteItems = new BaseItem[byRoleItems.Length + byPrincipalItems.Length];
        Array.Copy(byRoleItems, 0, deleteItems, 0, byRoleItems.Length);
        Array.Copy(byPrincipalItems, 0, deleteItems, byRoleItems.Length, byPrincipalItems.Length);

        // Delete all role assignment items.
        await DeleteAsync(
            items: deleteItems,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes all role assignments for a specified role within a resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [TraceMethod]
    private async Task DeleteRoleAssignmentsByRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // Create a query request to find all role assignments for the role.
        var queryRequest = ByRoleItem.CreateQueryRequest(
            tableName: _tableName,
            resourceName: resourceName,
            roleName: roleName);

        // Execute the query to retrieve all role assignment items.
        var byRoleItems = await QueryAsync(
            queryRequest,
            ByRoleItem.FromAttributeMap,
            cancellationToken);

        // Exit early if no role assignments exist.
        if (byRoleItems.Length == 0) return;

        // Create corresponding ByPrincipalItem instances for deletion.
        var byPrincipalItems = byRoleItems
            .Select(item => new ByPrincipalItem(
                principalId: item.PrincipalId,
                resourceName: item.ResourceName,
                roleName: item.RoleName))
            .ToArray();

        // Combine all items for deletion.
        var deleteItems = new BaseItem[byRoleItems.Length + byPrincipalItems.Length];
        Array.Copy(byRoleItems, 0, deleteItems, 0, byRoleItems.Length);
        Array.Copy(byPrincipalItems, 0, deleteItems, byRoleItems.Length, byPrincipalItems.Length);

        // Delete all role assignment items.
        await DeleteAsync(
            items: deleteItems,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves all role names assigned to a specified principal within a resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="principalId">The unique identifier of the principal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An array of role names ordered alphabetically.</returns>
    [TraceMethod]
    private async Task<string[]> GetRolesForPrincipalAsync(
        string principalId,
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Create a query request to find all role assignments for the principal.
        var queryRequest = ByPrincipalItem.CreateQueryRequest(
            tableName: _tableName,
            principalId: principalId,
            resourceName: resourceName);

        // Execute the query to retrieve all role assignment items.
        var byPrincipalItems = await QueryAsync(
            queryRequest,
            ByPrincipalItem.FromAttributeMap,
            cancellationToken);

        // Extract role names and return them in alphabetical order.
        return byPrincipalItems
            .Select(item => item.RoleName)
            .Order()
            .ToArray();
    }

    #endregion
}
