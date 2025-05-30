using Snapshooter.NUnit;
using Trelnex.Core;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

public partial class RBACRepositoryTests
{
    [Test]
    [Description("Creates a new role within an existing resource and verifies it exists in the system")]
    public async Task CreateRole()
    {
        // Generate unique test names for the resource and role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(CreateRole));

        // First create a resource as a prerequisite for the role creation.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Create a new role within the resource.
        // This is the primary operation being tested.
        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Verify the role was created correctly by retrieving it and checking DynamoDB state.
        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        // Compare results against the expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates a role that already exists and verifies idempotent behavior")]
    public async Task CreateRole_AlreadyExists()
    {
        // Generate unique test names for the resource and role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(CreateRole_AlreadyExists));

        // Create a resource and role for the idempotent test.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Capture state after first creation.
        var roleAfterFirst = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName);
        var itemsAfterFirst = await GetItemsAsync();

        // Create the same role again to test idempotent behavior.
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Capture state after second creation to verify no changes.
        var o = new
        {
            roleAfterFirst,
            itemsAfterFirst,
            roleAfterSecond = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName),
            itemsAfterSecond = await GetItemsAsync()
        };

        // Verify that creating the same role twice is idempotent.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role with empty resource name and verifies ValidationException is thrown")]
    public void CreateRole_EmptyResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(CreateRole_EmptyResourceName));

        // Attempt to create role with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAsync(resourceName: string.Empty, roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role with empty role name and verifies ValidationException is thrown")]
    public async Task CreateRole_EmptyRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(CreateRole_EmptyRoleName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create role with empty role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAsync(resourceName: resourceName, roleName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role with invalid resource name format and verifies ValidationException is thrown")]
    public void CreateRole_InvalidResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(CreateRole_InvalidResourceName));

        // Attempt to create role with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAsync(resourceName: "invalid-resource-name", roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role with invalid role name format and verifies ValidationException is thrown")]
    public async Task CreateRole_InvalidRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(CreateRole_InvalidRoleName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create role with invalid role name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAsync(resourceName: resourceName, roleName: "Invalid_Role_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple roles in the same resource and verifies they coexist properly")]
    public async Task CreateRole_MultipleRolesInResource()
    {
        // Generate unique test names.
        var (resourceName, _, _, _) = FormatNames(nameof(CreateRole_MultipleRolesInResource));

        var (_, _, roleName1, _) = FormatNames($"1_{nameof(CreateRole_MultipleRolesInResource)}");
        var (_, _, roleName2, _) = FormatNames($"2_{nameof(CreateRole_MultipleRolesInResource)}");
        var (_, _, roleName3, _) = FormatNames($"3_{nameof(CreateRole_MultipleRolesInResource)}");

        // Create resource and multiple roles.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName3);

        // Verify all roles exist.
        var o = new
        {
            role1 = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName1),
            role2 = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName2),
            role3 = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName3),
            items = await GetItemsAsync()
        };

        // Verify all roles were created successfully.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role when the resource does not exist and verifies HttpStatusCodeException is thrown")]
    public void CreateRole_NoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, _, roleName, _) = FormatNames(nameof(CreateRole_NoResource));

        // Attempt to create a role for a resource that doesn't exist.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateRoleAsync(
                resourceName: resourceName,
                roleName: roleName);
        });

        // Verify the exception details match expected values.
        // We capture the HTTP status code and message for snapshot comparison.
        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        // Validate the exception details against the expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role with null resource name and verifies ValidationException is thrown")]
    public void CreateRole_NullResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(CreateRole_NullResourceName));

        // Attempt to create role with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAsync(resourceName: null!, roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role with null role name and verifies ValidationException is thrown")]
    public async Task CreateRole_NullRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(CreateRole_NullRoleName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create role with null role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAsync(resourceName: resourceName, roleName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes an existing role and verifies it no longer exists")]
    public async Task DeleteRole()
    {
        // Generate unique test names for resource and role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(DeleteRole));

        // Create a resource and a role within it for the deletion test.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Delete the role within the resource.
        // This is the primary operation being tested.
        await _repository.DeleteRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Verify the role was deleted by attempting to retrieve it.
        // Should return null or an empty result.
        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        // Validate the role is properly removed while the resource remains.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role with empty resource name and verifies ValidationException is thrown")]
    public void DeleteRole_EmptyResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(DeleteRole_EmptyResourceName));

        // Attempt to delete role with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAsync(resourceName: string.Empty, roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role with empty role name and verifies ValidationException is thrown")]
    public void DeleteRole_EmptyRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(DeleteRole_EmptyRoleName));

        // Attempt to delete role with empty role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAsync(resourceName: resourceName, roleName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple roles, deletes one, and verifies the others remain unchanged")]
    public async Task DeleteRole_FromMultipleRoles()
    {
        // Generate unique test names.
        var (resourceName, _, _, _) = FormatNames(nameof(DeleteRole_FromMultipleRoles));

        var (_, _, roleName1, _) = FormatNames($"1_{nameof(CreateRole_MultipleRolesInResource)}");
        var (_, _, roleName2, _) = FormatNames($"2_{nameof(CreateRole_MultipleRolesInResource)}");
        var (_, _, roleName3, _) = FormatNames($"3_{nameof(CreateRole_MultipleRolesInResource)}");

        // Create resource and multiple roles.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName3);

        // Delete the middle role.
        await _repository.DeleteRoleAsync(resourceName: resourceName, roleName: roleName2);

        // Verify only the specified role was deleted.
        var o = new
        {
            role1After = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName1),
            role2After = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName2),
            role3After = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName3),
            items = await GetItemsAsync()
        };

        // Verify correct role was deleted and others remain.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role with invalid resource name format and verifies ValidationException is thrown")]
    public void DeleteRole_InvalidResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(DeleteRole_InvalidResourceName));

        // Attempt to delete role with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAsync(resourceName: "invalid-resource-name", roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role with invalid role name format and verifies ValidationException is thrown")]
    public void DeleteRole_InvalidRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(DeleteRole_InvalidRoleName));

        // Attempt to delete role with invalid role name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAsync(resourceName: resourceName, roleName: "Invalid_Role_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role when the resource does not exist and verifies idempotent behavior")]
    public async Task DeleteRole_NoResource()
    {
        // Generate unique test names for non-existent resource.
        var (resourceName, _, roleName, _) = FormatNames(nameof(DeleteRole_NoResource));

        // Attempt to delete role from non-existent resource (should be idempotent).
        await _repository.DeleteRoleAsync(resourceName: resourceName, roleName: roleName);

        // Verify no changes occurred.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Should complete without error and make no changes.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a non-existent role and verifies idempotent behavior")]
    public async Task DeleteRole_NotExists()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, _) = FormatNames(nameof(DeleteRole_NotExists));

        // Create resource but not the role.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to delete non-existent role (should be idempotent).
        await _repository.DeleteRoleAsync(resourceName: resourceName, roleName: roleName);

        // Verify no changes occurred beyond the resource creation.
        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Should complete without error and make no changes to the role.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role with null resource name and verifies ValidationException is thrown")]
    public void DeleteRole_NullResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(DeleteRole_NullResourceName));

        // Attempt to delete role with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAsync(resourceName: null!, roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role with null role name and verifies ValidationException is thrown")]
    public void DeleteRole_NullRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(DeleteRole_NullRoleName));

        // Attempt to delete role with null role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAsync(resourceName: resourceName, roleName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a role with multiple assignments and verifies all assignments are cascade deleted")]
    public async Task DeleteRole_WithMultipleRoleAssignments()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, _) = FormatNames(nameof(DeleteRole_WithMultipleRoleAssignments));

        var (_, _, _, principalId1) = FormatNames($"1_{nameof(CreateRole_MultipleRolesInResource)}");
        var (_, _, _, principalId2) = FormatNames($"2_{nameof(CreateRole_MultipleRolesInResource)}");
        var (_, _, _, principalId3) = FormatNames($"3_{nameof(CreateRole_MultipleRolesInResource)}");

        // Create resource, role, and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId3);

        // Delete the role, which should cascade delete all assignments.
        await _repository.DeleteRoleAsync(resourceName: resourceName, roleName: roleName);

        // Verify all assignments were removed via GetItemsAsync results.
        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Confirm role and all assignments are deleted.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a role with an assignment and verifies cascade deletion of the assignment")]
    public async Task DeleteRole_WithRoleAssignment()
    {
        // Generate unique test names for a complex test scenario.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(DeleteRole_WithRoleAssignment));

        // Create a resource with role and assignment.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.CreateRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Delete the role, which should cascade delete its role assignments.
        // This tests the role-assignment cascade deletion behavior.
        await _repository.DeleteRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Verify the principal access no longer references the deleted role.
        var o = new
        {
            principalAccessAfter = await _repository.GetPrincipalAccessAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // The principal access should no longer contain the deleted role.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves an existing role and verifies the correct data is returned")]
    public async Task GetRole()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, _) = FormatNames(nameof(GetRole));

        // Create resource and role for retrieval test.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Retrieve the role.
        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved role matches expected values.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a role with empty resource name and verifies ValidationException is thrown")]
    public void GetRole_EmptyResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(GetRole_EmptyResourceName));

        // Attempt to retrieve role with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetRoleAsync(resourceName: string.Empty, roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a role with empty role name and verifies ValidationException is thrown")]
    public void GetRole_EmptyRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetRole_EmptyRoleName));

        // Attempt to retrieve role with empty role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetRoleAsync(resourceName: resourceName, roleName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a role with invalid resource name format and verifies ValidationException is thrown")]
    public void GetRole_InvalidResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(GetRole_InvalidResourceName));

        // Attempt to retrieve role with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetRoleAsync(resourceName: "invalid-resource-name", roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a role with invalid role name format and verifies ValidationException is thrown")]
    public void GetRole_InvalidRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetRole_InvalidRoleName));

        // Attempt to retrieve role with invalid role name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetRoleAsync(resourceName: resourceName, roleName: "Invalid_Role_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a role for a non-existent resource and verifies HttpStatusCodeException is thrown")]
    public void GetRole_NoResource()
    {
        // Generate unique test names for resource and a non-existent role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(GetRole_NoResource));

        // Attempt to retrieve a role from a resource that doesn't exist.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GetRoleAsync(
                resourceName: resourceName,
                roleName: roleName);
        });

        // Verify the exception details match expected values.
        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        // Validate the exception details against the expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a non-existent role and verifies null is returned")]
    public async Task GetRole_NotExists()
    {
        // Generate unique test names for resource and a non-existent role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(GetRole_NotExists));

        // Create a resource but not the role we'll try to retrieve.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to retrieve a role that doesn't exist within an existing resource.
        // This tests the system's handling of non-existent roles.
        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        // Should return null or an empty result without throwing exceptions.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a role with null resource name and verifies ValidationException is thrown")]
    public void GetRole_NullResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(GetRole_NullResourceName));

        // Attempt to retrieve role with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetRoleAsync(resourceName: null!, roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a role with null role name and verifies ValidationException is thrown")]
    public void GetRole_NullRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetRole_NullRoleName));

        // Attempt to retrieve role with null role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetRoleAsync(resourceName: resourceName, roleName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }
}