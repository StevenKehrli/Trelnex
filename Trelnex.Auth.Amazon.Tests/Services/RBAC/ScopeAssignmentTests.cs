using Snapshooter.NUnit;
using Trelnex.Core;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

public partial class RBACRepositoryTests
{
    [Test]
    [Description("Creates a new scope assignment and verifies it exists in the system")]
    public async Task CreateScopeAssignment()
    {
        // Generate unique test names for resource, scope, and principal.
        var (resourceName, scopeName, _, principalId) = FormatNames(nameof(CreateScopeAssignment));

        // Create prerequisites: resource and scope.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Create the scope assignment.
        // This is the primary operation being tested.
        await _repository.CreateScopeAssignmentAsync(
            resourceName: resourceName,
            scopeName: scopeName,
            principalId: principalId);

        // Verify the assignment was created correctly.
        var o = new
        {
            principalsAfter = await _repository.GetPrincipalsForScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Compare results against the expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates a scope assignment that already exists and verifies idempotent behavior")]
    public async Task CreateScopeAssignment_AlreadyExists()
    {
        // Generate unique test names for resource, scope, and principal.
        var (resourceName, scopeName, _, principalId) = FormatNames(nameof(CreateScopeAssignment_AlreadyExists));

        // Create prerequisites and initial assignment.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        // Capture state after first creation.
        var principalsAfterFirst = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName);
        var itemsAfterFirst = await GetItemsAsync();

        // Create the same assignment again to test idempotent behavior.
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        // Capture state after second creation to verify no changes.
        var o = new
        {
            principalsAfterFirst,
            itemsAfterFirst,
            principalsAfterSecond = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName),
            itemsAfterSecond = await GetItemsAsync()
        };

        // Verify that creating the same assignment twice is idempotent.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope assignment with empty resource name and verifies ValidationException is thrown")]
    public void CreateScopeAssignment_EmptyResourceName()
    {
        // Generate unique scope and principal names.
        var (_, scopeName, _, principalId) = FormatNames(nameof(CreateScopeAssignment_EmptyResourceName));

        // Attempt to create assignment with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAssignmentAsync(resourceName: string.Empty, scopeName: scopeName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope assignment with empty scope name and verifies ValidationException is thrown")]
    public async Task CreateScopeAssignment_EmptyScopeName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(CreateScopeAssignment_EmptyScopeName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create assignment with empty scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: string.Empty, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope assignment with invalid resource name format and verifies ValidationException is thrown")]
    public void CreateScopeAssignment_InvalidResourceName()
    {
        // Generate unique scope and principal names.
        var (_, scopeName, _, principalId) = FormatNames(nameof(CreateScopeAssignment_InvalidResourceName));

        // Attempt to create assignment with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAssignmentAsync(resourceName: "invalid-resource-name", scopeName: scopeName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope assignment with invalid scope name format and verifies ValidationException is thrown")]
    public async Task CreateScopeAssignment_InvalidScopeName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(CreateScopeAssignment_InvalidScopeName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create assignment with invalid scope name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: "Invalid_Scope_Name!", principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple principals assigned to the same scope and verifies they coexist properly")]
    public async Task CreateScopeAssignment_MultiplePrincipalsToSameScope()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(CreateScopeAssignment_MultiplePrincipalsToSameScope));

        var (_, _, _, principalId1) = FormatNames($"1_{nameof(CreateScopeAssignment_MultiplePrincipalsToSameScope)}");
        var (_, _, _, principalId2) = FormatNames($"2_{nameof(CreateScopeAssignment_MultiplePrincipalsToSameScope)}");
        var (_, _, _, principalId3) = FormatNames($"3_{nameof(CreateScopeAssignment_MultiplePrincipalsToSameScope)}");

        // Create prerequisites and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId3);

        // Verify all principals are assigned to the scope.
        var o = new
        {
            principals = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Verify all assignments were created successfully.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Assigns the same principal to multiple scopes and verifies they coexist properly")]
    public async Task CreateScopeAssignment_SamePrincipalToMultipleScopes()
    {
        // Generate unique test names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(CreateScopeAssignment_SamePrincipalToMultipleScopes));

        var (_, scopeName1, _, _) = FormatNames($"1_{nameof(CreateScopeAssignment_SamePrincipalToMultipleScopes)}");
        var (_, scopeName2, _, _) = FormatNames($"2_{nameof(CreateScopeAssignment_SamePrincipalToMultipleScopes)}");
        var (_, scopeName3, _, _) = FormatNames($"3_{nameof(CreateScopeAssignment_SamePrincipalToMultipleScopes)}");

        // Create prerequisites and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName3);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName1, principalId: principalId);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName2, principalId: principalId);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName3, principalId: principalId);

        // Verify the principal is assigned to all scopes.
        var o = new
        {
            principals1 = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName1),
            principals2 = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName2),
            principals3 = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName3),
            items = await GetItemsAsync()
        };

        // Verify all assignments were created successfully.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope assignment when the resource does not exist and verifies HttpStatusCodeException is thrown")]
    public void CreateScopeAssignment_NoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, scopeName, _, principalId) = FormatNames(nameof(CreateScopeAssignment_NoResource));

        // Attempt to grant a scope within a non-existent resource.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateScopeAssignmentAsync(
                resourceName: resourceName,
                scopeName: scopeName,
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
    [Description("Attempts to create a scope assignment when the scope does not exist and verifies HttpStatusCodeException is thrown")]
    public async Task CreateScopeAssignment_NoScope()
    {
        // Generate unique test names for resource and a non-existent scope.
        var (resourceName, scopeName, _, principalId) = FormatNames(nameof(CreateScopeAssignment_NoScope));

        // Create a resource but not the scope we'll try to grant.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to grant a non-existent scope to a principal.
        // This should throw an HttpStatusCodeException.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateScopeAssignmentAsync(
                resourceName: resourceName,
                scopeName: scopeName,
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
    [Description("Attempts to create a scope assignment with null resource name and verifies ValidationException is thrown")]
    public void CreateScopeAssignment_NullResourceName()
    {
        // Generate unique scope and principal names.
        var (_, scopeName, _, principalId) = FormatNames(nameof(CreateScopeAssignment_NullResourceName));

        // Attempt to create assignment with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAssignmentAsync(resourceName: null!, scopeName: scopeName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope assignment with null scope name and verifies ValidationException is thrown")]
    public async Task CreateScopeAssignment_NullScopeName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(CreateScopeAssignment_NullScopeName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create assignment with null scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: null!, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes an existing scope assignment and verifies it no longer exists")]
    public async Task DeleteScopeAssignment()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(DeleteScopeAssignment));

        // Create prerequisites: resource, scope, role, and grant the scope to a principal.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.CreateScopeAssignmentAsync(
            resourceName: resourceName,
            scopeName: scopeName,
            principalId: principalId);

        var principalIdsBefore = await _repository.GetPrincipalsForScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.DeleteScopeAssignmentAsync(
            resourceName: resourceName,
            scopeName: scopeName,
            principalId: principalId);

        // Revoke the scope from the principal.
        // This is the primary operation being tested.
        var o = new
        {
            principalIdsBefore,
            principalIdsAfter = await _repository.GetPrincipalsForScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // The principal membership after revocation should no longer include the scope.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope assignment with empty resource name and verifies ValidationException is thrown")]
    public void DeleteScopeAssignment_EmptyResourceName()
    {
        // Generate unique scope and principal names.
        var (_, scopeName, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_EmptyResourceName));

        // Attempt to delete assignment with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAssignmentAsync(resourceName: string.Empty, scopeName: scopeName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope assignment with empty scope name and verifies ValidationException is thrown")]
    public void DeleteScopeAssignment_EmptyScopeName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_EmptyScopeName));

        // Attempt to delete assignment with empty scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAssignmentAsync(resourceName: resourceName, scopeName: string.Empty, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple assignments, deletes one, and verifies the others remain unchanged")]
    public async Task DeleteScopeAssignment_FromMultipleAssignments()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(DeleteScopeAssignment_FromMultipleAssignments));

        var (_, _, _, principalId1) = FormatNames($"1_{nameof(DeleteScopeAssignment_FromMultipleAssignments)}");
        var (_, _, _, principalId2) = FormatNames($"2_{nameof(DeleteScopeAssignment_FromMultipleAssignments)}");
        var (_, _, _, principalId3) = FormatNames($"3_{nameof(DeleteScopeAssignment_FromMultipleAssignments)}");

        // Create prerequisites and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId3);

        // Delete the middle assignment.
        await _repository.DeleteScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId2);

        // Verify only the specified assignment was deleted.
        var o = new
        {
            principalsAfter = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Verify correct assignment was deleted and others remain.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope assignment with invalid resource name format and verifies ValidationException is thrown")]
    public void DeleteScopeAssignment_InvalidResourceName()
    {
        // Generate unique scope and principal names.
        var (_, scopeName, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_InvalidResourceName));

        // Attempt to delete assignment with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAssignmentAsync(resourceName: "invalid-resource-name", scopeName: scopeName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope assignment with invalid scope name format and verifies ValidationException is thrown")]
    public void DeleteScopeAssignment_InvalidScopeName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_InvalidScopeName));

        // Attempt to delete assignment with invalid scope name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAssignmentAsync(resourceName: resourceName, scopeName: "Invalid_Scope_Name!", principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope assignment when the resource does not exist and verifies idempotent behavior")]
    public async Task DeleteScopeAssignment_NoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, scopeName, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_NoResource));

        // Attempt to revoke a scope from a principal for a non-existent resource (should be idempotent).
        await _repository.DeleteScopeAssignmentAsync(
            resourceName: resourceName,
            scopeName: scopeName,
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
    [Description("Attempts to delete a scope assignment when the scope does not exist and verifies idempotent behavior")]
    public async Task DeleteScopeAssignment_NoScope()
    {
        // Generate unique test names for resource and a non-existent scope.
        var (resourceName, scopeName, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_NoScope));

        // Create a resource but not the scope we'll try to revoke.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to revoke a scope that doesn't exist from a principal (should be idempotent).
        await _repository.DeleteScopeAssignmentAsync(
            resourceName: resourceName,
            scopeName: scopeName,
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
    [Description("Attempts to delete a non-existent scope assignment and verifies idempotent behavior")]
    public async Task DeleteScopeAssignment_NotExists()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_NotExists));

        // Create prerequisites but not the assignment.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Attempt to delete non-existent assignment (should be idempotent).
        await _repository.DeleteScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        // Verify no changes occurred beyond the resource and scope creation.
        var o = new
        {
            principalsAfter = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Should complete without error and make no changes to assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope assignment with null resource name and verifies ValidationException is thrown")]
    public void DeleteScopeAssignment_NullResourceName()
    {
        // Generate unique scope and principal names.
        var (_, scopeName, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_NullResourceName));

        // Attempt to delete assignment with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAssignmentAsync(resourceName: null!, scopeName: scopeName, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope assignment with null scope name and verifies ValidationException is thrown")]
    public void DeleteScopeAssignment_NullScopeName()
    {
        // Generate unique resource and principal names.
        var (resourceName, _, _, principalId) = FormatNames(nameof(DeleteScopeAssignment_NullScopeName));

        // Attempt to delete assignment with null scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAssignmentAsync(resourceName: resourceName, scopeName: null!, principalId: principalId));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves principals for an existing scope and verifies the correct data is returned")]
    public async Task GetPrincipalsForScope()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, principalId) = FormatNames(nameof(GetPrincipalsForScope));

        // Create prerequisites and assignment.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId);

        // Retrieve the principals for the scope.
        var o = new
        {
            principals = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved principals match expected values.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with empty resource name and verifies ValidationException is thrown")]
    public void GetPrincipalsForScope_EmptyResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(GetPrincipalsForScope_EmptyResourceName));

        // Attempt to retrieve principals with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForScopeAsync(resourceName: string.Empty, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with empty scope name and verifies ValidationException is thrown")]
    public void GetPrincipalsForScope_EmptyScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetPrincipalsForScope_EmptyScopeName));

        // Attempt to retrieve principals with empty scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with invalid resource name format and verifies ValidationException is thrown")]
    public void GetPrincipalsForScope_InvalidResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(GetPrincipalsForScope_InvalidResourceName));

        // Attempt to retrieve principals with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForScopeAsync(resourceName: "invalid-resource-name", scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with invalid scope name format and verifies ValidationException is thrown")]
    public void GetPrincipalsForScope_InvalidScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetPrincipalsForScope_InvalidScopeName));

        // Attempt to retrieve principals with invalid scope name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: "Invalid_Scope_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves principals for a scope with multiple assignments and verifies all principals are returned")]
    public async Task GetPrincipalsForScope_MultiplePrincipals()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(GetPrincipalsForScope_MultiplePrincipals));

        var (_, _, _, principalId1) = FormatNames($"1_{nameof(GetPrincipalsForScope_MultiplePrincipals)}");
        var (_, _, _, principalId2) = FormatNames($"2_{nameof(GetPrincipalsForScope_MultiplePrincipals)}");
        var (_, _, _, principalId3) = FormatNames($"3_{nameof(GetPrincipalsForScope_MultiplePrincipals)}");

        // Create prerequisites and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId3);

        // Retrieve all principals for the scope.
        var o = new
        {
            principals = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Verify all principals are returned in the correct order.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals when scope has no assignments and verifies empty array is returned")]
    public async Task GetPrincipalsForScope_NoPrincipals()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(GetPrincipalsForScope_NoPrincipals));

        // Create prerequisites but no assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Retrieve principals for the scope with no assignments.
        var o = new
        {
            principals = await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Should return empty array without throwing exceptions.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals for a non-existent resource and verifies HttpStatusCodeException is thrown")]
    public void GetPrincipalsForScope_NoResource()
    {
        // Generate unique test names for resource and scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(GetPrincipalsForScope_NoResource));

        // Attempt to retrieve principals from a resource that doesn't exist.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GetPrincipalsForScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName);
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
    [Description("Attempts to retrieve principals for a non-existent scope and verifies HttpStatusCodeException is thrown")]
    public async Task GetPrincipalsForScope_NoScope()
    {
        // Generate unique test names for resource and a non-existent scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(GetPrincipalsForScope_NoScope));

        // Create a resource but not the scope we'll try to query.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to retrieve principals for a scope that doesn't exist.
        // This should throw an HttpStatusCodeException.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GetPrincipalsForScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName);
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
    public void GetPrincipalsForScope_NullResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(GetPrincipalsForScope_NullResourceName));

        // Attempt to retrieve principals with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForScopeAsync(resourceName: null!, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve principals with null scope name and verifies ValidationException is thrown")]
    public void GetPrincipalsForScope_NullScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetPrincipalsForScope_NullScopeName));

        // Attempt to retrieve principals with null scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetPrincipalsForScopeAsync(resourceName: resourceName, scopeName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }
}
