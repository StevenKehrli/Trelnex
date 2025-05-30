using Snapshooter.NUnit;
using Trelnex.Core;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

public partial class RBACRepositoryTests
{
    [Test]
    [Description("Creates a new role assignment and verifies it exists in the system")]
    public async Task CreateRoleAssignment()
    {
        // Generate unique test names for resource, role, and principal.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(CreateRoleAssignment));

        // Create prerequisites: resource and role.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Create the role assignment.
        // This is the primary operation being tested.
        await _repository.CreateRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Verify the assignment was created correctly.
        var o = new
        {
            principalsAfter = await _repository.GetPrincipalsForRoleAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        // Compare results against the expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates a role assignment that already exists and verifies idempotent behavior")]
    public async Task CreateRoleAssignment_AlreadyExists()
    {
        // Generate unique test names for resource, role, and principal.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(CreateRoleAssignment_AlreadyExists));

        // Create prerequisites and initial assignment.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Capture state after first creation.
        var principalsAfterFirst = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName);
        var itemsAfterFirst = await GetItemsAsync();

        // Create the same assignment again to test idempotent behavior.
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Capture state after second creation to verify no changes.
        var o = new
        {
            principalsAfterFirst,
            itemsAfterFirst,
            principalsAfterSecond = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName),
            itemsAfterSecond = await GetItemsAsync()
        };

        // Verify that creating the same assignment twice is idempotent.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role assignment with empty resource name and verifies ValidationException is thrown")]
    public void CreateRoleAssignment_EmptyResourceName()
    {
        // Generate unique role and principal names.
        var (_, _, roleName, principalId) = FormatNames(nameof(CreateRoleAssignment_EmptyResourceName));

        // Attempt to create assignment with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAssignmentAsync(resourceName: string.Empty, roleName: roleName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role assignment with empty role name and verifies ValidationException is thrown")]
    public async Task CreateRoleAssignment_EmptyRoleName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(CreateRoleAssignment_EmptyRoleName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create assignment with empty role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: string.Empty, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role assignment with invalid resource name format and verifies ValidationException is thrown")]
    public void CreateRoleAssignment_InvalidResourceName()
    {
        // Generate unique role and principal names.
        var (_, _, roleName, principalId) = FormatNames(nameof(CreateRoleAssignment_InvalidResourceName));

        // Attempt to create assignment with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAssignmentAsync(resourceName: "invalid-resource-name", roleName: roleName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role assignment with invalid role name format and verifies ValidationException is thrown")]
    public async Task CreateRoleAssignment_InvalidRoleName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(CreateRoleAssignment_InvalidRoleName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create assignment with invalid role name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: "Invalid_Role_Name!", principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple principals assigned to the same role and verifies they coexist properly")]
    public async Task CreateRoleAssignment_MultiplePrincipalsToSameRole()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, _) = FormatNames(nameof(CreateRoleAssignment_MultiplePrincipalsToSameRole));

        var (_, _, _, principalId1) = FormatNames($"1_{nameof(CreateRoleAssignment_MultiplePrincipalsToSameRole)}");
        var (_, _, _, principalId2) = FormatNames($"2_{nameof(CreateRoleAssignment_MultiplePrincipalsToSameRole)}");
        var (_, _, _, principalId3) = FormatNames($"3_{nameof(CreateRoleAssignment_MultiplePrincipalsToSameRole)}");

        // Create prerequisites and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId3);

        // Verify all principals are assigned to the role.
        var o = new
        {
            principals = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Verify all assignments were created successfully.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Assigns the same principal to multiple roles and verifies they coexist properly")]
    public async Task CreateRoleAssignment_SamePrincipalToMultipleRoles()
    {
        // Generate unique test names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(CreateRoleAssignment_SamePrincipalToMultipleRoles));

        var (_, _, roleName1, _) = FormatNames($"1_{nameof(CreateRoleAssignment_SamePrincipalToMultipleRoles)}");
        var (_, _, roleName2, _) = FormatNames($"2_{nameof(CreateRoleAssignment_SamePrincipalToMultipleRoles)}");
        var (_, _, roleName3, _) = FormatNames($"3_{nameof(CreateRoleAssignment_SamePrincipalToMultipleRoles)}");

        // Create prerequisites and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName1);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName2);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName3);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName1, principalId: principalId);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName2, principalId: principalId);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName3, principalId: principalId);

        // Verify the principal is assigned to all roles.
        var o = new
        {
            principals1 = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName1),
            principals2 = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName2),
            principals3 = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName3),
            items = await GetItemsAsync()
        };

        // Verify all assignments were created successfully.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role assignment when the resource does not exist and verifies HttpStatusCodeException is thrown")]
    public void CreateRoleAssignment_NoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(CreateRoleAssignment_NoResource));

        // Attempt to grant a role within a non-existent resource.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateRoleAssignmentAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId);
        });

        // Verify the exception details match expected values.
        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        // Validate error handling behaves as expected.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role assignment when the role does not exist and verifies HttpStatusCodeException is thrown")]
    public async Task CreateRoleAssignment_NoRole()
    {
        // Generate unique test names for resource and a non-existent role.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(CreateRoleAssignment_NoRole));

        // Create a resource but not the role we'll try to grant.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to grant a non-existent role to a principal.
        // This should throw an HttpStatusCodeException.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateRoleAssignmentAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId);
        });

        // Verify the exception details match expected values.
        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        // Validate error handling behaves as expected.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role assignment with null resource name and verifies ValidationException is thrown")]
    public void CreateRoleAssignment_NullResourceName()
    {
        // Generate unique role and principal names.
        var (_, _, roleName, principalId) = FormatNames(nameof(CreateRoleAssignment_NullResourceName));

        // Attempt to create assignment with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAssignmentAsync(resourceName: null!, roleName: roleName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a role assignment with null role name and verifies ValidationException is thrown")]
    public async Task CreateRoleAssignment_NullRoleName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(CreateRoleAssignment_NullRoleName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create assignment with null role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: null!, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes an existing role assignment and verifies it no longer exists")]
    public async Task DeleteRoleAssignment()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(DeleteRoleAssignment));

        // Create prerequisites: resource, scope, role, and grant the role to a principal.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.CreateRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        var principalIdsBefore = await _repository.GetPrincipalsForRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.DeleteRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Revoke the role from the principal.
        // This is the primary operation being tested.
        var o = new
        {
            principalIdsBefore,
            principalIdsAfter = await _repository.GetPrincipalsForRoleAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        // The principal membership after revocation should no longer include the role.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role assignment with empty resource name and verifies ValidationException is thrown")]
    public void DeleteRoleAssignment_EmptyResourceName()
    {
        // Generate unique role and principal names.
        var (_, _, roleName, principalId) = FormatNames(nameof(DeleteRoleAssignment_EmptyResourceName));

        // Attempt to delete assignment with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAssignmentAsync(resourceName: string.Empty, roleName: roleName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role assignment with empty role name and verifies ValidationException is thrown")]
    public void DeleteRoleAssignment_EmptyRoleName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(DeleteRoleAssignment_EmptyRoleName));

        // Attempt to delete assignment with empty role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAssignmentAsync(resourceName: resourceName, roleName: string.Empty, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple assignments, deletes one, and verifies the others remain unchanged")]
    public async Task DeleteRoleAssignment_FromMultipleAssignments()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, _) = FormatNames(nameof(DeleteRoleAssignment_FromMultipleAssignments));

        var (_, _, _, principalId1) = FormatNames($"1_{nameof(DeleteRoleAssignment_FromMultipleAssignments)}");
        var (_, _, _, principalId2) = FormatNames($"2_{nameof(DeleteRoleAssignment_FromMultipleAssignments)}");
        var (_, _, _, principalId3) = FormatNames($"3_{nameof(DeleteRoleAssignment_FromMultipleAssignments)}");

        // Create prerequisites and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId3);

        // Delete the middle assignment.
        await _repository.DeleteRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId2);

        // Verify only the specified assignment was deleted.
        var o = new
        {
            principalsAfter = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Verify correct assignment was deleted and others remain.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role assignment with invalid resource name format and verifies ValidationException is thrown")]
    public void DeleteRoleAssignment_InvalidResourceName()
    {
        // Generate unique role and principal names.
        var (_, _, roleName, principalId) = FormatNames(nameof(DeleteRoleAssignment_InvalidResourceName));

        // Attempt to delete assignment with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAssignmentAsync(resourceName: "invalid-resource-name", roleName: roleName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role assignment with invalid role name format and verifies ValidationException is thrown")]
    public void DeleteRoleAssignment_InvalidRoleName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(DeleteRoleAssignment_InvalidRoleName));

        // Attempt to delete assignment with invalid role name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAssignmentAsync(resourceName: resourceName, roleName: "Invalid_Role_Name!", principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role assignment when the resource does not exist and verifies idempotent behavior")]
    public async Task DeleteRoleAssignment_NoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(DeleteRoleAssignment_NoResource));

        // Attempt to revoke a role from a principal for a non-existent resource (should be idempotent).
        await _repository.DeleteRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Verify no changes occurred.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Should complete without error and make no changes.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role assignment when the role does not exist and verifies idempotent behavior")]
    public async Task DeleteRoleAssignment_NoRole()
    {
        // Generate unique test names for resource and a non-existent role.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(DeleteRoleAssignment_NoRole));

        // Create a resource but not the role we'll try to revoke.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to revoke a role that doesn't exist from a principal (should be idempotent).
        await _repository.DeleteRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Verify no changes occurred beyond the resource creation.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Should complete without error and make no changes to assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a non-existent role assignment and verifies idempotent behavior")]
    public async Task DeleteRoleAssignment_NotExists()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(DeleteRoleAssignment_NotExists));

        // Create prerequisites but not the assignment.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Attempt to delete non-existent assignment (should be idempotent).
        await _repository.DeleteRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Verify no changes occurred beyond the resource and role creation.
        var o = new
        {
            principalsAfter = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Should complete without error and make no changes to assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role assignment with null resource name and verifies ValidationException is thrown")]
    public void DeleteRoleAssignment_NullResourceName()
    {
        // Generate unique role and principal names.
        var (_, _, roleName, principalId) = FormatNames(nameof(DeleteRoleAssignment_NullResourceName));

        // Attempt to delete assignment with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAssignmentAsync(resourceName: null!, roleName: roleName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a role assignment with null role name and verifies ValidationException is thrown")]
    public void DeleteRoleAssignment_NullRoleName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(DeleteRoleAssignment_NullRoleName));

        // Attempt to delete assignment with null role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteRoleAssignmentAsync(resourceName: resourceName, roleName: null!, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves principals for an existing role and verifies the correct data is returned")]
    public async Task GetPrincipalsForRole()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(GetPrincipalsForRole));

        // Create prerequisites and assignment.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId);

        // Retrieve the principals for the role.
        var o = new
        {
            principals = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved principals match expected values.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with empty resource name and verifies ValidationException is thrown")]
    public void GetPrincipalsForRole_EmptyResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(GetPrincipalsForRole_EmptyResourceName));

        // Attempt to retrieve principals with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForRoleAsync(resourceName: string.Empty, roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with empty role name and verifies ValidationException is thrown")]
    public void GetPrincipalsForRole_EmptyRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetPrincipalsForRole_EmptyRoleName));

        // Attempt to retrieve principals with empty role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with invalid resource name format and verifies ValidationException is thrown")]
    public void GetPrincipalsForRole_InvalidResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(GetPrincipalsForRole_InvalidResourceName));

        // Attempt to retrieve principals with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForRoleAsync(resourceName: "invalid-resource-name", roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with invalid role name format and verifies ValidationException is thrown")]
    public void GetPrincipalsForRole_InvalidRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetPrincipalsForRole_InvalidRoleName));

        // Attempt to retrieve principals with invalid role name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: "Invalid_Role_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves principals for a role with multiple assignments and verifies all principals are returned")]
    public async Task GetPrincipalsForRole_MultiplePrincipals()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, _) = FormatNames(nameof(GetPrincipalsForRole_MultiplePrincipals));

        var (_, _, _, principalId1) = FormatNames($"1_{nameof(GetPrincipalsForRole_MultiplePrincipals)}");
        var (_, _, _, principalId2) = FormatNames($"2_{nameof(GetPrincipalsForRole_MultiplePrincipals)}");
        var (_, _, _, principalId3) = FormatNames($"3_{nameof(GetPrincipalsForRole_MultiplePrincipals)}");

        // Create prerequisites and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId1);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId2);
        await _repository.CreateRoleAssignmentAsync(resourceName: resourceName, roleName: roleName, principalId: principalId3);

        // Retrieve all principals for the role.
        var o = new
        {
            principals = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Verify all principals are returned in the correct order.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals when role has no assignments and verifies empty array is returned")]
    public async Task GetPrincipalsForRole_NoPrincipals()
    {
        // Generate unique test names.
        var (resourceName, _, roleName, _) = FormatNames(nameof(GetPrincipalsForRole_NoPrincipals));

        // Create prerequisites but no assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateRoleAsync(resourceName: resourceName, roleName: roleName);

        // Retrieve principals for the role with no assignments.
        var o = new
        {
            principals = await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: roleName),
            items = await GetItemsAsync()
        };

        // Should return empty array without throwing exceptions.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals for a non-existent resource and verifies HttpStatusCodeException is thrown")]
    public void GetPrincipalsForRole_NoResource()
    {
        // Generate unique test names for resource and role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(GetPrincipalsForRole_NoResource));

        // Attempt to retrieve principals from a resource that doesn't exist.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GetPrincipalsForRoleAsync(
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
    [Description("Attempts to retrieve principals for a non-existent role and verifies HttpStatusCodeException is thrown")]
    public async Task GetPrincipalsForRole_NoRole()
    {
        // Generate unique test names for resource and a non-existent role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(GetPrincipalsForRole_NoRole));

        // Create a resource but not the role we'll try to query.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to retrieve principals for a role that doesn't exist.
        // This should throw an HttpStatusCodeException.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GetPrincipalsForRoleAsync(
                resourceName: resourceName,
                roleName: roleName);
        });

        // Verify the exception details match expected values.
        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        // Validate error handling behaves as expected.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with null resource name and verifies ValidationException is thrown")]
    public void GetPrincipalsForRole_NullResourceName()
    {
        // Generate unique role name.
        var (_, _, roleName, _) = FormatNames(nameof(GetPrincipalsForRole_NullResourceName));

        // Attempt to retrieve principals with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForRoleAsync(resourceName: null!, roleName: roleName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with null role name and verifies ValidationException is thrown")]
    public void GetPrincipalsForRole_NullRoleName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetPrincipalsForRole_NullRoleName));

        // Attempt to retrieve principals with null role name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForRoleAsync(resourceName: resourceName, roleName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }
}
