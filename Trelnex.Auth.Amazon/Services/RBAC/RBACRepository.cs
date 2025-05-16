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

/// <summary>
/// Defines the operations for managing Role-Based Access Control (RBAC) entities.
/// </summary>
/// <remarks>
/// This interface provides methods for managing the core RBAC components:
/// - Principals (users or services)
/// - Resources (protected assets)
/// - Roles (collections of permissions)
/// - Scopes (authorization boundaries)
/// - Role assignments (mapping principals to roles for specific resources)
///
/// The RBAC system follows a hierarchical model where:
/// 1. Resources are protected assets that can be accessed
/// 2. Roles define what actions can be performed on resources
/// 3. Scopes limit the context in which roles apply
/// 4. Principals are assigned roles on resources for specific scopes
/// </remarks>
internal interface IRBACRepository
{
    #region Principals

    /// <summary>
    /// Deletes a principal and all its role assignments from the RBAC system.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (user or service) to delete.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// This operation removes all role assignments associated with the principal
    /// across all resources. The operation is irreversible, and any JWT tokens
    /// issued to the principal will remain valid until they expire.
    /// </remarks>
    Task DeletePrincipalAsync(
        string principalId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Principal Memberships

    /// <summary>
    /// Retrieves the role memberships for a specified principal, resource and scope.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (user or service).</param>
    /// <param name="resourceName">The name of the resource to check permissions for.</param>
    /// <param name="scopeName">The optional scope name to filter by (null for all scopes).</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <returns>A <see cref="PrincipalMembership"/> containing the roles assigned to the principal.</returns>
    /// <remarks>
    /// This method retrieves the roles assigned to a principal for a specific resource.
    /// If scopeName is null or the default scope, all scopes are included in the result.
    /// This information is typically used for generating JWT tokens with appropriate claims.
    /// </remarks>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with status 404 NotFound when the resource doesn't exist.
    /// </exception>
    Task<PrincipalMembership> GetPrincipalMembershipAsync(
        string principalId,
        string resourceName,
        string? scopeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a specified role to a principal for a given resource.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (user or service).</param>
    /// <param name="resourceName">The name of the resource for which the role is being granted.</param>
    /// <param name="roleName">The name of the role being granted to the principal.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <returns>A <see cref="PrincipalMembership"/> containing the updated roles for the principal.</returns>
    /// <remarks>
    /// This method creates a new role assignment mapping a principal to a role for a specific resource.
    /// The operation fails if either the resource or role does not exist.
    /// This change takes effect immediately for new token issuance.
    /// </remarks>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with status 404 NotFound when the resource or role doesn't exist.
    /// </exception>
    Task<PrincipalMembership> GrantRoleToPrincipalAsync(
        string principalId,
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specified role from a principal for a given resource.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (user or service).</param>
    /// <param name="resourceName">The name of the resource for which the role is being revoked.</param>
    /// <param name="roleName">The name of the role being revoked from the principal.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <returns>A <see cref="PrincipalMembership"/> containing the updated roles for the principal.</returns>
    /// <remarks>
    /// This method removes a role assignment for a principal on a specific resource.
    /// If the role assignment does not exist, the operation is a no-op.
    /// This change takes effect immediately for new token issuance, but existing tokens
    /// will remain valid until they expire.
    /// </remarks>
    Task<PrincipalMembership> RevokeRoleFromPrincipalAsync(
        string principalId,
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Role Assignments

    /// <summary>
    /// Retrieves the role assignment for a specific role and resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the role.</param>
    /// <param name="roleName">The name of the role whose assignment is being retrieved.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <returns>A <see cref="RoleAssignment"/> containing the principals assigned to the role.</returns>
    /// <remarks>
    /// This method retrieves all principals that have been granted a specific role on a resource.
    /// The operation fails if either the resource or role does not exist.
    /// This is useful for auditing access permissions and understanding role distribution.
    /// </remarks>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with status 404 NotFound when the resource or role doesn't exist.
    /// </exception>
    Task<RoleAssignment> GetRoleAssignmentAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Resources

    /// <summary>
    /// Creates a new resource with the specified name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to be created.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// This method creates a new resource in the RBAC system.
    /// Resources represent protected assets that can be accessed through roles.
    /// New resources have no roles or scopes associated with them until they are explicitly created.
    /// If a resource with the same name already exists, the operation is idempotent.
    /// </remarks>
    Task CreateResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified resource, along with any associated roles and scopes.
    /// </summary>
    /// <param name="resourceName">The name of the resource to be deleted.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// This method removes a resource and all its associated entities from the RBAC system:
    /// - All roles defined for the resource
    /// - All scopes defined for the resource
    /// - All principal role assignments for the resource
    ///
    /// The operation is executed in parallel for efficiency and is irreversible.
    /// If the resource does not exist, the operation is a no-op.
    /// </remarks>
    Task DeleteResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the resource with the specified name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to retrieve.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <returns>A <see cref="Resource"/> object with details about the resource, or <see langword="null"/> if not found.</returns>
    /// <remarks>
    /// This method retrieves a resource along with its associated roles and scopes.
    /// The data is retrieved in parallel for efficiency.
    /// If the resource doesn't exist, null is returned.
    /// </remarks>
    Task<Resource?> GetResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of all available resources.
    /// </summary>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <returns>An array of resource names available in the system, sorted alphabetically.</returns>
    /// <remarks>
    /// This method returns only the resource names, not the full resource objects.
    /// To get detailed information about a specific resource, use <see cref="GetResourceAsync"/>.
    /// If no resources exist, an empty array is returned.
    /// </remarks>
    Task<string[]> GetResourcesAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Roles

    /// <summary>
    /// Creates a new role for the specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the role is being created.</param>
    /// <param name="roleName">The name of the role to be created.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// This method creates a new role for a specific resource.
    /// Roles define what actions principals can perform on resources.
    /// The operation fails if the resource does not exist.
    /// If a role with the same name already exists for the resource, the operation is idempotent.
    /// </remarks>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with status 404 NotFound when the resource doesn't exist.
    /// </exception>
    Task CreateRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified role for the given resource, including any associated role memberships.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the role is being deleted.</param>
    /// <param name="roleName">The name of the role to be deleted.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// This method removes a role and all its assignments from the RBAC system.
    /// The operation is executed in parallel for efficiency and is irreversible.
    /// All principal-role mappings for this role are also deleted.
    /// If the role does not exist, the operation is a no-op.
    /// This change takes effect immediately for new token issuance, but existing tokens
    /// will remain valid until they expire.
    /// </remarks>
    Task DeleteRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the specified role for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the role.</param>
    /// <param name="roleName">The name of the role to retrieve.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <returns>A <see cref="Role"/> object with details about the role, or <see langword="null"/> if not found.</returns>
    /// <remarks>
    /// This method retrieves information about a specific role for a resource.
    /// The operation fails if the resource does not exist.
    /// If the role doesn't exist for the specified resource, null is returned.
    /// </remarks>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with status 404 NotFound when the resource doesn't exist.
    /// </exception>
    Task<Role?> GetRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Scopes

    /// <summary>
    /// Creates a new scope for the specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the scope is being created.</param>
    /// <param name="scopeName">The name of the scope to be created.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// This method creates a new scope for a specific resource.
    /// Scopes define authorization boundaries for roles within a resource.
    /// The operation fails if the resource does not exist.
    /// If a scope with the same name already exists for the resource, the operation is idempotent.
    /// </remarks>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with status 404 NotFound when the resource doesn't exist.
    /// </exception>
    Task CreateScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified scope for the given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the scope is being deleted.</param>
    /// <param name="scopeName">The name of the scope to be deleted.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// This method removes a scope from the RBAC system.
    /// If the scope does not exist, the operation is a no-op.
    /// This change takes effect immediately for new token issuance, but existing tokens
    /// with this scope will remain valid until they expire.
    /// </remarks>
    Task DeleteScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the specified scope for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the scope.</param>
    /// <param name="scopeName">The name of the scope to retrieve.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <returns>A <see cref="Scope"/> object with details about the scope, or <see langword="null"/> if not found.</returns>
    /// <remarks>
    /// This method retrieves information about a specific scope for a resource.
    /// The operation fails if the resource does not exist.
    /// If the scope doesn't exist for the specified resource, null is returned.
    /// </remarks>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with status 404 NotFound when the resource doesn't exist.
    /// </exception>
    Task<Scope?> GetScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Implements Role-Based Access Control (RBAC) operations using DynamoDB as the backend storage.
/// </summary>
/// <remarks>
/// This class provides a complete implementation of the RBAC system, using specialized repositories
/// for each entity type (resources, roles, scopes, and principal-role mappings).
///
/// The RBAC repository orchestrates operations across these specialized repositories,
/// maintaining referential integrity and enforcing business rules.
/// </remarks>
internal class RBACRepository : IRBACRepository
{
    #region Private Fields

    /// <summary>
    /// The validator for determining if a scope name is the default scope.
    /// </summary>
    private readonly IScopeNameValidator _scopeNameValidator;

    /// <summary>
    /// Repository for managing principal-role mappings.
    /// </summary>
    private readonly PrincipalRoleRepository _principalRoleRepository;

    /// <summary>
    /// Repository for managing resources.
    /// </summary>
    private readonly ResourceRepository _resourceRepository;

    /// <summary>
    /// Repository for managing roles.
    /// </summary>
    private readonly RoleRepository _roleRepository;

    /// <summary>
    /// Repository for managing scopes.
    /// </summary>
    private readonly ScopeRepository _scopeRepository;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="RBACRepository"/> class.
    /// </summary>
    /// <param name="scopeNameValidator">The validator for determining if a scope name is the default scope.</param>
    /// <param name="client">The DynamoDB client for database operations.</param>
    /// <param name="tableName">The name of the DynamoDB table storing RBAC data.</param>
    /// <remarks>
    /// This constructor initializes the specialized repositories for each entity type,
    /// all using the same DynamoDB table with different partition and sort key patterns.
    /// </remarks>
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

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="RBACRepository"/> with configuration settings.
    /// </summary>
    /// <param name="configuration">The application configuration containing RBAC settings.</param>
    /// <param name="scopeNameValidator">The validator for determining if a scope name is the default scope.</param>
    /// <param name="credentialProvider">The provider for AWS credentials.</param>
    /// <returns>A configured <see cref="RBACRepository"/> instance.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when the RBAC configuration is missing.</exception>
    /// <remarks>
    /// This factory method creates a fully configured RBAC repository from application settings.
    /// It extracts the DynamoDB table name and AWS region from the configuration,
    /// and sets up the AWS credentials for accessing DynamoDB.
    /// </remarks>
    public static RBACRepository Create(
        IConfiguration configuration,
        IScopeNameValidator scopeNameValidator,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        // Get the AWS credentials from the provider.
        var credentials = credentialProvider.GetCredential();

        // Extract RBAC configuration from application settings.
        var rbacConfiguration = configuration
            .GetSection("RBAC")
            .Get<RBACConfiguration>()
            ?? throw new ConfigurationErrorsException("The RBAC configuration is not found.");

        // Get the AWS region endpoint from the configuration.
        var regionEndpoint = RegionEndpoint.GetBySystemName(rbacConfiguration.Region);

        // Create a DynamoDB client with the credentials and region.
        var client = new AmazonDynamoDBClient(credentials, regionEndpoint);

        // Create and return the RBAC repository.
        return new RBACRepository(scopeNameValidator, client, rbacConfiguration.TableName);
    }

    #endregion

    #region Principals

    /// <summary>
    /// Deletes the specified principal and all associated role assignments.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeletePrincipalAsync(
        string principalId,
        CancellationToken cancellationToken = default)
    {
        // Create a scan item to find all principal roles associated with the principal ID.
        var scanItem = new PrincipalRoleScanItem();
        scanItem.AddPrincipalId(principalId);

        // Scan the PrincipalRoleRepository to find all roles assigned to the principal.
        var principalRoleItems = await _principalRoleRepository
            .ScanAsync(scanItem, cancellationToken);

        // Delete all the principal roles found in the previous step.
        await _principalRoleRepository.DeleteAsync(principalRoleItems, cancellationToken);
    }

    #endregion Principals

    #region Principal Memberships

    /// <summary>
    /// Retrieves the role memberships for a specified principal, resource, and scope.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (e.g., user or service).</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="scopeName">The name of the scope. If null, all scopes are considered.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="PrincipalMembership"/> that represents the principal's role memberships for the specified resource and scope.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown with status 404 NotFound when the resource doesn't exist.</exception>
    public async Task<PrincipalMembership> GetPrincipalMembershipAsync(
        string principalId,
        string resourceName,
        string? scopeName = null,
        CancellationToken cancellationToken = default)
    {
        // Get the resource to ensure it exists.
        var resource = await GetResourceAsync(resourceName, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // Create a scan item to find all principal roles associated with the principal ID and resource name.
        var scanItem = new PrincipalRoleScanItem();
        scanItem.AddPrincipalId(principalId);
        scanItem.AddResourceName(resourceName);

        // Scan the PrincipalRoleRepository to find all roles assigned to the principal for the resource.
        var principalRoleItems = await _principalRoleRepository.ScanAsync(scanItem, cancellationToken);

        // Determine the scope names to filter by. If scopeName is null or the default scope, use all scopes from the resource.
        var scopeNames = (scopeName is null || _scopeNameValidator.IsDefault(scopeName))
            ? resource.ScopeNames
            : [ scopeName ];

        // Get the role names from the principal role items, filtering by roles that exist for the resource, and order them.
        var roleNames = principalRoleItems
            .Select(principalRoleItem => principalRoleItem.RoleName)
            .Where(roleName => resource.RoleNames.Contains(roleName))
            .Order()
            .ToArray();

        // Return the principal membership information.
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
    /// <exception cref="HttpStatusCodeException">Thrown with status 404 NotFound when the resource or role doesn't exist.</exception>
    public async Task<PrincipalMembership> GrantRoleToPrincipalAsync(
        string principalId,
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // Check if the resource exists.
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // Check if the role exists.
        var roleItem = new RoleItem(
            resourceName: resourceName,
            roleName: roleName);

        _ = await _roleRepository.GetAsync(roleItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Role '{roleName}' not found.");

        // Create a new PrincipalRoleItem to represent the granted role.
        var createItem = new PrincipalRoleItem(
            principalId: principalId,
            resourceName: resourceName,
            roleName: roleName);

        // Persist the new PrincipalRoleItem in the PrincipalRoleRepository.
        await _principalRoleRepository.CreateAsync(createItem, cancellationToken);

        // Retrieve and return the updated principal membership information.
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
        // Create a PrincipalRoleItem to represent the role being revoked.
        var deleteItem = new PrincipalRoleItem(
            principalId: principalId,
            resourceName: resourceName,
            roleName: roleName);

        // Delete the PrincipalRoleItem from the PrincipalRoleRepository.
        await _principalRoleRepository.DeleteAsync(deleteItem, cancellationToken);

        // Retrieve and return the updated principal membership information.
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
    /// <exception cref="HttpStatusCodeException">Thrown with status 404 NotFound when the resource or role doesn't exist.</exception>
    public async Task<RoleAssignment> GetRoleAssignmentAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // Check if the resource exists.
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // Check if the role exists.
        var roleItem = new RoleItem(
            resourceName: resourceName,
            roleName: roleName);

        _ = await _roleRepository.GetAsync(roleItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Role '{roleName}' not found.");

        // Create a scan item to find all principals assigned to the role for the resource.
        var scanItem = new PrincipalRoleScanItem();
        scanItem.AddResourceName(resourceName);
        scanItem.AddRoleName(roleName);

        // Scan the PrincipalRoleRepository to find all principals assigned to the role for the resource.
        var principalItems = await _principalRoleRepository.ScanAsync(scanItem, cancellationToken);

        // Get the principal IDs from the principal items and order them.
        var principalIds = principalItems
            .Select(principalItem => principalItem.PrincipalId)
            .Order()
            .ToArray();

        // Return the role assignment information.
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
    public async Task CreateResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Create a new ResourceItem to represent the resource.
        var createItem = new ResourceItem(
            resourceName: resourceName);

        // Persist the new ResourceItem in the ResourceRepository.
        await _resourceRepository.CreateAsync(createItem, cancellationToken);
    }

    /// <summary>
    /// Deletes the specified resource, along with any associated roles and scopes.
    /// </summary>
    /// <param name="resourceName">The name of the resource to be deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeleteResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        // Delete the resource.
        var deleteResourceTask = Task.Run(async () =>
        {
            // Create a ResourceItem to represent the resource to be deleted.
            var deleteItem = new ResourceItem(
                resourceName: resourceName);

            // Delete the ResourceItem from the ResourceRepository.
            await _resourceRepository.DeleteAsync(deleteItem, cancellationToken);
        }, cancellationToken);

        // Delete any scopes associated with the resource.
        var deleteScopesTask = Task.Run(async () =>
        {
            // Create a ScopeScanItem to find all scopes associated with the resource.
            var scopeScanItem = new ScopeScanItem();
            scopeScanItem.AddResourceName(resourceName);

            // Scan the ScopeRepository to find all scopes associated with the resource.
            var scopeItems = await _scopeRepository.ScanAsync(scopeScanItem, cancellationToken);

            // Delete all the scopes found in the previous step.
            await _scopeRepository.DeleteAsync(scopeItems, cancellationToken);
        }, cancellationToken);

        // Delete any roles associated with the resource.
        var deleteRolesTask = Task.Run(async () =>
        {
            // Create a RoleScanItem to find all roles associated with the resource.
            var roleScanItem = new RoleScanItem();
            roleScanItem.AddResourceName(resourceName);

            // Scan the RoleRepository to find all roles associated with the resource.
            var roleItems = await _roleRepository.ScanAsync(roleScanItem, cancellationToken);

            // Delete all the roles found in the previous step.
            await _roleRepository.DeleteAsync(roleItems, cancellationToken);
        }, cancellationToken);

        // Delete any principal roles associated with the resource.
        var deletePrincipalRolesTask = Task.Run(async () =>
        {
            // Create a PrincipalRoleScanItem to find all principal roles associated with the resource.
            var principalRoleScanItem = new PrincipalRoleScanItem();
            principalRoleScanItem.AddResourceName(resourceName);

            // Scan the PrincipalRoleRepository to find all principal roles associated with the resource.
            var principalRoleImtes = await _principalRoleRepository.ScanAsync(principalRoleScanItem, cancellationToken);

            // Delete all the principal roles found in the previous step.
            await _principalRoleRepository.DeleteAsync(principalRoleImtes, cancellationToken);
        }, cancellationToken);

        // Wait for all the tasks to complete.
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
        // Get the resource.
        var getItem = new ResourceItem(
            resourceName: resourceName);

        var getResourceTask = _resourceRepository.GetAsync(getItem, cancellationToken);

        // Get the scopes associated with the resource.
        var getScopesTask = GetScopesAsync(resourceName, cancellationToken);

        // Get the roles associated with the resource.
        var getRolesTask = GetRolesAsync(resourceName, cancellationToken);

        // Wait for all the tasks to complete.
        await Task.WhenAll(getResourceTask, getScopesTask, getRolesTask);

        // If the resource does not exist, return null.
        if (getResourceTask.Result is null) return null;

        // Return the resource information.
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
        // Create a scan item to find all resources.
        var scanItem = new ResourceScanItem();

        // Scan the ResourceRepository to find all resources.
        var resourceItems = await _resourceRepository.ScanAsync(scanItem, cancellationToken);

        // Get the resource names from the resource items and order them.
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
    /// <exception cref="HttpStatusCodeException">Thrown with status 404 NotFound when the resource doesn't exist.</exception>
    public async Task CreateRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // Check if the resource exists.
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // Create a new RoleItem to represent the role.
        var createItem = new RoleItem(
            resourceName: resourceName,
            roleName: roleName);

        // Persist the new RoleItem in the RoleRepository.
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
        // Delete the role.
        var deleteRoleTask = Task.Run(async () =>
        {
            // Create a RoleItem to represent the role to be deleted.
            var deleteItem = new RoleItem(
                resourceName: resourceName,
                roleName: roleName);

            // Delete the RoleItem from the RoleRepository.
            await _roleRepository.DeleteAsync(deleteItem, cancellationToken);
        }, cancellationToken);

        // Delete any principal roles associated with the role.
        var deletePrincipalRolesTask = Task.Run(async () =>
        {
            // Create a PrincipalRoleScanItem to find all principal roles associated with the role.
            var principalRoleScanItem = new PrincipalRoleScanItem();
            principalRoleScanItem.AddResourceName(resourceName);
            principalRoleScanItem.AddRoleName(roleName);

            // Scan the PrincipalRoleRepository to find all principal roles associated with the role.
            var principalRoleItems = await _principalRoleRepository
                .ScanAsync(principalRoleScanItem, cancellationToken);

            // Delete all the principal roles found in the previous step.
            await _principalRoleRepository.DeleteAsync(principalRoleItems, cancellationToken);
        }, cancellationToken);

        // Wait for all the tasks to complete.
        await Task.WhenAll(deleteRoleTask, deletePrincipalRolesTask);
    }

    /// <summary>
    /// Retrieves the specified role for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the role.</param>
    /// <param name="roleName">The name of the role to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Role"/> object, or <c>null</c> if the role does not exist.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown with status 404 NotFound when the resource doesn't exist.</exception>
    public async Task<Role?> GetRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // Check if the resource exists.
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // Create a RoleItem to represent the role to be retrieved.
        var getItem = new RoleItem(
            resourceName: resourceName,
            roleName: roleName);

        // Retrieve the RoleItem from the RoleRepository.
        var roleItem = await _roleRepository.GetAsync(getItem, cancellationToken);

        // If the role does not exist, return null.
        if (roleItem is null) return null;

        // Return the role information.
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
        // Create a scan item to find all roles associated with the resource.
        var scanItem = new RoleScanItem();
        scanItem.AddResourceName(resourceName);

        // Scan the RoleRepository to find all roles associated with the resource.
        var roleItems = await _roleRepository.ScanAsync(scanItem, cancellationToken);

        // Get the role names from the role items and order them.
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
    /// <exception cref="HttpStatusCodeException">Thrown with status 404 NotFound when the resource doesn't exist.</exception>
    public async Task CreateScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default)
    {
        // Check if the resource exists.
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // Create a new ScopeItem to represent the scope.
        var createItem = new ScopeItem(
            resourceName: resourceName,
            scopeName: scopeName);

        // Persist the new ScopeItem in the ScopeRepository.
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
        // Create a ScopeItem to represent the scope to be deleted.
        var deleteItem = new ScopeItem(
            resourceName: resourceName,
            scopeName: scopeName);

        // Delete the ScopeItem from the ScopeRepository.
        await _scopeRepository.DeleteAsync(deleteItem, cancellationToken);
    }

    /// <summary>
    /// Retrieves the specified scope for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with the scope.</param>
    /// <param name="scopeName">The name of the scope to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Scope"/> object, or <c>null</c> if the scope does not exist.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown with status 404 NotFound when the resource doesn't exist.</exception>
    public async Task<Scope?> GetScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default)
    {
        // Check if the resource exists.
        var resourceItem = new ResourceItem(
            resourceName: resourceName);

        _ = await _resourceRepository.GetAsync(resourceItem, cancellationToken)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound, $"Resource '{resourceName}' not found.");

        // Create a ScopeItem to represent the scope to be retrieved.
        var getItem = new ScopeItem(
            resourceName: resourceName,
            scopeName: scopeName);

        // Retrieve the ScopeItem from the ScopeRepository.
        var scopeItem = await _scopeRepository.GetAsync(getItem, cancellationToken);

        // If the scope does not exist, return null.
        if (scopeItem is null) return null;

        // Return the scope information.
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
        // Create a scan item to find all scopes associated with the resource.
        var scanItem = new ScopeScanItem();
        scanItem.AddResourceName(resourceName);

        // Scan the ScopeRepository to find all scopes associated with the resource.
        var scopeItems = await _scopeRepository.ScanAsync(scanItem, cancellationToken);

        // Get the scope names from the scope items and order them.
        return scopeItems
            .Select(scopeItem => scopeItem.ScopeName)
            .Order()
            .ToArray();
    }

    #endregion Scopes

    /// <summary>
    /// Represents the RBAC configuration settings.
    /// </summary>
    private record RBACConfiguration
    {
        /// <summary>
        /// The AWS region name of the DynamoDB table.
        /// </summary>
        public required string Region { get; init; }

        /// <summary>
        /// The DynamoDB table name.
        /// </summary>
        public required string TableName { get; init; }
    }
}
