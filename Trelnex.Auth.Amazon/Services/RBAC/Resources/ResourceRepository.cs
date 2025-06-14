using Trelnex.Auth.Amazon.Services.RBAC.Models;
using Trelnex.Auth.Amazon.Services.RBAC.Resources;
using Trelnex.Core.Observability;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal partial class RBACRepository
{
    /// <inheritdoc/>
    [TraceMethod]
    public async Task CreateResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Create the resource item for storage.
        var createItem = new ResourceItem(resourceName: normalizedResourceName!);

        // Store the resource in DynamoDB.
        await CreateAsync(
            items: [createItem],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task DeleteResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Create the resource item to be deleted.
        var deleteItem = new ResourceItem(resourceName: normalizedResourceName!);

        // Delete the resource to prevent any new scopes or roles from being created under it.
        await DeleteAsync(
            items: [deleteItem],
            cancellationToken: cancellationToken);

        // Start the delete scopes operation.
        var deleteScopesTask = DeleteScopesAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        // Start the delete roles operation.
        var deleteRolesTask = DeleteRolesAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        // Wait for all operations to complete.
        await Task.WhenAll(deleteScopesTask, deleteRolesTask);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task<Resource?> GetResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Create the resource item key for retrieval.
        var getItem = new ResourceItem(resourceName: normalizedResourceName!);

        // Attempt to retrieve the resource from DynamoDB.
        var resourceItemTask = GetAsync(
            getItem,
            ResourceItem.FromAttributeMap,
            cancellationToken);

        var scopeNamesTask = GetScopesAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        var roleNamesTask = GetRolesAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        await Task.WhenAll(resourceItemTask, scopeNamesTask, roleNamesTask);

        // Return null if the resource does not exist.
        if (resourceItemTask.Result is null) return null;

        // Convert the resource item to the public model.
        return new Resource
        {
            ResourceName = resourceItemTask.Result.ResourceName,
            ScopeNames = scopeNamesTask.Result,
            RoleNames = roleNamesTask.Result
        };
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task<string[]> GetResourcesAsync(
        CancellationToken cancellationToken = default)
    {
        // Create a query item to find all roles for the resource.
        var queryRequest = ResourceItem.CreateQueryRequest(
            tableName: _tableName);

        // Execute the query to retrieve all resource items.
        var resourceItems = await QueryAsync(
            queryRequest,
            ResourceItem.FromAttributeMap,
            cancellationToken);

        // Extract resource names and return them in alphabetical order.
        return resourceItems
            .Select(resourceItem => resourceItem.ResourceName)
            .Order()
            .ToArray();
    }
}
