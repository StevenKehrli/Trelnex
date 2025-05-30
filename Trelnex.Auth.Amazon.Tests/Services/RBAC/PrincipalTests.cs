using Snapshooter.NUnit;
using Trelnex.Core;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

public partial class RBACRepositoryTests
{
    [Test]
    [Description("Deletes a principal with no assignments and verifies no changes occur")]
    public async Task DeletePrincipal()
    {
        // Generate unique test principal ID to ensure test isolation.
        var principalId = "principal-deleteprincipal";

        // Capture the state before deletion for comparison.
        var itemsBefore = await GetItemsAsync();

        // Delete the principal to test the deletion functionality.
        await _repository.DeletePrincipalAsync(principalId: principalId);

        // Capture the state after deletion to verify no changes occurred.
        var o = new
        {
            itemsBefore,
            itemsAfter = await GetItemsAsync()
        };

        // Verify that no changes occurred when deleting a principal with no assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a principal with multiple role assignments and verifies all are removed")]
    public async Task DeletePrincipal_WithMultipleRoleAssignments()
    {
        // Generate unique test names for multiple resources and roles.
        var resourceName1 = "urn://resource-1-deleteprincipal-withmultipleroleassignments";
        var resourceName2 = "urn://resource-2-deleteprincipal-withmultipleroleassignments";

        var roleName1 = "role-1-deleteprincipal-withmultipleroleassignments";
        var roleName2 = "role-2-deleteprincipal-withmultipleroleassignments";

        var principalId = "principal-1-deleteprincipal-withmultipleroleassignments";

        // Set up test data: create resources, roles, and multiple role assignments for the same principal.
        await _repository.CreateResourceAsync(resourceName: resourceName1);
        await _repository.CreateRoleAsync(resourceName: resourceName1, roleName: roleName1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName1, roleName: roleName1, principalId: principalId);

        await _repository.CreateResourceAsync(resourceName: resourceName2);
        await _repository.CreateRoleAsync(resourceName: resourceName2, roleName: roleName2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName2, roleName: roleName2, principalId: principalId);

        // Delete the principal to test removal of all role assignments.
        await _repository.DeletePrincipalAsync(principalId: principalId);

        // Verify that all role assignments for the principal were deleted.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Confirm that only role assignments for this principal were deleted, resources and roles remain.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a principal with multiple scope assignments and verifies all are removed")]
    public async Task DeletePrincipal_WithMultipleScopeAssignments()
    {
        // Generate unique test names for multiple resources and scopes.
        var resourceName1 = "urn://resource-1-deleteprincipal-withmultiplescopeassignments";
        var resourceName2 = "urn://resource-2-deleteprincipal-withmultiplescopeassignments";

        var scopeName1 = "scope-1-deleteprincipal-withmultiplescopeassignments";
        var scopeName2 = "scope-2-deleteprincipal-withmultiplescopeassignments";

        var principalId = "principal-1-deleteprincipal-withmultiplescopeassignments";

        // Set up test data: create resources, scopes, and multiple scope assignments for the same principal.
        await _repository.CreateResourceAsync(resourceName: resourceName1);
        await _repository.CreateScopeAsync(resourceName: resourceName1, scopeName: scopeName1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName1, scopeName: scopeName1, principalId: principalId);

        await _repository.CreateResourceAsync(resourceName: resourceName2);
        await _repository.CreateScopeAsync(resourceName: resourceName2, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName2, scopeName: scopeName2, principalId: principalId);

        // Delete the principal to test removal of all scope assignments.
        await _repository.DeletePrincipalAsync(principalId: principalId);

        // Verify that all scope assignments for the principal were deleted.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Confirm that only scope assignments for this principal were deleted, resources and scopes remain.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a principal with both role and scope assignments and verifies all are removed")]
    public async Task DeletePrincipal_WithRoleAndScopeAssignments()
    {
        // Generate unique test names for the complete hierarchy.
        var resourceName = "urn://resource-deleteprincipal-withroleandscopeassignments";
        var scopeName = "scope-deleteprincipal-withroleandscopeassignments";
        var roleName = "role-deleteprincipal-withroleandscopeassignments";
        var principalId = "principal-deleteprincipal-withroleandscopeassignments";

        // Set up test data: create resource, scope, role, and both types of assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Delete the principal to test removal of all assignments.
        await _repository.DeletePrincipalAsync(principalId: principalId);

        // Verify that all assignments for the principal were deleted.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Confirm complete deletion of principal assignments while preserving resources, scopes, and roles.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access for a resource when principal has both scope and role assignments")]
    public async Task GetPrincipalAccess()
    {
        // Generate unique test names for the complete hierarchy.
        var resourceName = "urn://resource-getprincipalaccess";
        var scopeName = "scope-getprincipalaccess";
        var roleName = "role-getprincipalaccess";
        var principalId = "principal-getprincipalaccess";

        // Set up test data: create resource, scope, role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access to test the retrieval functionality.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Verify the principal access contains both scope and role assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with empty resource name and verifies ValidationException is thrown")]
    public void GetPrincipalAccess_EmptyResourceName()
    {
        // Generate unique test principal ID.
        var principalId = "principal-getprincipalaccess-emptyresourcename";

        // Attempt to get principal access with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access for non-existent resource and verifies HttpStatusCodeException is thrown")]
    public void GetPrincipalAccess_ResourceNotExists()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccess-resourcenotexists";
        var principalId = "principal-getprincipalaccess-resourcenotexists";

        // Attempt to get principal access for non-existent resource should throw HttpStatusCodeException.
        var exception = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName));

        // Verify the correct error details.
        var o = new
        {
            message = exception.Message,
            statusCode = exception.HttpStatusCode
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access when principal has no assignments")]
    public async Task GetPrincipalAccess_WithNoAssignments()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccess-withnoassignments";
        var principalId = "principal-getprincipalaccess-withnoassignments";

        // Set up test data: create resource only, no assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Get principal access to test no-assignments scenario.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Verify the principal access contains no assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access when principal has role assignments but no scope assignments and verifies no roles are returned")]
    public async Task GetPrincipalAccess_WithOnlyRoleAssignments()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccess-withonlyroleassignments";
        var roleName = "role-getprincipalaccess-withonlyroleassignments";
        var principalId = "principal-getprincipalaccess-withonlyroleassignments";

        // Set up test data: create resource, role, and role assignment only (no scope assignments).
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access to test role-only scenario without scope assignments.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Verify no roles are returned when principal has no scope assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access when principal has only scope assignments and no role assignments")]
    public async Task GetPrincipalAccess_WithOnlyScopeAssignments()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccess-withonlyscopeassignments";
        var scopeName = "scope-getprincipalaccess-withonlyscopeassignments";
        var principalId = "principal-getprincipalaccess-withonlyscopeassignments";

        // Set up test data: create resource, scope, and scope assignment only.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        // Get principal access to test scope-only scenario.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Verify the principal access contains scope assignments but no role assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access with specific scope when scope matches assignment")]
    public async Task GetPrincipalAccessWithScope()
    {
        // Generate unique test names for the complete hierarchy.
        var resourceName = "urn://resource-getprincipalaccesswithscope";
        var scopeName = "scope-getprincipalaccesswithscope";
        var roleName = "role-getprincipalaccesswithscope";
        var principalId = "principal-getprincipalaccesswithscope";

        // Set up test data: create resource, scope, role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access with specific scope to test scope filtering.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Verify the principal access is filtered by the specified scope.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access with default scope when principal has assignments")]
    public async Task GetPrincipalAccessWithScope_DefaultScope()
    {
        // Generate unique test names for the complete hierarchy.
        var resourceName = "urn://resource-getprincipalaccesswithscope-defaultscope";
        var scopeName = "scope-getprincipalaccesswithscope-defaultscope";
        var roleName = "role-getprincipalaccesswithscope-defaultscope";
        var principalId = "principal-getprincipalaccesswithscope-defaultscope";

        // Set up test data: create resource, scope, role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access with default scope to test all scope behavior.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: ".default"),
            items = await GetItemsAsync()
        };

        // Verify the principal access returns all scopes and roles when using default scope.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access with default scope when principal has no scope assignments and verifies no roles are returned")]
    public async Task GetPrincipalAccessWithScope_DefaultScopeNoScopeAssignments()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-defaultscopenoscopeassignments";
        var roleName = "role-getprincipalaccesswithscope-defaultscopenoscopeassignments";
        var principalId = "principal-getprincipalaccesswithscope-defaultscopenoscopeassignments";

        // Set up test data: create resource, role, and role assignment only (no scope assignments).
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access with default scope when no scope assignments exist.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: ".default"),
            items = await GetItemsAsync()
        };

        // Verify no roles are returned when principal has no scope assignments, even with default scope.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with scope using empty resource name and verifies ValidationException is thrown")]
    public void GetPrincipalAccessWithScope_EmptyResourceName()
    {
        // Generate unique test names.
        var scopeName = "scope-getprincipalaccesswithscope-emptyresourcename";
        var principalId = "principal-getprincipalaccesswithscope-emptyresourcename";

        // Attempt to get principal access with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: string.Empty, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with scope using empty scope name and verifies ValidationException is thrown")]
    public void GetPrincipalAccessWithScope_EmptyScopeName()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-emptyscopename";
        var principalId = "principal-getprincipalaccesswithscope-emptyscopename";

        // Attempt to get principal access with empty scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with scope using invalid resource name format and verifies ValidationException is thrown")]
    public void GetPrincipalAccessWithScope_InvalidResourceName()
    {
        // Generate unique test names.
        var scopeName = "scope-getprincipalaccesswithscope-invalidresourcename";
        var principalId = "principal-getprincipalaccesswithscope-invalidresourcename";

        // Attempt to get principal access with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: "invalid-resource-name", scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with scope using invalid scope name format and verifies ValidationException is thrown")]
    public void GetPrincipalAccessWithScope_InvalidScopeName()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-invalidscopename";
        var principalId = "principal-getprincipalaccesswithscope-invalidscopename";

        // Attempt to get principal access with invalid scope name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: "Invalid_Scope_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access with scope when principal has no matching scope assignments and verifies no roles are returned")]
    public async Task GetPrincipalAccessWithScope_NoMatchingScope()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-nomatchingscope";

        var scopeName = "scope-getprincipalaccesswithscope-nomatchingscope";
        var differentScopeName = "scope-different-getprincipalaccesswithscope-nomatchingscope";

        var roleName = "role-getprincipalaccesswithscope-nomatchingscope";
        var principalId = "principal-getprincipalaccesswithscope-nomatchingscope";

        // Set up test data: create resource, different scope, role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: differentScopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access with scope that doesn't match assignments.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: differentScopeName),
            items = await GetItemsAsync()
        };

        // Verify no scopes or roles are returned when requested scope doesn't match principal's scope assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with scope using null resource name and verifies ValidationException is thrown")]
    public void GetPrincipalAccessWithScope_NullResourceName()
    {
        // Generate unique test names.
        var principalId = "principal-getprincipalaccesswithscope-nullresourcename";

        // Attempt to get principal access with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: null!, scopeName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with scope using null scope name and verifies ValidationException is thrown")]
    public void GetPrincipalAccessWithScope_NullScopeName()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-nullscopename";
        var principalId = "principal-getprincipalaccesswithscope-nullscopename";

        // Attempt to get principal access with null scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with scope for non-existent resource and verifies HttpStatusCodeException is thrown")]
    public void GetPrincipalAccessWithScope_ResourceNotExists()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-resourcenotexists";
        var scopeName = "scope-getprincipalaccesswithscope-resourcenotexists";
        var principalId = "principal-getprincipalaccesswithscope-resourcenotexists";

        // Attempt to get principal access for non-existent resource should throw HttpStatusCodeException.
        var exception = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: scopeName));

        // Verify the correct error details.
        var o = new
        {
            message = exception.Message,
            statusCode = exception.HttpStatusCode
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with scope for non-existent scope and verifies HttpStatusCodeException is thrown")]
    public async Task GetPrincipalAccessWithScope_ScopeNotExists()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-scopenotexists";
        var scopeName = "scope-getprincipalaccesswithscope-scopenotexists";
        var principalId = "principal-getprincipalaccesswithscope-scopenotexists";

        // Create resource but not the scope we'll try to query.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to get principal access for non-existent scope should throw HttpStatusCodeException.
        var exception = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: scopeName));

        // Verify the correct error details.
        var o = new
        {
            message = exception.Message,
            statusCode = exception.HttpStatusCode
        };

        Snapshot.Match(o);
    }
}
