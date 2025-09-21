using System.Net;
using Trelnex.Auth.Amazon.Services.RBAC.Models;
using Trelnex.Auth.Amazon.Services.RBAC.Scopes;
using Trelnex.Core.Exceptions;
using Trelnex.Core.Observability;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal partial class RBACRepository
{
    /// <inheritdoc/>
    [TraceMethod]
    public async Task CreateScopeAsync(
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

        // Verify the resource exists before creating the scope.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName!}' not found.");

        // Create the scope item for storage.
        var createItem = new ScopeItem(
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!);

        // Store the scope in DynamoDB.
        await CreateAsync(
            items: [createItem],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task DeleteScopeAsync(
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

        // Create the scope item to be deleted.
        var deleteItem = new ScopeItem(
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!);

        // Delete the scope to prevent any new scope assignments from being created under it.
        await DeleteAsync(
            items: [deleteItem],
            cancellationToken: cancellationToken);

        // Delete the scope assignments associated with this scope.
        await DeleteScopeAssignmentsByScopeAsync(
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task DeleteScopesAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Validate the resource name.
        (var vrResourceName, var normalizedResourceName) =
            _resourceNameValidator.Validate(
                resourceName);

        vrResourceName.ValidateOrThrow("resourceName");

        // Retrieve all existing scopes for the resource.
        var scopes = await GetScopesAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);

        // Exit early if no scopes exist.
        if (scopes.Length == 0) return;

        // Create scope items to be deleted.
        var deleteItems = scopes
            .Select(scopeName => new ScopeItem(
                resourceName: normalizedResourceName!,
                scopeName: scopeName))
            .ToArray();

        // Delete all scopes for the resource to prevent any new scope assignments from being created under them.
        await DeleteAsync(
            items: deleteItems,
            cancellationToken: cancellationToken);

        // Delete all scope assignments for the resource.
        await DeleteScopeAssignmentsByResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task<Scope?> GetScopeAsync(
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

        // Verify the resource exists before attempting to retrieve the scope.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName!}' not found.");

        // Create the scope item key for retrieval.
        var getItem = new ScopeItem(
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!);

        // Attempt to retrieve the scope from DynamoDB.
        var scopeItem = await GetAsync(
            getItem,
            ScopeItem.FromAttributeMap,
            cancellationToken);

        // Return null if the scope does not exist.
        if (scopeItem is null) return null;

        // Convert the scope item to the public model.
        return new Scope
        {
            ResourceName = scopeItem.ResourceName,
            ScopeName = scopeItem.ScopeName
        };
    }

    /// <summary>
    /// Retrieves all scope names for a specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An array of scope names ordered alphabetically.</returns>
    [TraceMethod]
    private async Task<string[]> GetScopesAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Create a query item to find all scopes for the resource.
        var queryRequest = ScopeItem.CreateQueryRequest(
            tableName: _tableName,
            resourceName: resourceName);

        // Execute the query to retrieve all scope items.
        var scopeItems = await QueryAsync(
            queryRequest,
            ScopeItem.FromAttributeMap,
            cancellationToken);

        // Extract scope names and return them in alphabetical order.
        return scopeItems
            .Select(scopeItem => scopeItem.ScopeName)
            .Order()
            .ToArray();
    }
}
