using Snapshooter.NUnit;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

public partial class RBACRepositoryTests
{
    [Test]
    [Description("Creates a new resource and verifies it exists in the system")]
    public async Task CreateResource()
    {
        // Generate unique test resource name to ensure test isolation.
        var resourceName = "urn://resource-createresource";

        // Create the resource in the RBAC system.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Capture the state after creation to verify the operation succeeded.
        var o = new
        {
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Verify the resource was created correctly using snapshot testing.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates a resource that already exists and verifies idempotent behavior")]
    public async Task CreateResource_AlreadyExists()
    {
        // Generate unique test resource name to ensure test isolation.
        var resourceName = "urn://resource-createresource-alreadyexists";

        // Create the resource for the first time.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Capture the state after first creation.
        var resourcesAfterFirst = await _repository.GetResourcesAsync(default);
        var itemsAfterFirst = await GetItemsAsync();

        // Create the same resource again to test idempotent behavior.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Capture the state after second creation to verify no changes.
        var o = new
        {
            resourcesAfterFirst,
            itemsAfterFirst,
            resourcesAfterSecond = await _repository.GetResourcesAsync(default),
            itemsAfterSecond = await GetItemsAsync()
        };

        // Verify that creating the same resource twice is idempotent (no errors, no duplicates).
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a resource with empty name and verifies ValidationException is thrown")]
    public async Task CreateResource_EmptyName()
    {
        // Attempt to create a resource with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateResourceAsync(resourceName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message,
            resourcesAfter = await _repository.GetResourcesAsync(default)
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a resource with invalid name format and verifies ValidationException is thrown")]
    public async Task CreateResource_InvalidName()
    {
        // Attempt to create a resource with invalid format (not URI-like) should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateResourceAsync(resourceName: "invalid-resource-name"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message,
            resourcesAfter = await _repository.GetResourcesAsync(default)
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a resource with null name and verifies ValidationException is thrown")]
    public async Task CreateResource_NullName()
    {
        // Attempt to create a resource with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateResourceAsync(resourceName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message,
            resourcesAfter = await _repository.GetResourcesAsync(default)
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes an existing resource and verifies it no longer exists")]
    public async Task DeleteResource()
    {
        // Generate unique test resource name to ensure test isolation.
        var resourceName = "urn://resource-deleteresource";

        // Create a resource that will be deleted in this test.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Capture the state before deletion for comparison.
        var resourcesBefore = await _repository.GetResourcesAsync(default);

        // Delete the resource to test the deletion functionality.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Capture the state after deletion to verify the resource was removed.
        var o = new
        {
            resourcesBefore,
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Verify the resource was successfully deleted.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a resource with empty name and verifies ValidationException is thrown")]
    public void DeleteResource_EmptyName()
    {
        // Attempt to delete a resource with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteResourceAsync(resourceName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple resources, deletes one, and verifies the others remain unchanged")]
    public async Task DeleteResource_FromMultiple()
    {
        // Generate unique test resource names to ensure test isolation.
        var resourceName1 = "urn://resource-deleteresource-frommultiple-1";
        var resourceName2 = "urn://resource-deleteresource-frommultiple-2";
        var resourceName3 = "urn://resource-deleteresource-frommultiple-3";

        // Create three resources for the deletion test.
        await _repository.CreateResourceAsync(resourceName: resourceName1);
        await _repository.CreateResourceAsync(resourceName: resourceName2);
        await _repository.CreateResourceAsync(resourceName: resourceName3);

        // Capture the state before deletion for comparison.
        var resourcesBefore = await _repository.GetResourcesAsync(default);

        // Delete the middle resource to test selective deletion.
        await _repository.DeleteResourceAsync(resourceName: resourceName2);

        // Capture the state after deletion to verify only one resource was removed.
        var o = new
        {
            resourcesBefore,
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Verify that only the specified resource was deleted and others remain.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a resource with invalid name format and verifies ValidationException is thrown")]
    public void DeleteResource_InvalidName()
    {
        // Attempt to delete a resource with invalid format (not URI-like) should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteResourceAsync(resourceName: "invalid-resource-name"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a non-existent resource and verifies no changes occur")]
    public async Task DeleteResource_NotExists()
    {
        // Generate a unique name for a resource that will not be created.
        var resourceName = "urn://resource-deleteresource-notexists";

        // Capture the initial state before attempting deletion.
        var resourcesBefore = await _repository.GetResourcesAsync(default);

        // Attempt to delete a resource that doesn't exist to test error handling.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Capture the state after the failed deletion attempt.
        var o = new
        {
            resourcesBefore,
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Verify that no changes occurred when deleting a non-existent resource.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a resource with null name and verifies ValidationException is thrown")]
    public void DeleteResource_NullName()
    {
        // Attempt to delete a resource with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteResourceAsync(resourceName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with multiple roles having multiple assignments and verifies complete cascade deletion")]
    public async Task DeleteResource_WithMultipleRoleAssignments()
    {
        // Generate unique test names for resource, roles, and principals.
        var resourceName = "urn://resource-deleteresource-withmultipleroleassignments";

        var roleName1 = "role-1-deleteresource-withmultipleroleassignments";
        var roleName2 = "role-2-deleteresource-withmultipleroleassignments";

        var principalId1 = "principal-1-deleteresource-withmultipleroleassignments";
        var principalId2 = "principal-2-deleteresource-withmultipleroleassignments";
        var principalId3 = "principal-3-deleteresource-withmultipleroleassignments";
        var principalId4 = "principal-4-deleteresource-withmultipleroleassignments";

        // Set up test data: create resource, multiple roles, and multiple assignments per role.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName1, principalId: principalId1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName1, principalId: principalId2);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName2, principalId: principalId3);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName2, principalId: principalId4);

        // Delete the parent resource to test complete cascade deletion.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify that all related entities were deleted in cascade.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Confirm complete cascade deletion of resource, roles, and all assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with multiple roles and verifies cascade deletion of all roles")]
    public async Task DeleteResource_WithMultipleRoles()
    {
        // Generate unique test names for resource and multiple roles.
        var resourceName = "urn://resource-deleteresource-withmultipleroles";

        var roleName1 = "role-1-deleteresource-withmultipleroles";
        var roleName2 = "role-2-deleteresource-withmultipleroles";

        // Set up test data: create resource and multiple associated roles.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);

        // Delete the parent resource to test cascade deletion of all roles.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify that resource and all roles were deleted in cascade.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Confirm cascade deletion worked correctly for multiple roles.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with multiple scopes having multiple assignments and verifies complete cascade deletion")]
    public async Task DeleteResource_WithMultipleScopeAssignments()
    {
        // Generate unique test names for resource, scopes, and principals.
        var resourceName = "urn://resource-deleteresource-withmultiplescopeassignments";

        var scopeName1 = "scope-1-deleteresource-withmultiplescopeassignments";
        var scopeName2 = "scope-2-deleteresource-withmultiplescopeassignments";

        var principalId1 = "principal-1-deleteresource-withmultiplescopeassignments";
        var principalId2 = "principal-2-deleteresource-withmultiplescopeassignments";
        var principalId3 = "principal-3-deleteresource-withmultiplescopeassignments";
        var principalId4 = "principal-4-deleteresource-withmultiplescopeassignments";

        // Set up test data: create resource, multiple scopes, and multiple assignments per scope.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId2);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId3);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId4);

        // Delete the parent resource to test complete cascade deletion.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify that all related entities were deleted in cascade.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Confirm complete cascade deletion of resource, scopes, and all assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with multiple scopes and verifies cascade deletion of all scopes")]
    public async Task DeleteResource_WithMultipleScopes()
    {
        // Generate unique test names for resource and multiple scopes.
        var resourceName = "urn://resource-deleteresource-withmultiplescopes";

        var scopeName1 = "scope-1-deleteresource-withmultiplescopes";
        var scopeName2 = "scope-2-deleteresource-withmultiplescopes";

        // Set up test data: create resource and multiple associated scopes.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);

        // Delete the parent resource to test cascade deletion of all scopes.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify that resource and all scopes were deleted in cascade.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Confirm cascade deletion worked correctly for multiple scopes.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with an associated role and verifies cascade deletion")]
    public async Task DeleteResource_WithRole()
    {
        // Generate unique test names for both resource and role.
        var resourceName = "urn://resource-deleteresource-withrole";
        var roleName = "role-deleteresource-withrole";

        // Set up test data: create resource and associated role.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Delete the parent resource to test cascade deletion of roles.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify that both resource and role were deleted in cascade.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Confirm cascade deletion worked correctly.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with a role assignment and verifies complete cascade deletion")]
    public async Task DeleteResource_WithRoleAssignment()
    {
        // Generate unique test names for the complete hierarchy.
        var resourceName = "urn://resource-deleteresource-withroleassignment";
        var roleName = "role-deleteresource-withroleassignment";
        var principalId = "principal-deleteresource-withroleassignment";

        // Set up test data: create resource, role, and role assignment.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Delete the parent resource to test complete cascade deletion.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify that all related entities were deleted in cascade.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Confirm complete cascade deletion of resource, roles, and assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with an associated scope and verifies cascade deletion")]
    public async Task DeleteResource_WithScope()
    {
        // Generate unique test names for resource and scope.
        var resourceName = "urn://resource-deleteresource-withscope";
        var scopeName = "scope-deleteresource-withscope";

        // Create a resource and a scope within it for the deletion test.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Delete the resource, which should cascade delete its scopes.
        // This tests the resource-scope cascade deletion behavior.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify the resource and scopes were deleted by attempting to retrieve the resource.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate that both the resource and its scopes are properly removed.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with both scope and role and verifies cascade deletion of all entities")]
    public async Task DeleteResource_WithScopeAndRole()
    {
        // Generate unique test names for resource and scope.
        var resourceName = "urn://resource-deleteresource-withscopeandrole";
        var scopeName = "scope-deleteresource-withscopeandrole";
        var roleName = "role-deleteresource-withscopeandrole";

        // Create a resource and a scope within it for the deletion test.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Delete the resource, which should cascade delete its scopes and roles.
        // This tests the resource-scope and resource-role cascade deletion behavior.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify the resource, scopes, and roles were deleted by attempting to retrieve the resource.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate that both the resource and its scopes are properly removed.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with a scope assignment and verifies complete cascade deletion")]
    public async Task DeleteResource_WithScopeAssignment()
    {
        // Generate unique test names for a complex test scenario.
        var resourceName = "urn://resource-deleteresource-withscopeassignment";
        var scopeName = "scope-deleteresource-withscopeassignment";
        var principalId = "principal-deleteresource-withscopeassignment";

        // Create a resource with scope and assignment.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        // Delete the resource, which should cascade delete roles, scopes, and assignments.
        // This tests the complete cascade deletion behavior across multiple entity types.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify all related data was deleted by checking the raw DynamoDB items.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // The items list should not contain any entries related to the deleted resource.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a resource with both scope and role assignments and verifies complete cascade deletion")]
    public async Task DeleteResource_WithScopeAssignmentAndRoleAssignment()
    {
        // Generate unique test names for a complex test scenario.
        var resourceName = "urn://resource-deleteresource-withscopeassignmentandroleassignment";
        var scopeName = "scope-deleteresource-withscopeassignmentandroleassignment";
        var roleName = "role-deleteresource-withscopeassignmentandroleassignment";
        var principalId = "principal-deleteresource-withscopeassignmentandroleassignment";

        // Create a resource with scope, role, and assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Delete the resource, which should cascade delete roles, scopes, and assignments.
        // This tests the complete cascade deletion behavior across multiple entity types.
        await _repository.DeleteResourceAsync(resourceName: resourceName);

        // Verify all related data was deleted by checking the raw DynamoDB items.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // The items list should not contain any entries related to the deleted resource.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves an existing resource and verifies the correct data is returned")]
    public async Task GetResource()
    {
        // Generate a unique test resource name.
        var resourceName = "urn://resource-getresource";

        // Create a resource to test retrieval.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Retrieve the resource by name.
        // This is the primary operation being tested.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved resource matches the expected value.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a resource with empty name and verifies ValidationException is thrown")]
    public void GetResource_EmptyName()
    {
        // Attempt to retrieve a resource with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetResourceAsync(resourceName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a resource with invalid name format and verifies ValidationException is thrown")]
    public void GetResource_InvalidName()
    {
        // Attempt to retrieve a resource with invalid format (not URI-like) should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetResourceAsync(resourceName: "invalid-resource-name"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a non-existent resource and verifies null is returned")]
    public async Task GetResource_NotExists()
    {
        // Generate a name for a resource that doesn't exist in the system.
        var resourceName = "urn://resource-getresource-notexists";

        // Attempt to retrieve a resource that doesn't exist.
        // This tests the system's handling of non-existent resources.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Should return null or an empty result without throwing exceptions.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a resource with null name and verifies ValidationException is thrown")]
    public void GetResource_NullName()
    {
        // Attempt to retrieve a resource with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetResourceAsync(resourceName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves a resource with an associated role and verifies both entities are stored correctly")]
    public async Task GetResource_WithRole()
    {
        // Generate unique test names for resource and role.
        var resourceName = "urn://resource-getresource-withrole";
        var roleName = "role-getresource-withrole";

        // Create a resource with a role for the retrieval test.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Retrieve the resource and verify its associated data.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved resource and its role are properly stored.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves a resource with an associated scope and verifies both entities are stored correctly")]
    public async Task GetResource_WithScope()
    {
        // Generate unique test names for resource and scope.
        var resourceName = "urn://resource-getresource-withscope";
        var scopeName = "scope-getresource-withscope";

        // Create a resource with a scope for the retrieval test.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Retrieve the resource and verify its associated data.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved resource and its scope are properly stored.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves a resource with both scope and role and verifies all entities are stored correctly")]
    public async Task GetResource_WithScopeAndRole()
    {
        // Generate unique test names for resource, scope, and role.
        var resourceName = "urn://resource-getresource-withscopeandrole";
        var scopeName = "scope-getresource-withscopeandrole";
        var roleName = "role-getresource-withscopeandrole";

        // Create a resource with both a role and scope for the retrieval test.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Retrieve the resource and verify its associated data.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved resource and its role and scope are properly stored.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple resources and verifies GetResources returns all resources in alphabetical order")]
    public async Task GetResources_MultipleResources()
    {
        // Generate unique test resource names to ensure test isolation.
        var resourceName1 = "urn://resource-1-getresources-multipleresources";
        var resourceName2 = "urn://resource-2-getresources-multipleresources";
        var resourceName3 = "urn://resource-3-getresources-multipleresources";

        // Create three resources in non-alphabetical order to test sorting.
        await _repository.CreateResourceAsync(resourceName: resourceName3);
        await _repository.CreateResourceAsync(resourceName: resourceName2);
        await _repository.CreateResourceAsync(resourceName: resourceName1);

        // Retrieve all resources to test the listing functionality with multiple resources.
        var o = new
        {
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Verify the resource list contains all three resources in alphabetical order.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves all resources when none exist and verifies an empty list is returned")]
    public async Task GetResources_NoResources()
    {
        // Test resource retrieval when no resources exist in the system.
        var o = new
        {
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Verify that an empty list is returned when no resources exist.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves all resources when one exists and verifies the correct list is returned")]
    public async Task GetResources_OneResource()
    {
        // Generate unique test resource name to ensure test isolation.
        var resourceName = "urn://resource-getresources-oneresource";

        // Create a single resource to test retrieval of resource lists.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Retrieve all resources to test the listing functionality.
        var o = new
        {
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Verify the resource list contains the expected resource.
        Snapshot.Match(o);
    }
}
