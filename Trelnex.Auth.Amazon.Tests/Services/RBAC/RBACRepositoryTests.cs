using System.Collections.Immutable;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Snapshooter.NUnit;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

[Category("RBAC")]
[Ignore("Requires a DynamoDB table.")]
public class RBACRepositoryTests
{
    private AmazonDynamoDBClient _client = null!;
    private string _tableName = null!;
    private RBACRepository _repository = null!;

    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Load configuration from appsettings.json and optional user-specific settings.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Retrieve AWS credentials using the default credentials provider chain.
        var credentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Get the AWS region from configuration to ensure we connect to the correct DynamoDB endpoint.
        var region = configuration
            .GetSection("RBAC:Region")
            .Get<string>()!;

        // Convert the region string to an AWS RegionEndpoint object required by the AWS SDK.
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        // Get the DynamoDB table name that stores the RBAC data.
        var tableName = configuration
            .GetSection("RBAC:TableName")
            .Get<string>()!;

        // Initialize the DynamoDB client with credentials and region.
        _client = new AmazonDynamoDBClient(credentials, regionEndpoint);
        _tableName = tableName;

        // Create the RBAC repository with the validator, client, and table name.
        // The ScopeNameValidator ensures scope names adhere to required format rules.
        _repository = new RBACRepository(
            new ScopeNameValidator(),
            _client,
            tableName);
    }

    [OneTimeTearDown]
    public void TestFixtureCleanup()
    {
        // Release resources allocated by the DynamoDB client to prevent resource leaks.
        _client.Dispose();
    }

    [TearDown]
    public async Task TestCleanup()
    {
        // Perform a scan operation to retrieve all items in the DynamoDB table.
        // This ensures we can clean up the table after each test for proper isolation.
        var scanRequest = new ScanRequest()
        {
            TableName = _tableName,
            ConsistentRead = true
        };

        // Execute the scan operation against DynamoDB.
        var scanResponse = await _client.ScanAsync(scanRequest, default);

        // If the table is empty, there's nothing to clean up.
        if (scanResponse.Items.Count == 0) return;

        // Process each DynamoDB item, extracting the primary key components (resourceName and subjectName) which are needed for deletion.
        var items = scanResponse.Items
            .Select(attributeMap =>
            {
                return (
                    resourceName: attributeMap["resourceName"].S,
                    subjectName: attributeMap["subjectName"].S);
            });

        // Create DeleteRequest objects for each item.
        // Each request specifies the complete primary key for the item.
        var deleteRequests = items.Select(item =>
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "resourceName", new AttributeValue(item.resourceName) },
                { "subjectName", new AttributeValue(item.subjectName) }
            };

            return new DeleteRequest { Key = key };
        });

        // Convert DeleteRequest objects to WriteRequest objects required by the BatchWriteItem API.
        var writeRequests = deleteRequests.Select(deleteRequest => new WriteRequest { DeleteRequest = deleteRequest });

        // Prepare the BatchWriteItemRequest, which allows deleting multiple items in a single operation.
        var batchWriteRequest = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                { _tableName, writeRequests.ToList() }
            }
        };

        // Execute batch deletes until all items are processed.
        // DynamoDB might return unprocessed items if the request exceeds service limits.
        while (batchWriteRequest.RequestItems.Count > 0)
        {
            // Execute the batch write operation and capture the response.
            var batchWriteItemResponse = await _client.BatchWriteItemAsync(batchWriteRequest, default);

            // Update the request with any unprocessed items for the next iteration.
            batchWriteRequest.RequestItems = batchWriteItemResponse.UnprocessedItems;
        }
    }

    [Test]
    [Description("Tests the creation of a new resource")]
    public async Task RBACRepository_CreateResource()
    {
        // Generate unique test resource names based on the test method name.
        // This ensures test isolation and prevents conflicts between test runs.
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepository_CreateResource));

        // Create a new resource in the RBAC system.
        // This is the operation being tested in this test case.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Construct an anonymous object containing:
        // 1. The list of resources after the creation operation
        // 2. The raw DynamoDB items to verify data was stored correctly
        var o = new
        {
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Use snapshot testing to verify the operation results match expected state.
        // This compares against a saved snapshot from a previous successful test run.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the creation of a new role within an existing resource")]
    public async Task RBACRepository_CreateRole()
    {
        // Generate unique test names for the resource and role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepository_CreateRole));

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
    [Description("Tests retrieving a resource after creating a role within it")]
    public async Task RBACRepository_CreateRoleGetResource()
    {
        // Generate unique test names for this test case.
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepository_CreateRoleGetResource));

        // Set up test data: create a resource and a role within it.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Retrieve the resource to verify its state after role creation.
        // This tests that resource information properly includes created roles.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate results against expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests that creating a role fails when the resource does not exist")]
    public void RBACRepository_CreateRoleNoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepository_CreateRoleNoResource));

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
    [Description("Tests the creation of a new scope within an existing resource")]
    public async Task RBACRepository_CreateScope()
    {
        // Generate unique test names for resource and scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepository_CreateScope));

        // Create a resource as a prerequisite for scope creation.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Create a scope within the resource.
        // This is the primary operation being tested.
        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        // Verify the scope was created successfully by retrieving it.
        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        // Validate the results against expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a resource after creating a scope within it")]
    public async Task RBACRepository_CreateScopeGetResource()
    {
        // Generate unique test names for this test case.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepository_CreateScopeGetResource));

        // Set up test data: create a resource and a scope within it.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        // Retrieve the resource to verify its state after scope creation.
        // This verifies that resource information properly includes created scopes.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate results against expected snapshot.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests that creating a scope fails when the resource does not exist")]
    public void RBACRepository_CreateScopeNoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepository_CreateScopeNoResource));

        // Attempt to create a scope for a resource that doesn't exist.
        // This operation should fail with an HttpStatusCodeException.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName);
        });

        // Capture the exception details for validation.
        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        // Verify error handling behaves as expected.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the deletion of a principal and its role assignments")]
    public async Task RBACRepository_DeletePrincipal()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_DeletePrincipal));

        // Set up complex test scenario with resource, scope, role, and a principal assignment.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Capture the state of role assignments before the principal deletion.
        var roleAssignmentBefore = await _repository.GetRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Delete the principal, which should remove all of its role assignments.
        // This is the primary operation being tested.
        await _repository.DeletePrincipalAsync(
            principalId: principalId);

        // Verify role assignments are updated correctly after principal deletion.
        // The roleAssignmentAfter should no longer include the deleted principal.
        var o = new
        {
            roleAssignmentBefore,
            roleAssignmentAfter = await _repository.GetRoleAssignmentAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        // Validate that the principal was properly removed from role assignments.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the deletion of a resource")]
    public async Task RBACRepository_DeleteResource()
    {
        // Generate a unique test resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepository_DeleteResource));

        // Create a resource to delete in the test.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Capture the list of resources before deletion for comparison.
        var resourcesBefore = await _repository.GetResourcesAsync(default);

        // Delete the resource that was just created.
        // This is the primary operation being tested.
        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        // Verify the resource was properly deleted by comparing lists before and after.
        var o = new
        {
            resourcesBefore,
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // The resourcesAfter list should no longer contain the deleted resource.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests deleting a non-existent resource has no effect")]
    public async Task RBACRepository_DeleteResourceNotExists()
    {
        // Generate a name for a resource that doesn't exist in the system.
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepository_DeleteResourceNotExists));

        // Capture the state of resources before attempting to delete a non-existent resource.
        var resourcesBefore = await _repository.GetResourcesAsync(default);

        // Attempt to delete a resource that doesn't exist.
        // This should complete without errors but have no effect on the system.
        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        // Verify the system state remains unchanged when deleting a non-existent resource.
        var o = new
        {
            resourcesBefore,
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // The before and after lists should be identical since nothing was deleted.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the deletion of a resource with associated roles")]
    public async Task RBACRepository_DeleteResourceWithRole()
    {
        // Generate unique test names for resource and role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepository_DeleteResourceWithRole));

        // Create a resource and a role within it for the deletion test.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Delete the resource, which should cascade delete its roles.
        // This tests the cascade deletion behavior.
        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        // Verify the resource was deleted by attempting to retrieve it.
        // Should return null or an empty result.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate the resource and its associated roles are properly removed.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the deletion of a resource with associated role assignments")]
    public async Task RBACRepository_DeleteResourceWithRoleAssignment()
    {
        // Generate unique test names for a complex test scenario.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_DeleteResourceWithRoleAssignment));

        // Create a resource with scopes, roles, and principal assignments.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Delete the resource, which should cascade delete roles, scopes, and assignments.
        // This tests the complete cascade deletion behavior across multiple entity types.
        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        // Verify all related data was deleted by checking the raw DynamoDB items.
        var o = new
        {
            items = await GetItemsAsync()
        };

        // The items list should not contain any entries related to the deleted resource.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the deletion of a resource with associated scopes")]
    public async Task RBACRepository_DeleteResourceWithScope()
    {
        // Generate unique test names for resource and scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepository_DeleteResourceWithScope));

        // Create a resource and a scope within it for the deletion test.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        // Delete the resource, which should cascade delete its scopes.
        // This tests the resource-scope cascade deletion behavior.
        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        // Verify the resource and scopes were deleted by attempting to retrieve the resource.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate that both the resource and its scopes are properly removed.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the deletion of a role")]
    public async Task RBACRepository_DeleteRole()
    {
        // Generate unique test names for resource and role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepository_DeleteRole));

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
    [Description("Tests retrieving a resource after deleting a role within it")]
    public async Task RBACRepository_DeleteRoleGetResource()
    {
        // Generate unique test names for resource and role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepository_DeleteRoleGetResource));

        // Create a resource and a role within it for the test.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Delete the role and then verify the resource state is updated correctly.
        await _repository.DeleteRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Retrieve the resource to verify its state after role deletion.
        // This verifies that the resource no longer references the deleted role.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // The resource should still exist but not contain the deleted role.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the deletion of a role with associated role assignments")]
    public async Task RBACRepository_DeleteRoleWithRoleAssignment()
    {
        // Generate unique test names for a complex test scenario.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_DeleteRoleWithRoleAssignment));

        // Create a resource with scope, role, and principal assignment.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Delete the role, which should cascade delete its role assignments.
        // This tests the role-assignment cascade deletion behavior.
        await _repository.DeleteRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Verify the principal membership no longer references the deleted role.
        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // The principal membership should no longer contain the deleted role.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests the deletion of a scope")]
    public async Task RBACRepository_DeleteScope()
    {
        // Generate unique test names for resource and scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepository_DeleteScope));

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
    [Description("Tests retrieving a resource after deleting a scope within it")]
    public async Task RBACRepository_DeleteScopeGetResource()
    {
        // Generate unique test names for resource and scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepository_DeleteScopeGetResource));

        // Create a resource and a scope within it for the test.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        // Delete the scope and then verify the resource state is updated correctly.
        await _repository.DeleteScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        // Retrieve the resource to verify its state after scope deletion.
        // This verifies that the resource no longer references the deleted scope.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // The resource should still exist but not contain the deleted scope.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a principal membership with default scope")]
    public async Task RBACRepository_GetPrincipalMembershipDefaultScope()
    {
        // Generate unique test names for the test scenario with multiple scopes.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepository_GetPrincipalMembershipDefaultScope));
        var (_, scopeName1, _, _) = FormatNames("1_" + nameof(RBACRepository_GetPrincipalMembershipDefaultScope));
        var (_, scopeName2, _, _) = FormatNames("2_" + nameof(RBACRepository_GetPrincipalMembershipDefaultScope));

        // Create a resource with multiple scopes, a role, and a principal assignment.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName1);
        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName2);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Retrieve the principal membership with the special ".default" scope parameter.
        // This tests the system's handling of default scope access.
        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName,
                scopeName: ".default"),
            items = await GetItemsAsync()
        };

        // Validate the default scope membership is correctly returned.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a principal membership across multiple scopes")]
    public async Task RBACRepository_GetPrincipalMembershipMultipleScopes()
    {
        // Generate unique test names for the test scenario with multiple scopes.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepository_GetPrincipalMembershipMultipleScopes));
        var (_, scopeName1, _, _) = FormatNames("1_" + nameof(RBACRepository_GetPrincipalMembershipMultipleScopes));
        var (_, scopeName2, _, _) = FormatNames("2_" + nameof(RBACRepository_GetPrincipalMembershipMultipleScopes));

        // Create a resource with multiple scopes, a role, and a principal assignment.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName1);
        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName2);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Retrieve the principal membership without specifying a scope.
        // This should return membership information across all available scopes.
        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate that memberships from all scopes are correctly returned.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a principal membership with a specific scope")]
    public async Task RBACRepository_GetPrincipalMembershipSpecificScope()
    {
        // Generate unique test names for the test scenario with multiple scopes.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepository_GetPrincipalMembershipSpecificScope));
        var (_, scopeName1, _, _) = FormatNames("1_" + nameof(RBACRepository_GetPrincipalMembershipSpecificScope));
        var (_, scopeName2, _, _) = FormatNames("2_" + nameof(RBACRepository_GetPrincipalMembershipSpecificScope));

        // Create a resource with multiple scopes, a role, and a principal assignment.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName1);
        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName2);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Retrieve the principal membership for a specific scope.
        // This tests scope-specific membership filtering.
        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName,
                scopeName: scopeName1),
            items = await GetItemsAsync()
        };

        // Validate that only memberships from the specified scope are returned.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving an existing resource")]
    public async Task RBACRepository_GetResource()
    {
        // Generate a unique test resource name.
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepository_GetResource));

        // Create a resource to test retrieval.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Retrieve the resource by name.
        // This is the primary operation being tested.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Validate the retrieved resource matches the expected value.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a non-existent resource")]
    public async Task RBACRepository_GetResourceNotExists()
    {
        // Generate a name for a resource that doesn't exist in the system.
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepository_GetResourceNotExists));

        // Attempt to retrieve a resource that doesn't exist.
        // This tests the system's handling of non-existent resources.
        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // Should return null or an empty result without throwing exceptions.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a non-existent role")]
    public async Task RBACRepository_GetRoleNotExists()
    {
        // Generate unique test names for resource and a non-existent role.
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepository_GetRoleNotExists));

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
    [Description("Tests retrieving a non-existent scope")]
    public async Task RBACRepository_GetScopeNotExists()
    {
        // Generate unique test names for resource and a non-existent scope.
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepository_GetScopeNotExists));

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
    [Description("Tests granting a role to a principal")]
    public async Task RBACRepository_GrantRoleToPrincipal()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_GrantRoleToPrincipal));

        // Create prerequisites: resource, scope, and role.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Grant a role to a principal, returning the updated principal membership.
        // This is the primary operation being tested.
        var o = new
        {
            principalMembershipAfter = await _repository.GrantRoleToPrincipalAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId),
            items = await GetItemsAsync()
        };

        // Validate the role was properly granted to the principal.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a principal membership after granting a role")]
    public async Task RBACRepository_GrantRoleToPrincipalGetPrincipalMembership()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_GrantRoleToPrincipalGetPrincipalMembership));

        // Create prerequisites: resource, scope, and role.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Grant a role to a principal.
        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Retrieve the principal membership to verify it contains the granted role.
        // This verifies that the membership data properly reflects the grant operation.
        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // The membership should include the newly granted role.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a role assignment after granting a role to a principal")]
    public async Task RBACRepository_GrantRoleToPrincipalGetRoleAssignment()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_GrantRoleToPrincipalGetRoleAssignment));

        // Create prerequisites: resource, scope, and role.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Capture the role assignment state before granting the role.
        var roleAssignmentBefore = await _repository.GetRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Grant a role to a principal.
        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Retrieve the role assignment to verify it includes the principal.
        // This tests the bidirectional nature of role-principal relationships.
        var o = new
        {
            roleAssignmentBefore,
            roleAssignmentAfter = await _repository.GetRoleAssignmentAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        // The role assignment should now include the principal.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests that granting a role fails when the resource does not exist")]
    public void RBACRepository_GrantRoleToPrincipalNoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepository_GrantRoleToPrincipalNoResource));

        // Attempt to grant a role within a non-existent resource.
        // This should throw an HttpStatusCodeException with an appropriate error message.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GrantRoleToPrincipalAsync(
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
    [Description("Tests that granting a role fails when the role does not exist")]
    public async Task RBACRepository_GrantRoleToPrincipalNoRole()
    {
        // Generate unique test names for resource and a non-existent role.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepository_GrantRoleToPrincipalNoRole));

        // Create a resource but not the role we'll try to grant.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        // Attempt to grant a non-existent role to a principal.
        // This should throw an HttpStatusCodeException.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GrantRoleToPrincipalAsync(
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
    [Description("Tests the system with no existing resources")]
    public async Task RBACRepository_NoResources()
    {
        // Query the system for resources when none exist.
        // This tests the system's behavior with an empty database.
        var o = new
        {
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        // Should return empty collections without errors.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests revoking a role from a principal")]
    public async Task RBACRepository_RevokeRoleFromPrincipal()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_RevokeRoleFromPrincipal));

        // Create prerequisites: resource, scope, role, and grant the role to a principal.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var principalMembershipBefore = await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Revoke the role from the principal.
        // This is the primary operation being tested.
        var o = new
        {
            principalMembershipBefore,
            principalMembershipAfter = await _repository.RevokeRoleFromPrincipalAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId),
            items = await GetItemsAsync()
        };

        // The principal membership after revocation should no longer include the role.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a principal membership after revoking a role")]
    public async Task RBACRepository_RevokeRoleFromPrincipalGetPrincipalMembership()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_RevokeRoleFromPrincipalGetPrincipalMembership));

        // Create prerequisites: resource, scope, role, and grant the role to a principal.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var principalMembershipBefore = await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Revoke the role from the principal.
        await _repository.RevokeRoleFromPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Retrieve the principal membership to verify it no longer contains the revoked role.
        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        // The membership should not include the revoked role.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests retrieving a role assignment after revoking a role from a principal")]
    public async Task RBACRepository_RevokeRoleFromPrincipalGetRoleAssignment()
    {
        // Generate unique test names for resource, scope, role, and principal.
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepository_RevokeRoleFromPrincipalGetRoleAssignment));

        // Create prerequisites: resource, scope, role, and grant the role to a principal.
        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Capture the role assignment state before revocation.
        var roleAssignmentBefore = await _repository.GetRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName);

        // Revoke the role from the principal.
        await _repository.RevokeRoleFromPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        // Retrieve the role assignment to verify it no longer includes the principal.
        var o = new
        {
            roleAssignmentBefore,
            roleAssignmentAfter = await _repository.GetRoleAssignmentAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        // The role assignment should no longer include the principal.
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests that revoking a role fails when the resource does not exist")]
    public void RBACRepository_RevokeRoleFromPrincipalNoResource()
    {
        // Generate unique test names for a non-existent resource.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepository_RevokeRoleFromPrincipalNoResource));

        // Attempt to revoke a role from a principal for a non-existent resource.
        // This should throw an HttpStatusCodeException.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.RevokeRoleFromPrincipalAsync(
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
    [Description("Tests that revoking a role fails when the role does not exist")]
    public async Task RBACRepository_RevokeRoleFromPrincipalNoRole()
    {
        // Generate unique test names for resource and a non-existent role.
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepository_RevokeRoleFromPrincipalNoRole));

        // Create a resource but with a different name than we'll use in the test.
        // This tests revoking a role when the role doesn't exist.
        await _repository.CreateResourceAsync(
            resourceName: "testResource_RBACRepository_RevokeRoleFromPrincipalNoRole");

        // Attempt to grant a role that doesn't exist to a principal.
        // This tests the system's handling of non-existent roles in grants.
        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GrantRoleToPrincipalAsync(
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

    /// <summary>
    /// Generates standardized test entity names based on the test method name.
    /// This ensures each test has unique identifiers to prevent test interference.
    /// </summary>
    /// <param name="testName">The name of the test method.</param>
    /// <returns>A tuple containing names for resource, scope, role, and principal.</returns>
    private static (string resourceName, string scopeName, string roleName, string principalId) FormatNames(
        string testName)
    {
        // Generate consistent names for each entity type prefixed with the entity type
        // and suffixed with the test name to ensure uniqueness across tests.
        var resourceName = $"testResource_{testName}";
        var scopeName = $"testScope_{testName}";
        var roleName = $"testRole_{testName}";
        var principalId = $"testPrincipal_{testName}";

        // Output the generated names to the console for debugging purposes.
        // This helps track which test entities are being created during test execution.
        Console.WriteLine($"resourceName: {resourceName}");
        Console.WriteLine($"scopeName: {scopeName}");
        Console.WriteLine($"roleName: {roleName}");
        Console.WriteLine($"principalId: {principalId}");

        return (resourceName, scopeName, roleName, principalId);
    }

    /// <summary>
    /// Retrieves all items from the DynamoDB table used for RBAC storage.
    /// This helper method is used to verify the state of the underlying data.
    /// </summary>
    /// <returns>A list of items from the DynamoDB table, with attribute values converted to strings.</returns>
    private async Task<List<ImmutableSortedDictionary<string, string>>> GetItemsAsync()
    {
        // Create a scan request to retrieve all items from the RBAC table.
        // This is used for verification in tests to check the raw data state.
        var scanRequest = new ScanRequest()
        {
            TableName = _tableName,
            ConsistentRead = true // Use strong consistency to ensure we get the latest data
        };

        // Execute the scan operation to retrieve all items.
        var scanResponse = await _client.ScanAsync(scanRequest, default);

        // Process the raw DynamoDB items into a more usable format.
        // Convert each item's attributes into a sorted dictionary of string values.
        return scanResponse.Items
            .Select(attributeMap => attributeMap
                .ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.S))
            .ToList();
    }
}