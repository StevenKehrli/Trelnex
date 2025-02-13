using System.Configuration;
using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Trelnex.Auth.Amazon.Services.RBAC.Models;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal interface IRBACRepository
{

#region Principals

    /// <summary>
    /// Deletes the specified principal
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    Task DeletePrincipalAsync(
        string principalId,
        CancellationToken cancellationToken = default);

#endregion Principals

#region Principal Memberships

    /// <summary>
    /// Retrieves the role memberships for a specified principal, resource and scope.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="scopeName">The name of the scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="PrincipalMembership"/> that represents the principal's role memberships for the specified resource and scope.</returns>
    Task<PrincipalMembership> GetPrincipalMembershipAsync(
        string principalId,
        string resourceName,
        string? scopeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a specified role to a principal for a given resource.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    /// <param name="resourceName">The name of the resource for which the role is being granted.</param>
    /// <param name="roleName">The name of the role being granted to the principal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="PrincipalMembership"/> object representing the new principal membership.</returns>
    Task<PrincipalMembership> GrantRoleToPrincipalAsync(
        string principalId,
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specified role from a principal for a given resource.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    /// <param name="resourceName">The name of the resource for which the role is being revoked.</param>
    /// <param name="roleName">The name of the role being revoked from the principal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="PrincipalMembership"/> object representing the new principal membership.</returns>
    Task<PrincipalMembership> RevokeRoleFromPrincipalAsync(
        string principalId,
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

#endregion Principal Memberships

#region Role Assignments

    /// <summary>
    /// Retrieves the role assignment for a specific role and resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the role.</param>
    /// <param name="roleName">The name of the role whose assignment is being retrieved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="RoleAssignment"/> representing the role assignment for the specified role and resource.</returns>
    Task<RoleAssignment> GetRoleAssignmentAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

#endregion Role Assignments

#region Resources

    /// <summary>
    /// Creates a new resource with the specified name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified resource, along with any associated roles and scopes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="resourceName">The name of the resource to be deleted.</param>
    Task DeleteResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the resource with the specified name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Resource"/> object, or <c>null</c> if the resource does not exist.</returns>
    Task<Resource?> GetResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of all available resources.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An array of names representing all available resources in the system.</returns>
    Task<string[]> GetResourcesAsync(
        CancellationToken cancellationToken = default);

#endregion Resources

#region Roles

    /// <summary>
    /// Creates a new role for the specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the role is being created.</param>
    /// <param name="roleName">The name of the role to be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified role for the given resource, including any associated role memberships.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the role is being deleted.</param>
    /// <param name="roleName">The name of the role to be deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the specified role for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the role.</param>
    /// <param name="roleName">The name of the role to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Role"/> object, or <c>null</c> if the role does not exist.</returns>
    Task<Role?> GetRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

#endregion Roles

#region Scopes

    /// <summary>
    /// Creates a new scope for the specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the scope is being created.</param>
    /// <param name="scopeName">The name of the scope to be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified scope for the given resource, including any associated scope memberships.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the scope is being deleted.</param>
    /// <param name="scopeName">The name of the scope to be deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the specified scope for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the scope.</param>
    /// <param name="scopeName">The name of the scope to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Scope"/> object, or <c>null</c> if the scope does not exist.</returns>
    Task<Scope?> GetScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

#endregion Scopes

}

internal class RBACRepository : IRBACRepository
{
    private readonly IScopeNameValidator _scopeNameValidator;

    private readonly PrincipalRoleRepository _principalRoleRepository;
    private readonly ResourceRepository _resourceRepository;
    private readonly RoleRepository _roleRepository;
    private readonly ScopeRepository _scopeRepository;

    internal RBACRepository(
        IScopeNameValidator scopeNameValidator,
        AmazonDynamoDBClient client,
        string tableName)
    {
        _scopeNameValidator = scopeNameValidator;

        _principalRoleRepository = new PrincipalRoleRepository(client, tableName);
        _resourceRepository = new ResourceRepository(client, tableName);
        _roleRepository = new RoleRepository(client, tableName);
        _scopeRepository = new ScopeRepository(client, tableName);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RBACRepository"/>.
    /// </summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="scopeNameValidator">The scope name validator.</param>
    /// <param name="credentialProvider">The credential provider to get the AWS credentials.</param>
    /// <returns>The <see cref="RBACRepository"/>.</returns>
    public static RBACRepository Create(
        IConfiguration configuration,
        IScopeNameValidator scopeNameValidator,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        var credentials = credentialProvider.GetCredential();

        var rbacConfiguration = configuration
            .GetSection("RBAC")
            .Get<RBACConfiguration>()
            ?? throw new ConfigurationErrorsException("The RBAC configuration is not found.");

        // get the region
        var region = RegionEndpoint.GetBySystemName(rbacConfiguration.Region);

        // create the dynamodb client
        var client = new AmazonDynamoDBClient(credentials, region);

        // create the rbac repository
        return new RBACRepository(scopeNameValidator, client, rbacConfiguration.TableName);
    }

#region Principals

    /// <summary>
    /// Deletes the specified principal
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    public async Task DeletePrincipalAsync(
        string principalId,
        CancellationToken cancellationToken = default)
    {
        // get any principal roles
        var scanItem = new PrincipalRoleScanItem();
        scanItem.AddPrincipalId(principalId);

        // delete the principal roles
        var principalRoleItems = await _principalRoleRepository
            .ScanAsync(scanItem, cancellationToken);

        await _principalRoleRepository.DeleteAsync(principalRoleItems, cancellationToken);
    }

#endregion Principals

#region Principal Memberships

    /// <summary>
    /// Retrieves the role memberships for a specified principal, resource and scope.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="scopeName">The name of the scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="PrincipalMembership"/> that represents the principal's role memberships for the specified resource and scope.</returns>
    public async Task<PrincipalMembership> GetPrincipalMembershipAsync(
        string principalId,
        string resourceName,
        string? scopeName = null,
        CancellationToken cancellationToken = default)
    {
        // get the resource
        var resource = await GetResourceAsync(resourceName, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // scan
        var scanItem = new PrincipalRoleScanItem();
        scanItem.AddPrincipalId(principalId);
        scanItem.AddResourceName(resourceName);

        var principalRoleItems = await _principalRoleRepository.ScanAsync(scanItem, cancellationToken);

        // get the scope names
        var scopeNames = (scopeName is null || _scopeNameValidator.IsDefault(scopeName))
            ? resource.ScopeNames
            : [ scopeName ];

        // get the role names
        var roleNames = principalRoleItems
            .Select(principalRoleItem => principalRoleItem.RoleName)
            .Where(roleName => resource.RoleNames.Contains(roleName))
            .Order()
            .ToArray();

        return new PrincipalMembership
        {
            PrincipalId = principalId,
            ResourceName = resourceName,
            ScopeNames = scopeNames,
            RoleNames = roleNames ?? []
        };
    }

    /// <summary>
    /// Grants a specified role to a principal for a given resource.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    /// <param name="resourceName">The name of the resource for which the role is being granted.</param>
    /// <param name="roleName">The name of the role being granted to the principal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="PrincipalMembership"/> object representing the new principal membership.</returns>
    public async Task<PrincipalMembership> GrantRoleToPrincipalAsync(
        string principalId,
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // check if resource exists
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // check if role exists
        var roleItem = new RoleItem(
            resourceName: resourceName,
            roleName: roleName);

        _ = await _roleRepository.GetAsync(roleItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Role '{roleName}' not found.");

        // create
        var createItem = new PrincipalRoleItem(
            principalId: principalId,
            resourceName: resourceName,
            roleName: roleName);

        await _principalRoleRepository.CreateAsync(createItem, cancellationToken);

        // get
        return await GetPrincipalMembershipAsync(
            principalId: principalId,
            resourceName: resourceName,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Revokes a specified role from a principal for a given resource.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    /// <param name="resourceName">The name of the resource for which the role is being revoked.</param>
    /// <param name="roleName">The name of the role being revoked from the principal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="PrincipalMembership"/> object representing the new principal membership.</returns>
    public async Task<PrincipalMembership> RevokeRoleFromPrincipalAsync(
        string principalId,
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // delete
        var deleteItem = new PrincipalRoleItem(
            principalId: principalId,
            resourceName: resourceName,
            roleName: roleName);

        await _principalRoleRepository.DeleteAsync(deleteItem, cancellationToken);

        // get
        return await GetPrincipalMembershipAsync(
            principalId: principalId,
            resourceName: resourceName,
            cancellationToken: cancellationToken);
    }

#endregion Principal Memberships

#region Role Assignments

    /// <summary>
    /// Retrieves the role assignment for a specific role and resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the role.</param>
    /// <param name="roleName">The name of the role whose assignment is being retrieved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="RoleAssignment"/> representing the role assignment for the specified role and resource.</returns>
    public async Task<RoleAssignment> GetRoleAssignmentAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // check if resource exists
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // check if role exists
        var roleItem = new RoleItem(
            resourceName: resourceName,
            roleName: roleName);

        _ = await _roleRepository.GetAsync(roleItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Role '{roleName}' not found.");

        // scan
        var scanItem = new PrincipalRoleScanItem();
        scanItem.AddResourceName(resourceName);
        scanItem.AddRoleName(roleName);

        var principalItems = await _principalRoleRepository.ScanAsync(scanItem, cancellationToken);

        // get the principal ids
        var principalIds = principalItems
            .Select(principalItem => principalItem.PrincipalId)
            .Order()
            .ToArray();

        return new RoleAssignment
        {
            ResourceName = resourceName,
            RoleName = roleName,
            PrincipalIds = principalIds ?? []
        };
    }

#endregion Role Assignments

#region Resources

    /// <summary>
    /// Creates a new resource with the specified name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Resource"/> object representing the new resource.</returns>
    public async Task CreateResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // create
        var createItem = new ResourceItem(
            resourceName: resourceName);

        await _resourceRepository.CreateAsync(createItem, cancellationToken);
    }

    /// <summary>
    /// Deletes the specified resource, along with any associated roles and scopes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="resourceName">The name of the resource to be deleted.</param>
    public async Task DeleteResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // delete the resource
        var deleteResourceTask = Task.Run(async () =>
        {
            var deleteItem = new ResourceItem(
                resourceName: resourceName);

            await _resourceRepository.DeleteAsync(deleteItem, cancellationToken);
        }, cancellationToken);

        // get any scopes and then delete
        var deleteScopesTask = Task.Run(async () =>
        {
            var scopeScanItem = new ScopeScanItem();
            scopeScanItem.AddResourceName(resourceName);

            var scopeItems = await _scopeRepository.ScanAsync(scopeScanItem, cancellationToken);

            await _scopeRepository.DeleteAsync(scopeItems, cancellationToken);
        }, cancellationToken);

        // get any roles and then delete
        var deleteRolesTask = Task.Run(async () =>
        {
            var roleScanItem = new RoleScanItem();
            roleScanItem.AddResourceName(resourceName);

            var roleItems = await _roleRepository.ScanAsync(roleScanItem, cancellationToken);

            await _roleRepository.DeleteAsync(roleItems, cancellationToken);
        }, cancellationToken);

        // get any principal roles under this resource and then delete
        var deletePrincipalRolesTask = Task.Run(async () =>
        {
            var principalRoleScanItem = new PrincipalRoleScanItem();
            principalRoleScanItem.AddResourceName(resourceName);

            var principalRoleImtes = await _principalRoleRepository.ScanAsync(principalRoleScanItem, cancellationToken);

            await _principalRoleRepository.DeleteAsync(principalRoleImtes, cancellationToken);
        }, cancellationToken);

        // wait for the tasks to complete
        await Task.WhenAll(deleteResourceTask, deleteScopesTask, deleteRolesTask, deletePrincipalRolesTask);
    }

    /// <summary>
    /// Retrieves the resource with the specified name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Resource"/> object, or <c>null</c> if the resource does not exist.</returns>
    public async Task<Resource?> GetResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // get the resource
        var getItem = new ResourceItem(
            resourceName: resourceName);

        var getResourceTask = _resourceRepository.GetAsync(getItem, cancellationToken);

        // get the scopes
        var getScopesTask = GetScopesAsync(resourceName, cancellationToken);

        // get the roles
        var getRolesTask = GetRolesAsync(resourceName, cancellationToken);

        // wait for all tasks to complete
        await Task.WhenAll(getResourceTask, getScopesTask, getRolesTask);

        // if resource does not exist, return null
        if (getResourceTask.Result is null) return null;

        return new Resource
        {
            ResourceName = resourceName,
            ScopeNames = getScopesTask.Result,
            RoleNames = getRolesTask.Result
        };
    }

    /// <summary>
    /// Retrieves a list of all available resources.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An array <see cref="Resource"/> objects representing all available resources in the system.</returns>
    public async Task<string[]> GetResourcesAsync(
        CancellationToken cancellationToken = default)
    {
        var scanItem = new ResourceScanItem();

        var resourceItems = await _resourceRepository.ScanAsync(scanItem, cancellationToken);

        return resourceItems
            .Select(resourceItem => resourceItem.ResourceName)
            .Order()
            .ToArray();
    }

#endregion Resources

#region Roles

    /// <summary>
    /// Creates a new role for the specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the role is being created.</param>
    /// <param name="roleName">The name of the role to be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Role"/> object representing the new role.</returns>
    public async Task CreateRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // check if resource exists
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // create
        var createItem = new RoleItem(
            resourceName: resourceName,
            roleName: roleName);

        await _roleRepository.CreateAsync(createItem, cancellationToken);
    }

    /// <summary>
    /// Deletes the specified role for the given resource, including any associated role memberships.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the role is being deleted.</param>
    /// <param name="roleName">The name of the role to be deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeleteRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // delete the role
        var deleteRoleTask = Task.Run(async () =>
        {
            var deleteItem = new RoleItem(
                resourceName: resourceName,
                roleName: roleName);

            await _roleRepository.DeleteAsync(deleteItem, cancellationToken);
        }, cancellationToken);

        // get any principal roles with this role and delete
        var deletePrincipalRolesTask = Task.Run(async () =>
        {
            var principalRoleScanItem = new PrincipalRoleScanItem();
            principalRoleScanItem.AddResourceName(resourceName);
            principalRoleScanItem.AddRoleName(roleName);

            // delete the principal roles with this role
            var principalRoleItems = await _principalRoleRepository
                .ScanAsync(principalRoleScanItem, cancellationToken);

            await _principalRoleRepository.DeleteAsync(principalRoleItems, cancellationToken);
        }, cancellationToken);

        await Task.WhenAll(deleteRoleTask, deletePrincipalRolesTask);
    }

    /// <summary>
    /// Retrieves the specified role for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the role.</param>
    /// <param name="roleName">The name of the role to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Role"/> object, or <c>null</c> if the role does not exist.</returns>
    public async Task<Role?> GetRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // check if resource exists
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // get
        var getItem = new RoleItem(
            resourceName: resourceName,
            roleName: roleName);

        var roleItem = await _roleRepository.GetAsync(getItem, cancellationToken);

        if (roleItem is null) return null;

        return new Role
        {
            ResourceName = roleItem.ResourceName,
            RoleName = roleItem.RoleName
        };
    }

    /// <summary>
    /// Retrieves a list of all available roles for a specific resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which roles are being retrieved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An array of names representing all available roles for the resource.</returns>
    private async Task<string[]> GetRolesAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // scan
        var scanItem = new RoleScanItem();
        scanItem.AddResourceName(resourceName);

        var roleItems = await _roleRepository.ScanAsync(scanItem, cancellationToken);

        return roleItems
            .Select(roleItem => roleItem.RoleName)
            .Order()
            .ToArray();
    }

#endregion Roles

#region Scopes

    /// <summary>
    /// Creates a new scope for the specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the scope is being created.</param>
    /// <param name="scopeName">The name of the scope to be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Scope"/> object representing the new scope.</returns>
    public async Task CreateScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default)
    {
        // check if resource exists
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // create
        var createItem = new ScopeItem(
            resourceName: resourceName,
            scopeName: scopeName);

        await _scopeRepository.CreateAsync(createItem, cancellationToken);
    }

    /// <summary>
    /// Deletes the specified scope for the given resource, including any associated scope memberships.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the scope is being deleted.</param>
    /// <param name="scopeName">The name of the scope to be deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeleteScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default)
    {
        // delete
        var deleteItem = new ScopeItem(
            resourceName: resourceName,
            scopeName: scopeName);

        await _scopeRepository.DeleteAsync(deleteItem, cancellationToken);
    }

    /// <summary>
    /// Retrieves the specified scope for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the scope.</param>
    /// <param name="scopeName">The name of the scope to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Scope"/> object, or <c>null</c> if the scope does not exist.</returns>
    public async Task<Scope?> GetScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default)
    {
        // check if resource exists
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // get
        var getItem = new ScopeItem(
            resourceName: resourceName,
            scopeName: scopeName);

        var scopeItem = await _scopeRepository.GetAsync(getItem, cancellationToken);

        if (scopeItem is null) return null;

        return new Scope
        {
            ResourceName = scopeItem.ResourceName,
            ScopeName = scopeItem.ScopeName
        };
    }

    /// <summary>
    /// Retrieves a list of all available scopes for a specific resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which scopes are being retrieved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An array of names representing all available scopes for the resource.</returns>
    private async Task<string[]> GetScopesAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // scan
        var scanItem = new ScopeScanItem();
        scanItem.AddResourceName(resourceName);

        var scopeItems = await _scopeRepository.ScanAsync(scanItem, cancellationToken);

        return scopeItems
            .Select(scopeItem => scopeItem.ScopeName)
            .Order()
            .ToArray();
    }

#endregion Scopes

    private record RBACConfiguration
    {
        /// <summary>
        /// The AWS region name of the dynamodb table.
        /// </summary>
        public required string Region { get; init; }

        /// <summary>
        /// The dynamodb table name.
        /// </summary>
        public required string TableName { get; init; }
    }
}
