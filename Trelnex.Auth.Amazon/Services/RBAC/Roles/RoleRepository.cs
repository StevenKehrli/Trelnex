using System.Net;
using Trelnex.Auth.Amazon.Services.RBAC.Models;
using Trelnex.Auth.Amazon.Services.RBAC.Roles;
using Trelnex.Core.Exceptions;
using Trelnex.Core.Observability;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal partial class RBACRepository
{
    /// <inheritdoc/>
    [TraceMethod]
    public async Task CreateRoleAsync(
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

        // Verify the resource exists before creating the role.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName}' not found.");

        // Create the role item for storage.
        var createItem = new RoleItem(
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!);

        // Store the role in DynamoDB.
        await CreateAsync(
            items: [createItem],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task DeleteRoleAsync(
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

        // Create the role item to be deleted.
        var deleteItem = new RoleItem(
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!);

        // Delete the role to prevent any new role assignments from being created under it.
        await DeleteAsync(
            items: [deleteItem],
            cancellationToken: cancellationToken);

        // Delete the role assignments associated with this role.
        await DeleteRoleAssignmentsByRoleAsync(
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task DeleteRolesAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Retrieve all existing roles for the resource.
        var roles = await GetRolesAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        // Exit early if no roles exist.
        if (roles.Length == 0) return;

        // Create role items to be deleted.
        var deleteItems = roles
            .Select(roleName => new RoleItem(
                resourceName: normalizedResourceName!,
                roleName: roleName))
            .ToArray();

        // Delete all roles for the resource to prevent any new role assignments from being created under them.
        await DeleteAsync(
            items: deleteItems,
            cancellationToken: cancellationToken);

        // Delete the role assignments associated with this resource.
        await DeleteRoleAssignmentsByResourceAsync(
            resourceName: resourceName,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task<Role?> GetRoleAsync(
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

        // Verify the resource exists before attempting to retrieve the role.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName}' not found.");

        // Create the role item key for retrieval.
        var getItem = new RoleItem(
            resourceName: normalizedResourceName!,
            roleName: normalizedRoleName!);

        // Attempt to retrieve the role from DynamoDB.
        var roleItem = await GetAsync(
            getItem,
            RoleItem.FromAttributeMap,
            cancellationToken);

        // Return null if the role does not exist.
        if (roleItem is null) return null;

        // Convert the role item to the public model.
        return new Role
        {
            ResourceName = roleItem.ResourceName,
            RoleName = roleItem.RoleName
        };
    }

    /// <summary>
    /// Retrieves all role names for a specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An array of role names ordered alphabetically.</returns>
    [TraceMethod]
    private async Task<string[]> GetRolesAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Create a query item to find all roles for the resource.
        var queryRequest = RoleItem.CreateQueryRequest(
            tableName: _tableName,
            resourceName: resourceName);

        // Execute the query to retrieve all role items.
        var roleItems = await QueryAsync(
            queryRequest,
            RoleItem.FromAttributeMap,
            cancellationToken);

        // Extract role names and return them in alphabetical order.
        return roleItems
            .Select(roleItem => roleItem.RoleName)
            .Order()
            .ToArray();
    }
}
