using System.Configuration;
using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Trelnex.Auth.Amazon.Services.RBAC.Models;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Exceptions;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal interface IRBACRepository
{
    #region Principals

    /// <summary>
    /// Deletes all assignments for the specified principal.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal to be deleted.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <remarks>
    /// This operation cascades to delete all role and scope assignments for the principal.
    /// </remarks>
    Task DeletePrincipalAsync(
        string principalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the access information for a principal within a specific resource.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal.</param>
    /// <param name="resourceName">The name of the resource to check access for.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <returns>A <see cref="PrincipalAccess"/> object containing the principal's roles and scopes for the resource.</returns>
    /// <remarks>
    /// Returns roles only if the principal has scope assignments within the resource.
    /// </remarks>
    Task<PrincipalAccess> GetPrincipalAccessAsync(
        string principalId,
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the access information for a principal within a specific resource and scope.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal.</param>
    /// <param name="resourceName">The name of the resource to check access for.</param>
    /// <param name="scopeName">The name of the scope to filter access by.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <returns>A <see cref="PrincipalAccess"/> object containing the principal's roles for the specific scope.</returns>
    /// <remarks>
    /// Returns roles only if the principal has the specified scope assignment within the resource.
    /// </remarks>
    Task<PrincipalAccess> GetPrincipalAccessAsync(
        string principalId,
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Resources

    /// <summary>
    /// Creates a new resource with the specified name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to be created.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <remarks>
    /// Resources represent protected assets that can be accessed through scope and roles.
    /// </remarks>
    Task CreateResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource to be deleted.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    Task DeleteResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the resource with the specified name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to retrieve.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <returns>A <see cref="Resource"/> object with details about the resource, or <see langword="null"/> if not found.</returns>
    Task<Resource?> GetResourceAsync(
        string resourceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of all available resources.
    /// </summary>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <returns>An array of resources.</returns>
    Task<string[]> GetResourcesAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Roles

    /// <summary>
    /// Creates a new role for the specified resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the role is being created.</param>
    /// <param name="roleName">The name of the role to be created.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <remarks>
    /// Roles define what actions principals can perform on resources.
    /// </remarks>
    Task CreateRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified role for the given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the role is being deleted.</param>
    /// <param name="roleName">The name of the role to be deleted.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    Task DeleteRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the specified role for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the role is being retrieved.</param>
    /// <param name="roleName">The name of the role to retrieve.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <returns>A <see cref="Role"/> object with details about the role, or <see langword="null"/> if not found.</returns>
    Task<Role?> GetRoleAsync(
        string resourceName,
        string roleName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Role Assignments

    /// <summary>
    /// Creates a new role assignment for the specified principal and role.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the role is being assigned.</param>
    /// <param name="roleName">The name of the role being assigned to the principal.</param>
    /// <param name="principalId">The unique identifier of the principal being assigned to the role.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// Role assignments define which authorization boundaries a principal can access within a resource.
    /// </remarks>
    Task CreateRoleAssignmentAsync(
        string resourceName,
        string roleName,
        string principalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified role assignment for the given principal and role.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the role assignment is being deleted.</param>
    /// <param name="roleName">The name of the role being removed from the principal.</param>
    /// <param name="principalId">The unique identifier of the principal whose role assignment is being deleted.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    Task DeleteRoleAssignmentAsync(
        string resourceName,
        string roleName,
        string principalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all principals assigned to the specified role within a resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource containing the role.</param>
    /// <param name="roleName">The name of the role to query principals for.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <returns>An array of principal identifiers assigned to the role, ordered alphabetically.</returns>
    Task<string[]> GetPrincipalsForRoleAsync(
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
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <remarks>
    /// Scopes define authorization boundaries for roles within a resource.
    /// </remarks>
    Task CreateScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified scope for the given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the scope is being deleted.</param>
    /// <param name="scopeName">The name of the scope to be deleted.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    Task DeleteScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the specified scope for a given resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the scope is being retrieved.</param>
    /// <param name="scopeName">The name of the scope to retrieve.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <returns>A <see cref="Scope"/> object with details about the scope, or <see langword="null"/> if not found.</returns>
    Task<Scope?> GetScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Scope Assignments

    /// <summary>
    /// Creates a new scope assignment for the specified principal and scope.
    /// </summary>
    /// <param name="resourceName">The name of the resource for which the scope is being assigned.</param>
    /// <param name="scopeName">The name of the scope being assigned to the principal.</param>
    /// <param name="principalId">The unique identifier of the principal being assigned to the scope.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    /// <remarks>
    /// Scope assignments define which authorization boundaries a principal can access within a resource.
    /// </remarks>
    Task CreateScopeAssignmentAsync(
        string resourceName,
        string scopeName,
        string principalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified scope assignment for the given principal and scope.
    /// </summary>
    /// <param name="resourceName">The name of the resource from which the scope assignment is being deleted.</param>
    /// <param name="scopeName">The name of the scope being removed from the principal.</param>
    /// <param name="principalId">The unique identifier of the principal whose scope assignment is being deleted.</param>
    /// <param name="cancellationToken">An optional token for canceling the operation.</param>
    Task DeleteScopeAssignmentAsync(
        string resourceName,
        string scopeName,
        string principalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all principals assigned to the specified scope within a resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource containing the scope.</param>
    /// <param name="scopeName">The name of the scope to query principals for.</param>
    /// <param name="cancellationToken">An optional token for cancelling the operation.</param>
    /// <returns>An array of principal identifiers assigned to the scope, ordered alphabetically.</returns>
    Task<string[]> GetPrincipalsForScopeAsync(
        string resourceName,
        string scopeName,
        CancellationToken cancellationToken = default);

    #endregion
}

internal partial class RBACRepository : IRBACRepository
{
    #region Private Fields

    /// <summary>
    /// The validator for determining if a resource name is valid.
    /// </summary>
    private readonly IResourceNameValidator _resourceNameValidator;

    /// <summary>
    /// The validator for determining if a scope name is valid and is the default scope.
    /// </summary>
    private readonly IScopeNameValidator _scopeNameValidator;

    /// <summary>
    /// The validator for determining if a role name is valid.
    /// </summary>
    private readonly IRoleNameValidator _roleNameValidator;

    /// <summary>
    /// The DynamoDB client for database operations.
    /// </summary>
    private readonly AmazonDynamoDBClient _client;

    /// <summary>
    /// The name of the DynamoDB table storing RBAC data.
    /// </summary>
    private readonly string _tableName;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="RBACRepository"/> class.
    /// </summary>
    /// <param name="resourceNameValidator">The validator for determining if a resource name is valid.</param>
    /// <param name="scopeNameValidator">The validator for determining if a scope name is valid and is the default scope.</param>
    /// <param name="roleNameValidator">The validator for determining if a role name is valid.</param>
    /// <param name="client">The DynamoDB client for database operations.</param>
    /// <param name="tableName">The name of the DynamoDB table storing RBAC data.</param>
    /// <remarks>
    /// This constructor initializes the specialized repositories for each entity type,
    /// all using the same DynamoDB table with different partition and sort key patterns.
    /// </remarks>
    internal RBACRepository(
        IResourceNameValidator resourceNameValidator,
        IScopeNameValidator scopeNameValidator,
        IRoleNameValidator roleNameValidator,
        AmazonDynamoDBClient client,
        string tableName)
    {
        _resourceNameValidator = resourceNameValidator;
        _scopeNameValidator = scopeNameValidator;
        _roleNameValidator = roleNameValidator;
        _client = client;
        _tableName = tableName;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="RBACRepository"/> with configuration settings.
    /// </summary>
    /// <param name="configuration">The application configuration containing RBAC settings.</param>
    /// <param name="resourceNameValidator">The validator for determining if a resource name is valid.</param>
    /// <param name="scopeNameValidator">The validator for determining if a scope name is valid and is the default scope.</param>
    /// <param name="roleNameValidator">The validator for determining if a role name is valid.</param>
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
        IResourceNameValidator resourceNameValidator,
        IScopeNameValidator scopeNameValidator,
        IRoleNameValidator roleNameValidator,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        // Get the AWS credentials from the provider.
        var credentials = credentialProvider.GetCredential();

        // Extract RBAC configuration from application settings.
        var rbacConfiguration = configuration
            .GetSection("RBAC")
            .Get<RBACConfiguration>()
            ?? throw new ConfigurationErrorsException("The RBAC configuration is not valid.");

        // Get the AWS region endpoint from the configuration.
        var regionEndpoint = RegionEndpoint.GetBySystemName(rbacConfiguration.Region);

        // Create a DynamoDB client with the credentials and region.
        var client = new AmazonDynamoDBClient(credentials, regionEndpoint);

        // Create and return the RBAC repository.
        return new RBACRepository(
            resourceNameValidator,
            scopeNameValidator,
            roleNameValidator,
            client,
            rbacConfiguration.TableName);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates the specified items in the DynamoDB table.
    /// </summary>
    /// <param name="item">The items to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    private async Task CreateAsync<TBaseItem>(
        TBaseItem[] items,
        CancellationToken cancellationToken)
        where TBaseItem : BaseItem
    {
        // If there are no items to create, return.
        if (items.Length is 0) return;

        // Create the put requests.
        var putRequests = items.Select(item => new PutRequest { Item = item.ToAttributeMap() });
        var writeRequests = putRequests.Select(putRequest => new WriteRequest { PutRequest = putRequest });

        var batchWriteRequest = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                { _tableName, writeRequests.ToList() }
            }
        };

        try
        {
            // Write until complete.
            while (batchWriteRequest.RequestItems.Count > 0)
            {
                // Create the items.
                var response = await _client.BatchWriteItemAsync(batchWriteRequest, cancellationToken);

                // Set the unprocessed items for the next iteration.
                batchWriteRequest.RequestItems = response.UnprocessedItems;
            }
        }
        catch (AmazonDynamoDBException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Deletes the specified items from the DynamoDB table.
    /// </summary>
    /// <param name="items">The items to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    private async Task DeleteAsync<TBaseItem>(
        TBaseItem[] items,
        CancellationToken cancellationToken)
        where TBaseItem : BaseItem
    {
        // If there are no items to delete, return.
        if (items.Length is 0) return;

        // Create the delete requests.
        var deleteRequests = items.Select(item => new DeleteRequest { Key = item.Key });
        var writeRequests = deleteRequests.Select(deleteRequest => new WriteRequest { DeleteRequest = deleteRequest });

        var batchWriteRequest = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                { _tableName, writeRequests.ToList() }
            }
        };

        try
        {
            // Delete until complete.
            while (batchWriteRequest.RequestItems.Count > 0)
            {
                // Delete the items.
                var response = await _client.BatchWriteItemAsync(batchWriteRequest, cancellationToken);

                // Set the unprocessed items for the next iteration.
                batchWriteRequest.RequestItems = response.UnprocessedItems;
            }
        }
        catch (AmazonDynamoDBException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Gets the specified item from the DynamoDB table.
    /// </summary>
    /// <param name="item">The item to get (only key properties are used).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The item if found; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    private async Task<TBaseItem?> GetAsync<TBaseItem>(
        TBaseItem item,
        Func<Dictionary<string, AttributeValue>, TBaseItem?> fromAttributeMap,
        CancellationToken cancellationToken)
        where TBaseItem : BaseItem
    {
        // Create the get request.
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = item.Key,
            ConsistentRead = true // Use consistent read for up-to-date data.
        };

        try
        {
            // Get the item.
            var response = await _client.GetItemAsync(request, cancellationToken);

            // Convert the attribute map to the item.
            return fromAttributeMap(response.Item);
        }
        catch (AmazonDynamoDBException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Queries the DynamoDB table for items matching the specified criteria.
    /// </summary>
    /// <param name="queryRequest">The query request containing the criteria for the query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An array of matching items.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    private async Task<TBaseItem[]> QueryAsync<TBaseItem>(
        QueryRequest queryRequest,
        Func<Dictionary<string, AttributeValue>, TBaseItem?> fromAttributeMap,
        CancellationToken cancellationToken)
        where TBaseItem : BaseItem
    {
        var result = new List<TBaseItem>();

        try
        {
            do
            {
                var response = await _client.QueryAsync(queryRequest, cancellationToken);

                var items = response.Items
                    .Select(fromAttributeMap)
                    .OfType<TBaseItem>();

                result.AddRange(items);

                // Continue pagination if there are more results
                queryRequest.ExclusiveStartKey = response.LastEvaluatedKey;

            } while (queryRequest.ExclusiveStartKey?.Count > 0);

            return result.ToArray();
        }
        catch (AmazonDynamoDBException ex)
        {
            throw new HttpStatusCodeException(
                HttpStatusCode.ServiceUnavailable,
                ex.Message);
        }
    }

    #endregion

    #region Nested Types

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

    #endregion
}
