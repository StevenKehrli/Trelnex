using System.Net;
using Trelnex.Auth.Amazon.Services.RBAC.ScopeAssignments;
using Trelnex.Core.Exceptions;
using Trelnex.Core.Observability;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal partial class RBACRepository
{
    #region Public Methods

    /// <inheritdoc/>
    [TraceMethod]
    public async Task CreateScopeAssignmentAsync(
        string resourceName,
        string scopeName,
        string principalId,
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

        // Verify the resource exists before creating the scope assignment.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName!}' not found.");

        // Verify the scope exists before creating the scope assignment.
        _ = await GetScopeAsync(
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Scope '{normalizedScopeName!}' not found.");

        // Create the scope assignment items for storage.
        var byPrincipalItem = new ByPrincipalItem(
            principalId: principalId,
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!);

        var byScopeItem = new ByScopeItem(
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!,
            principalId: principalId);

        // Store the scope assignment items in DynamoDB.
        await CreateAsync<BaseItem>(
            items: [byPrincipalItem, byScopeItem],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task DeleteScopeAssignmentAsync(
        string resourceName,
        string scopeName,
        string principalId,
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

        // Create the scope assignment items to be deleted.
        var byPrincipalItem = new ByPrincipalItem(
            principalId: principalId,
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!);

        var byScopeItem = new ByScopeItem(
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!,
            principalId: principalId);

        // Delete the scope assignment items from DynamoDB.
        await DeleteAsync<BaseItem>(
            items: [byPrincipalItem, byScopeItem],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    [TraceMethod]
    public async Task<string[]> GetPrincipalsForScopeAsync(
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

        // Verify the resource exists before creating the scope assignment.
        _ = await GetResourceAsync(
            resourceName: normalizedResourceName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{normalizedResourceName!}' not found.");

        // Verify the scope exists before creating the scope assignment.
        _ = await GetScopeAsync(
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!,
            cancellationToken: cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Scope '{normalizedScopeName!}' not found.");

        // Create a query request to find all scope assignments for the scope.
        var queryRequest = ByScopeItem.CreateQueryRequest(
            tableName: _tableName,
            resourceName: normalizedResourceName!,
            scopeName: normalizedScopeName!);

        // Execute the query to retrieve all scope assignment items.
        var byScopeItems = await QueryAsync(
            queryRequest,
            ByScopeItem.FromAttributeMap,
            cancellationToken);

        // Extract principal IDs and return them in alphabetical order.
        return byScopeItems
            .Select(item => item.PrincipalId)
            .Order()
            .ToArray();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Deletes all scope assignments for a specified principal.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [TraceMethod]
    private async Task DeleteScopeAssignmentsByPrincipalAsync(
        string principalId,
        CancellationToken cancellationToken = default)
    {
        // Create a query request to find all scope assignments for the resource.
        var queryRequest = ByPrincipalItem.CreateQueryRequest(
            tableName: _tableName,
            principalId: principalId);

        // Execute the query to retrieve all scope assignment items.
        var byPrincipalItems = await QueryAsync(
            queryRequest,
            ByPrincipalItem.FromAttributeMap,
            cancellationToken);

        // Exit early if no scope assignments exist.
        if (byPrincipalItems.Length == 0) return;

        // Create corresponding ByScopeItem instances for deletion.
        var byScopeItems = byPrincipalItems
            .Select(item => new ByScopeItem(
                resourceName: item.ResourceName,
                scopeName: item.ScopeName,
                principalId: item.PrincipalId))
            .ToArray();

        // Combine all items for deletion.
        var deleteItems = new BaseItem[byPrincipalItems.Length + byScopeItems.Length];
        Array.Copy(byPrincipalItems, 0, deleteItems, 0, byPrincipalItems.Length);
        Array.Copy(byScopeItems, 0, deleteItems, byPrincipalItems.Length, byScopeItems.Length);

        // Delete all scope assignment items.
        await DeleteAsync(
            items: deleteItems,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes all scope assignments for a specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [TraceMethod]
    private async Task DeleteScopeAssignmentsByResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Create a query request to find all scope assignments for the resource.
        var queryRequest = ByScopeItem.CreateQueryRequest(
            tableName: _tableName,
            resourceName: resourceName);

        // Execute the query to retrieve all scope assignment items.
        var byScopeItems = await QueryAsync(
            queryRequest,
            ByScopeItem.FromAttributeMap,
            cancellationToken);

        // Exit early if no scope assignments exist.
        if (byScopeItems.Length == 0) return;

        // Create corresponding ByPrincipalItem instances for deletion.
        var byPrincipalItems = byScopeItems
            .Select(item => new ByPrincipalItem(
                principalId: item.PrincipalId,
                resourceName: item.ResourceName,
                scopeName: item.ScopeName))
            .ToArray();

        // Combine all items for deletion.
        var deleteItems = new BaseItem[byScopeItems.Length + byPrincipalItems.Length];
        Array.Copy(byScopeItems, 0, deleteItems, 0, byScopeItems.Length);
        Array.Copy(byPrincipalItems, 0, deleteItems, byScopeItems.Length, byPrincipalItems.Length);

        // Delete all scope assignment items.
        await DeleteAsync(
            items: deleteItems,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes all scope assignments for a specified scope within a resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="scopeName">The name of the scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [TraceMethod]
    private async Task DeleteScopeAssignmentsByScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default)
    {
        // Create a query request to find all scope assignments for the scope.
        var queryRequest = ByScopeItem.CreateQueryRequest(
            tableName: _tableName,
            resourceName: resourceName,
            scopeName: scopeName);

        // Execute the query to retrieve all scope assignment items.
        var byScopeItems = await QueryAsync(
            queryRequest,
            ByScopeItem.FromAttributeMap,
            cancellationToken);

        // Exit early if no scope assignments exist.
        if (byScopeItems.Length == 0) return;

        // Create corresponding ByPrincipalItem instances for deletion.
        var byPrincipalItems = byScopeItems
            .Select(item => new ByPrincipalItem(
                principalId: item.PrincipalId,
                resourceName: item.ResourceName,
                scopeName: item.ScopeName))
            .ToArray();

        // Combine all items for deletion.
        var deleteItems = new BaseItem[byScopeItems.Length + byPrincipalItems.Length];
        Array.Copy(byScopeItems, 0, deleteItems, 0, byScopeItems.Length);
        Array.Copy(byPrincipalItems, 0, deleteItems, byScopeItems.Length, byPrincipalItems.Length);

        // Delete all scope assignment items.
        await DeleteAsync(
            items: deleteItems,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves all scope names assigned to a specified principal within a resource.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal.</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An array of scope names ordered alphabetically.</returns>
    [TraceMethod]
    private async Task<string[]> GetScopesForPrincipalAsync(
        string principalId,
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Create a query request to find all scope assignments for the principal.
        var queryRequest = ByPrincipalItem.CreateQueryRequest(
            tableName: _tableName,
            principalId: principalId,
            resourceName: resourceName);

        // Execute the query to retrieve all scope assignment items.
        var byPrincipalItems = await QueryAsync(
            queryRequest,
            ByPrincipalItem.FromAttributeMap,
            cancellationToken);

        // Extract scope names and return them in alphabetical order.
        return byPrincipalItems
            .Select(item => item.ScopeName)
            .Order()
            .ToArray();
    }

    #endregion
}
