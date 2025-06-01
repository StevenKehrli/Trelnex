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
    [Description("Deletes a principal with multiple scope and role assignments and verifies all are removed")]
    public async Task DeletePrincipal_WithMultipleScopeAndRoleAssignments()
    {
        // Generate unique test names for the complete hierarchy.
        var resourceName = "urn://resource-deleteprincipal-withmultiplescopeandroleassignments";
        var scopeName1 = "scope-1-deleteprincipal-withmultiplescopeandroleassignments";
        var scopeName2 = "scope-2-deleteprincipal-withmultiplescopeandroleassignments";
        var roleName1 = "role-1-deleteprincipal-withmultiplescopeandroleassignments";
        var roleName2 = "role-2-deleteprincipal-withmultiplescopeandroleassignments";
        var principalId = "principal-deleteprincipal-withmultiplescopeandroleassignments";

        // Set up test data: create resource, multiple scopes, multiple roles, and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName1, principalId: principalId);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName2, principalId: principalId);

        // Delete the principal to test removal of all assignments.
        await _repository.DeletePrincipalAsync(principalId: principalId);

        // Verify that all scope and role assignments for the principal were deleted.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Confirm complete deletion of all principal assignments while preserving resources, scopes, and roles.
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
    [Description("Deletes a principal with both scope and role assignments and verifies all are removed")]
    public async Task DeletePrincipal_WithScopeAndRoleAssignments()
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
    [Description("Attempts to get principal access with invalid resource name format and verifies ValidationException is thrown")]
    public void GetPrincipalAccess_InvalidResourceName()
    {
        // Generate unique test principal ID.
        var principalId = "principal-getprincipalaccess-invalidresourcename";

        // Attempt to get principal access with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: "invalid-resource-name"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with null resource name and verifies ValidationException is thrown")]
    public void GetPrincipalAccess_NullResourceName()
    {
        // Generate unique test principal ID.
        var principalId = "principal-getprincipalaccess-nullresourcename";

        // Attempt to get principal access with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: null!));

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
    [Description("Gets principal access when principal has assignments across multiple resources")]
    public async Task GetPrincipalAccess_WithMultipleResources()
    {
        // Generate unique test names.
        var resourceName1 = "urn://resource-1-getprincipalaccess-acrossmultipleresources";
        var resourceName2 = "urn://resource-2-getprincipalaccess-acrossmultipleresources";
        var scopeName1 = "scope-1-getprincipalaccess-acrossmultipleresources";
        var scopeName2 = "scope-2-getprincipalaccess-acrossmultipleresources";
        var roleName1 = "role-1-getprincipalaccess-acrossmultipleresources";
        var roleName2 = "role-2-getprincipalaccess-acrossmultipleresources";
        var principalId = "principal-getprincipalaccess-acrossmultipleresources";

        // Set up test data: create multiple resources with assignments for the same principal.
        await _repository.CreateResourceAsync(resourceName: resourceName1);
        await _repository.CreateScopeAsync(resourceName: resourceName1, scopeName: scopeName1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName1, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName1, roleName: roleName1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName1, roleName: roleName1, principalId: principalId);

        await _repository.CreateResourceAsync(resourceName: resourceName2);
        await _repository.CreateScopeAsync(resourceName: resourceName2, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName2, scopeName: scopeName2, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName2, roleName: roleName2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName2, roleName: roleName2, principalId: principalId);

        // Get principal access for first resource only.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName1),
            items = await GetItemsAsync()
        };

        // Verify only assignments for the specified resource are returned, not assignments from other resources.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access when principal has multiple role assignments but only one scope assignment")]
    public async Task GetPrincipalAccess_WithMultipleRoleAssignments()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccess-withmultipleroleassignments";
        var scopeName = "scope-getprincipalaccess-withmultipleroleassignments";
        var roleName1 = "role-1-getprincipalaccess-withmultipleroleassignments";
        var roleName2 = "role-2-getprincipalaccess-withmultipleroleassignments";
        var principalId = "principal-getprincipalaccess-withmultipleroleassignments";

        // Set up test data: create resource, one scope, multiple roles, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName1, principalId: principalId);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName2, principalId: principalId);

        // Get principal access to test multiple role assignments scenario.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Verify the principal access contains all role assignments since scope assignment exists.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access when principal has multiple scope and multiple role assignments within the same resource")]
    public async Task GetPrincipalAccess_WithMultipleScopeAndRoleAssignments()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccess-withmultiplescopeandroleassignments";
        var scopeName1 = "scope-1-getprincipalaccess-withmultiplescopeandroleassignments";
        var scopeName2 = "scope-2-getprincipalaccess-withmultiplescopeandroleassignments";
        var roleName1 = "role-1-getprincipalaccess-withmultiplescopeandroleassignments";
        var roleName2 = "role-2-getprincipalaccess-withmultiplescopeandroleassignments";
        var principalId = "principal-getprincipalaccess-withmultiplescopeandroleassignments";

        // Set up test data: create resource, multiple scopes, multiple roles, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName1, principalId: principalId);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName2, principalId: principalId);

        // Get principal access to test multiple scope and role assignments scenario.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Verify the principal access contains all scope and role assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access when principal has multiple scope assignments within the same resource")]
    public async Task GetPrincipalAccess_WithMultipleScopeAssignments()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccess-withmultiplescopeassignments";
        var scopeName1 = "scope-1-getprincipalaccess-withmultiplescopeassignments";
        var scopeName2 = "scope-2-getprincipalaccess-withmultiplescopeassignments";
        var roleName = "role-getprincipalaccess-withmultiplescopeassignments";
        var principalId = "principal-getprincipalaccess-withmultiplescopeassignments";

        // Set up test data: create resource, multiple scopes, one role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access to test multiple scope assignments scenario.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Verify the principal access contains all scope assignments and role assignment.
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
    [Description("Gets principal access with default scope when principal has multiple scope assignments")]
    public async Task GetPrincipalAccessWithScope_DefaultScopeWithMultipleScopes()
    {
        // Generate unique test names for the complete hierarchy.
        var resourceName = "urn://resource-getprincipalaccesswithscope-defaultscopewithmultiplescopes";
        var scopeName1 = "scope-1-getprincipalaccesswithscope-defaultscopewithmultiplescopes";
        var scopeName2 = "scope-2-getprincipalaccesswithscope-defaultscopewithmultiplescopes";
        var roleName = "role-getprincipalaccesswithscope-defaultscopewithmultiplescopes";
        var principalId = "principal-getprincipalaccesswithscope-defaultscopewithmultiplescopes";

        // Set up test data: create resource, multiple scopes, role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access with default scope to test all scope behavior with multiple scopes.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: ".default"),
            items = await GetItemsAsync()
        };

        // Verify the principal access returns all multiple scopes and roles when using default scope.
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
    [Description("Gets principal access with scope when principal has multiple scope assignments and one matches the specified scope")]
    public async Task GetPrincipalAccessWithScope_MatchingOneOfMultipleScopes()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-matchingoneofmultiplescopes";

        var scopeName1 = "scope-1-getprincipalaccesswithscope-matchingoneofmultiplescopes";
        var scopeName2 = "scope-2-getprincipalaccesswithscope-matchingoneofmultiplescopes";

        var roleName = "role-getprincipalaccesswithscope-matchingoneofmultiplescopes";
        var principalId = "principal-getprincipalaccesswithscope-matchingoneofmultiplescopes";

        // Set up test data: create resource, multiple scopes, role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access with specific scope that matches one of the principal's multiple scope assignments.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: scopeName1),
            items = await GetItemsAsync()
        };

        // Verify only the matching scope and associated roles are returned when requested scope matches one of the principal's multiple scope assignments.
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
    [Description("Gets principal access with scope when principal has multiple scope assignments but none match the specified scope")]
    public async Task GetPrincipalAccessWithScope_NoMatchingMultipleScopes()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-nomatchingmultiplescopes";

        var scopeName1 = "scope-1-getprincipalaccesswithscope-nomatchingmultiplescopes";
        var scopeName2 = "scope-2-getprincipalaccesswithscope-nomatchingmultiplescopes";
        var differentScopeName = "scope-different-getprincipalaccesswithscope-nomatchingmultiplescopes";

        var roleName = "role-getprincipalaccesswithscope-nomatchingmultiplescopes";
        var principalId = "principal-getprincipalaccesswithscope-nomatchingmultiplescopes";

        // Set up test data: create resource, multiple scopes, role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: differentScopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Get principal access with scope that doesn't match any of the principal's multiple scope assignments.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: differentScopeName),
            items = await GetItemsAsync()
        };

        // Verify no scopes or roles are returned when requested scope doesn't match any of the principal's multiple scope assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to get principal access with null resource name and verifies ValidationException is thrown")]
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

    [Test]
    [Description("Gets principal access with scope when principal has assignments across multiple resources")]
    public async Task GetPrincipalAccessWithScope_WithMultipleResources()
    {
        // Generate unique test names.
        var resourceName1 = "urn://resource-1-getprincipalaccesswithscope-acrossmultipleresources";
        var resourceName2 = "urn://resource-2-getprincipalaccesswithscope-acrossmultipleresources";
        var scopeName1 = "scope-1-getprincipalaccesswithscope-acrossmultipleresources";
        var scopeName2 = "scope-2-getprincipalaccesswithscope-acrossmultipleresources";
        var roleName1 = "role-1-getprincipalaccesswithscope-acrossmultipleresources";
        var roleName2 = "role-2-getprincipalaccesswithscope-acrossmultipleresources";
        var principalId = "principal-getprincipalaccesswithscope-acrossmultipleresources";

        // Set up test data: create multiple resources with scope and role assignments for the same principal.
        await _repository.CreateResourceAsync(resourceName: resourceName1);
        await _repository.CreateScopeAsync(resourceName: resourceName1, scopeName: scopeName1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName1, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName1, roleName: roleName1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName1, roleName: roleName1, principalId: principalId);

        await _repository.CreateResourceAsync(resourceName: resourceName2);
        await _repository.CreateScopeAsync(resourceName: resourceName2, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName2, scopeName: scopeName2, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName2, roleName: roleName2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName2, roleName: roleName2, principalId: principalId);

        // Get principal access with scope for first resource only.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName1, scopeName: scopeName1),
            items = await GetItemsAsync()
        };

        // Verify only assignments for the specified resource and scope are returned, not assignments from other resources.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Gets principal access with scope when principal has multiple role assignments and matching scope")]
    public async Task GetPrincipalAccessWithScope_WithMultipleRoleAssignments()
    {
        // Generate unique test names.
        var resourceName = "urn://resource-getprincipalaccesswithscope-withmultipleroleassignments";
        var scopeName = "scope-getprincipalaccesswithscope-withmultipleroleassignments";
        var roleName1 = "role-1-getprincipalaccesswithscope-withmultipleroleassignments";
        var roleName2 = "role-2-getprincipalaccesswithscope-withmultipleroleassignments";
        var principalId = "principal-getprincipalaccesswithscope-withmultipleroleassignments";

        // Set up test data: create resource, scope, multiple roles, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName1, principalId: principalId);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName2, principalId: principalId);

        // Get principal access with specific scope when principal has multiple role assignments.
        var o = new
        {
            principalAccess = await _repository.GetPrincipalAccessAsync(principalId: principalId, resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Verify the matching scope and all associated role assignments are returned.
        Snapshot.Match(o);
    }
}
