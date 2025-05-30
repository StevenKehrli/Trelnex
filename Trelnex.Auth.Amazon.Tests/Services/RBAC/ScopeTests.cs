using Snapshooter.NUnit;
using Trelnex.Core;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

public partial class RBACRepositoryTests
{
    [Test]
    [Description("Creates a new scope within an existing resource and verifies it exists in the system")]
    public async Task CreateScope()
    {
        // Generate unique test names for the resource and scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(CreateScope));

        // First create a resource as a prerequisite for the scope creation.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Create a new scope within the resource.
        // This is the primary operation being tested.
        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        // Verify the scope was created correctly by retrieving it and checking DynamoDB state.
        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Compare results against the expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates a scope that already exists and verifies idempotent behavior")]
    public async Task CreateScope_AlreadyExists()
    {
        // Generate unique test names for the resource and scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(CreateScope_AlreadyExists));

        // Create a resource and scope for the idempotent test.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Capture state after first creation.
        var scopeAfterFirst = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName);
        var itemsAfterFirst = await GetItemsAsync();

        // Create the same scope again to test idempotent behavior.
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Capture state after second creation to verify no changes.
        var o = new
        {
            scopeAfterFirst,
            itemsAfterFirst,
            scopeAfterSecond = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName),
            itemsAfterSecond = await GetItemsAsync()
        };

        // Verify that creating the same scope twice is idempotent.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope with empty resource name and verifies ValidationException is thrown")]
    public void CreateScope_EmptyResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(CreateScope_EmptyResourceName));

        // Attempt to create scope with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAsync(resourceName: string.Empty, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope with empty scope name and verifies ValidationException is thrown")]
    public async Task CreateScope_EmptyScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(CreateScope_EmptyScopeName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create scope with empty scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope with invalid resource name format and verifies ValidationException is thrown")]
    public void CreateScope_InvalidResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(CreateScope_InvalidResourceName));

        // Attempt to create scope with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAsync(resourceName: "invalid-resource-name", scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope with invalid scope name format and verifies ValidationException is thrown")]
    public async Task CreateScope_InvalidScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(CreateScope_InvalidScopeName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create scope with invalid scope name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: "Invalid_Scope_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple scopes in the same resource and verifies they coexist properly")]
    public async Task CreateScope_MultipleScopesInResource()
    {
        // Generate unique test names.
        var (resourceName, _, _, _) = FormatNames(nameof(CreateScope_MultipleScopesInResource));

        var (_, scopeName1, _, _) = FormatNames($"1_{nameof(CreateScope_MultipleScopesInResource)}");
        var (_, scopeName2, _, _) = FormatNames($"2_{nameof(CreateScope_MultipleScopesInResource)}");
        var (_, scopeName3, _, _) = FormatNames($"3_{nameof(CreateScope_MultipleScopesInResource)}");

        // Create resource and multiple scopes.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName3);

        // Verify all scopes exist.
        var o = new
        {
            scope1 = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName1),
            scope2 = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName2),
            scope3 = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName3),
            items = await GetItemsAsync()
        };

        // Verify all scopes were created successfully.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope when the resource does not exist and verifies HttpStatusCodeException is thrown")]
    public void CreateScope_NoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(CreateScope_NoResource));

        // Attempt to create a scope for a resource that doesn't exist.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName);
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
    [Description("Attempts to create a scope with null resource name and verifies ValidationException is thrown")]
    public void CreateScope_NullResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(CreateScope_NullResourceName));

        // Attempt to create scope with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAsync(resourceName: null!, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to create a scope with null scope name and verifies ValidationException is thrown")]
    public async Task CreateScope_NullScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(CreateScope_NullScopeName));

        // Create resource first.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to create scope with null scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes an existing scope and verifies it no longer exists")]
    public async Task DeleteScope()
    {
        // Generate unique test names for resource and scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(DeleteScope));

        // Create a resource and a scope within it for the deletion test.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        // Delete the scope within the resource.
        // This is the primary operation being tested.
        await _repository.DeleteScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        // Verify the scope was deleted by attempting to retrieve it.
        // Should return null or an empty result.
        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Validate the scope is properly removed while the resource remains.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope with empty resource name and verifies ValidationException is thrown")]
    public void DeleteScope_EmptyResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(DeleteScope_EmptyResourceName));

        // Attempt to delete scope with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAsync(resourceName: string.Empty, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope with empty scope name and verifies ValidationException is thrown")]
    public void DeleteScope_EmptyScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(DeleteScope_EmptyScopeName));

        // Attempt to delete scope with empty scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAsync(resourceName: resourceName, scopeName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Creates multiple scopes, deletes one, and verifies the others remain unchanged")]
    public async Task DeleteScope_FromMultipleScopes()
    {
        // Generate unique test names.
        var (resourceName, _, _, _) = FormatNames(nameof(DeleteScope_FromMultipleScopes));

        var (_, scopeName1, _, _) = FormatNames($"1_{nameof(DeleteScope_FromMultipleScopes)}");
        var (_, scopeName2, _, _) = FormatNames($"2_{nameof(DeleteScope_FromMultipleScopes)}");
        var (_, scopeName3, _, _) = FormatNames($"3_{nameof(DeleteScope_FromMultipleScopes)}");

        // Create resource and multiple scopes.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName1);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName2);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName3);

        // Delete the middle scope.
        await _repository.DeleteScopeAsync(resourceName: resourceName, scopeName: scopeName2);

        // Verify only the specified scope was deleted.
        var o = new
        {
            scope1After = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName1),
            scope2After = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName2),
            scope3After = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName3),
            items = await GetItemsAsync()
        };

        // Verify correct scope was deleted and others remain.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope with invalid resource name format and verifies ValidationException is thrown")]
    public void DeleteScope_InvalidResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(DeleteScope_InvalidResourceName));

        // Attempt to delete scope with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAsync(resourceName: "invalid-resource-name", scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope with invalid scope name format and verifies ValidationException is thrown")]
    public void DeleteScope_InvalidScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(DeleteScope_InvalidScopeName));

        // Attempt to delete scope with invalid scope name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAsync(resourceName: resourceName, scopeName: "Invalid_Scope_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope when the resource does not exist and verifies idempotent behavior")]
    public async Task DeleteScope_NoResource()
    {
        // Generate unique test names for non-existent resource.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(DeleteScope_NoResource));

        // Attempt to delete scope from non-existent resource (should be idempotent).
        await _repository.DeleteScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Verify no changes occurred.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // Should complete without error and make no changes.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a non-existent scope and verifies idempotent behavior")]
    public async Task DeleteScope_NotExists()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(DeleteScope_NotExists));

        // Create resource but not the scope.
        await _repository.CreateResourceAsync(resourceName: resourceName);

        // Attempt to delete non-existent scope (should be idempotent).
        await _repository.DeleteScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Verify no changes occurred beyond the resource creation.
        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Should complete without error and make no changes to the scope.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope with null resource name and verifies ValidationException is thrown")]
    public void DeleteScope_NullResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(DeleteScope_NullResourceName));

        // Attempt to delete scope with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAsync(resourceName: null!, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to delete a scope with null scope name and verifies ValidationException is thrown")]
    public void DeleteScope_NullScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(DeleteScope_NullScopeName));

        // Attempt to delete scope with null scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.DeleteScopeAsync(resourceName: resourceName, scopeName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Deletes a scope with multiple assignments and verifies all assignments are cascade deleted")]
    public async Task DeleteScope_WithMultipleScopeAssignments()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(DeleteScope_WithMultipleScopeAssignments));

        var (_, _, _, principalId1) = FormatNames($"1_{nameof(DeleteScope_WithMultipleScopeAssignments)}");
        var (_, _, _, principalId2) = FormatNames($"2_{nameof(DeleteScope_WithMultipleScopeAssignments)}");
        var (_, _, _, principalId3) = FormatNames($"3_{nameof(DeleteScope_WithMultipleScopeAssignments)}");

        // Create resource, scope, and multiple assignments.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId1);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId2);
        await _repository.CreateScopeAssignmentAsync(resourceName: resourceName, scopeName: scopeName, principalId: principalId3);

        // Delete the scope, which should cascade delete all assignments.
        await _repository.DeleteScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Verify all assignments were removed via GetItemsAsync results.
        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Confirm scope and all assignments are deleted.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Retrieves an existing scope and verifies the correct data is returned")]
    public async Task GetScope()
    {
        // Generate unique test names.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(GetScope));

        // Create resource and scope for retrieval test.
        await _repository.CreateResourceAsync(resourceName: resourceName);
        await _repository.CreateScopeAsync(resourceName: resourceName, scopeName: scopeName);

        // Retrieve the scope.
        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(resourceName: resourceName, scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved scope matches expected values.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a scope with empty resource name and verifies ValidationException is thrown")]
    public void GetScope_EmptyResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(GetScope_EmptyResourceName));

        // Attempt to retrieve scope with empty resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetScopeAsync(resourceName: string.Empty, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a scope with empty scope name and verifies ValidationException is thrown")]
    public void GetScope_EmptyScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetScope_EmptyScopeName));

        // Attempt to retrieve scope with empty scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetScopeAsync(resourceName: resourceName, scopeName: string.Empty));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a scope with invalid resource name format and verifies ValidationException is thrown")]
    public void GetScope_InvalidResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(GetScope_InvalidResourceName));

        // Attempt to retrieve scope with invalid resource name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetScopeAsync(resourceName: "invalid-resource-name", scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a scope with invalid scope name format and verifies ValidationException is thrown")]
    public void GetScope_InvalidScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetScope_InvalidScopeName));

        // Attempt to retrieve scope with invalid scope name format should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetScopeAsync(resourceName: resourceName, scopeName: "Invalid_Scope_Name!"));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a scope for a non-existent resource and verifies HttpStatusCodeException is thrown")]
    public void GetScope_NoResource()
    {
        // Generate unique test names for resource and a non-existent scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(GetScope_NoResource));

        // Attempt to retrieve a scope from a resource that doesn't exist.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GetScopeAsync(
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
    [Description("Attempts to retrieve a non-existent scope and verifies null is returned")]
    public async Task GetScope_NotExists()
    {
        // Generate unique test names for resource and a non-existent scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(GetScope_NotExists));

        // Create a resource but not the scope we'll try to retrieve.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to retrieve a scope that doesn't exist within an existing resource.
        // This tests the system's handling of non-existent scopes.
        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Should return null or an empty result without throwing exceptions.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a scope with null resource name and verifies ValidationException is thrown")]
    public void GetScope_NullResourceName()
    {
        // Generate unique scope name.
        var (_, scopeName, _, _) = FormatNames(nameof(GetScope_NullResourceName));

        // Attempt to retrieve scope with null resource name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetScopeAsync(resourceName: null!, scopeName: scopeName));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    [Description("Attempts to retrieve a scope with null scope name and verifies ValidationException is thrown")]
    public void GetScope_NullScopeName()
    {
        // Generate unique resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(GetScope_NullScopeName));

        // Attempt to retrieve scope with null scope name should throw ValidationException.
        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
            await _repository.GetScopeAsync(resourceName: resourceName, scopeName: null!));

        // Verify the correct validation error details.
        var o = new
        {
            message = exception.Message
        };

        Snapshot.Match(o);
    }
}
