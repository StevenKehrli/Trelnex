using System.Collections.Immutable;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Snapshooter.NUnit;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core;

namespace Trelnex.Auth.Amazon.Tests.Services.RBAC;

[Ignore("Requires a DynamoDB table.")]
public class RBACRepositoryTests
{
    private AmazonDynamoDBClient _client = null!;
    private string _tableName = null!;
    private RBACRepository _repository = null!;

    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // This method is called once prior to executing any of the tests in the fixture.

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // create a dynamodb client for scan and cleanup
        var credentials = FallbackCredentialsFactory.GetCredentials();

        var region = configuration
            .GetSection("RBAC:Region")
            .Value!;

        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        var tableName = configuration
            .GetSection("RBAC:TableName")
            .Value!;

        _client = new AmazonDynamoDBClient(credentials, regionEndpoint);
        _tableName = tableName;

        _repository = new RBACRepository(
            new ScopeNameValidator(),
            _client,
            tableName);
    }

    [OneTimeTearDown]
    public void TextFixtureCleanup()
    {
        _client.Dispose();
    }

    [TearDown]
    public async Task TestCleanup()
    {
        // This method is called after each test case is run.

        // 1. get all items in the table

        var scanRequest = new ScanRequest()
        {
            TableName = _tableName,
            ConsistentRead = true
        };

        // scan
        var scanResponse = await _client.ScanAsync(scanRequest, default);

        if (scanResponse.Items.Count == 0) return;

        // convert the response to the items
        var items = scanResponse.Items
            .Select(attributeMap =>
            {
                return (
                    resourceName: attributeMap["resourceName"].S,
                    subjectName: attributeMap["subjectName"].S);
            });

        // 2. delete all items in the table

        // create the delete requests
        var deleteRequests = items.Select(item =>
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "resourceName", new AttributeValue(item.resourceName) },
                { "subjectName", new AttributeValue(item.subjectName) }
            };

            return new DeleteRequest { Key = key };
        });

        var writeRequests = deleteRequests.Select(deleteRequest => new WriteRequest { DeleteRequest = deleteRequest });

        var batchWriteRequest = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                { _tableName, writeRequests.ToList() }
            }
        };

        // delete until complete
        while (batchWriteRequest.RequestItems.Count > 0)
        {
            // delete
            var batchWriteItemResponse = await _client.BatchWriteItemAsync(batchWriteRequest, default);

            batchWriteRequest.RequestItems = batchWriteItemResponse.UnprocessedItems;
        }
    }

    [Test]
    public async Task RBACRepositoryTests_CreateResource()
    {
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepositoryTests_CreateResource));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public void RBACRepositoryTests_CreateRoleNoResource()
    {
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepositoryTests_CreateRoleNoResource));

        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateRoleAsync(
                resourceName: resourceName,
                roleName: roleName);
        });

        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_CreateRole()
    {
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepositoryTests_CreateRole));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_CreateRoleGetResource()
    {
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepositoryTests_CreateRoleGetResource));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public void RBACRepositoryTests_CreateScopeNoResource()
    {
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepositoryTests_CreateScopeNoResource));

        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.CreateScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName);
        });

        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_CreateScope()
    {
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepositoryTests_CreateScope));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_CreateScopeGetResource()
    {
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepositoryTests_CreateScopeGetResource));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeletePrincipal()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_DeletePrincipal));

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

        var roleAssignmentBefore = await _repository.GetRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.DeletePrincipalAsync(
            principalId: principalId);

        var o = new
        {
            roleAssignmentBefore,
            roleAssignmentAfter = await _repository.GetRoleAssignmentAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteResource()
    {
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepositoryTests_DeleteResource));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        var resourcesBefore = await _repository.GetResourcesAsync(default);

        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            resourcesBefore,
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteResourceNotExists()
    {
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepositoryTests_DeleteResourceNotExists));

        var resourcesBefore = await _repository.GetResourcesAsync(default);

        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            resourcesBefore,
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteResourceWithRole()
    {
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepositoryTests_DeleteResourceWithRole));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }


    [Test]
    public async Task RBACRepositoryTests_DeleteResourceWithRoleAssignment()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_DeleteResourceWithRoleAssignment));

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

        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteResourceWithScope()
    {
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepositoryTests_DeleteResourceWithScope));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.DeleteResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteRole()
    {
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepositoryTests_DeleteRole));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.DeleteRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteRoleGetResource()
    {
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepositoryTests_DeleteRoleGetResource));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.DeleteRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteRoleWithRoleAssignment()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_DeleteRoleWithRoleAssignment));

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

        await _repository.DeleteRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteScope()
    {
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepositoryTests_DeleteScope));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.DeleteScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_DeleteScopeGetResource()
    {
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepositoryTests_DeleteScopeGetResource));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.DeleteScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GetPrincipalMembershipDefaultScope()
    {
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));
        var (_, scopeName1, _, _) = FormatNames("1_" + nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));
        var (_, scopeName2, _, _) = FormatNames("2_" + nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));

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

        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName,
                scopeName: ".default"),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GetPrincipalMembershipMultipleScopes()
    {
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));
        var (_, scopeName1, _, _) = FormatNames("1_" + nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));
        var (_, scopeName2, _, _) = FormatNames("2_" + nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));

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

        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GetPrincipalMembershipSpecificScope()
    {
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));
        var (_, scopeName1, _, _) = FormatNames("1_" + nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));
        var (_, scopeName2, _, _) = FormatNames("2_" + nameof(RBACRepositoryTests_GetPrincipalMembershipDefaultScope));

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

        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName,
                scopeName: scopeName1),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GetResource()
    {
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepositoryTests_GetResource));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GetResourceNotExists()
    {
        var (resourceName, _, _, _) = FormatNames(nameof(RBACRepositoryTests_GetResourceNotExists));

        var o = new
        {
            resourceAfter = await _repository.GetResourceAsync(
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GetRoleNotExists()
    {
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepositoryTests_GetRoleNotExists));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            roleAfter = await _repository.GetRoleAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GetScopeNotExists()
    {
        var (resourceName, scopeName, _, _) = FormatNames(nameof(RBACRepositoryTests_GetScopeNotExists));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        var o = new
        {
            scopeAfter = await _repository.GetScopeAsync(
                resourceName: resourceName,
                scopeName: scopeName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GrantRoleToPrincipal()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_GrantRoleToPrincipal));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var o = new
        {
            principalMembershipAfter = await _repository.GrantRoleToPrincipalAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GrantRoleToPrincipalGetPrincipalMembership()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_GrantRoleToPrincipalGetPrincipalMembership));

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

        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GrantRoleToPrincipalGetRoleAssignment()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_GrantRoleToPrincipalGetRoleAssignment));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        await _repository.CreateScopeAsync(
            resourceName: resourceName,
            scopeName: scopeName);

        await _repository.CreateRoleAsync(
            resourceName: resourceName,
            roleName: roleName);

        var roleAssignmentBefore = await _repository.GetRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.GrantRoleToPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        var o = new
        {
            roleAssignmentBefore,
            roleAssignmentAfter = await _repository.GetRoleAssignmentAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public void RBACRepositoryTests_GrantRoleToPrincipalNoResource()
    {
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_GrantRoleToPrincipalNoResource));

        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GrantRoleToPrincipalAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId);
        });

        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_GrantRoleToPrincipalNoRole()
    {
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_GrantRoleToPrincipalNoRole));

        await _repository.CreateResourceAsync(
            resourceName: resourceName);

        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GrantRoleToPrincipalAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId);
        });

        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_NoResources()
    {
        var (resourceName, _, roleName, _) = FormatNames(nameof(RBACRepositoryTests_NoResources));

        var o = new
        {
            resourcesAfter = await _repository.GetResourcesAsync(default),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_RevokeRoleFromPrincipal()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_RevokeRoleFromPrincipal));

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

        var o = new
        {
            principalMembershipBefore,
            principalMembershipAfter = await _repository.RevokeRoleFromPrincipalAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_RevokeRoleFromPrincipalGetPrincipalMembership()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_RevokeRoleFromPrincipalGetPrincipalMembership));

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

        await _repository.RevokeRoleFromPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        var o = new
        {
            principalMembershipAfter = await _repository.GetPrincipalMembershipAsync(
                principalId: principalId,
                resourceName: resourceName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_RevokeRoleFromPrincipalGetRoleAssignment()
    {
        var (resourceName, scopeName, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_RevokeRoleFromPrincipalGetRoleAssignment));

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

        var roleAssignmentBefore = await _repository.GetRoleAssignmentAsync(
            resourceName: resourceName,
            roleName: roleName);

        await _repository.RevokeRoleFromPrincipalAsync(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);

        var o = new
        {
            roleAssignmentBefore,
            roleAssignmentAfter = await _repository.GetRoleAssignmentAsync(
                resourceName: resourceName,
                roleName: roleName),
            items = await GetItemsAsync()
        };

        Snapshot.Match(o);
    }

    [Test]
    public void RBACRepositoryTests_RevokeRoleFromPrincipalNoResource()
    {
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_RevokeRoleFromPrincipalNoResource));

        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.RevokeRoleFromPrincipalAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId);
        });

        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        Snapshot.Match(o);
    }

    [Test]
    public async Task RBACRepositoryTests_RevokeRoleFromPrincipalNoRole()
    {
        var (resourceName, _, roleName, principalId) = FormatNames(nameof(RBACRepositoryTests_RevokeRoleFromPrincipalNoRole));

        await _repository.CreateResourceAsync(
            resourceName: "testResource_RBACRepositoryTests_RevokeRoleFromPrincipalNoRole");

        var ex = Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
        {
            await _repository.GrantRoleToPrincipalAsync(
                resourceName: resourceName,
                roleName: roleName,
                principalId: principalId);
        });

        var o = new
        {
            statusCode = ex.HttpStatusCode,
            message = ex.Message
        };

        Snapshot.Match(o);
    }

    private static (string resourceName, string scopeName, string roleName, string principalId) FormatNames(
        string testName)
    {
        var resourceName = $"testResource_{testName}";
        var scopeName = $"testScope_{testName}";
        var roleName = $"testRole_{testName}";
        var principalId = $"testPrincipal_{testName}";

        Console.WriteLine($"resourceName: {resourceName}");
        Console.WriteLine($"scopeName: {scopeName}");
        Console.WriteLine($"roleName: {roleName}");
        Console.WriteLine($"principalId: {principalId}");

        return (resourceName, scopeName, roleName, principalId);
    }

    private async Task<List<ImmutableSortedDictionary<string, string>>> GetItemsAsync()
    {
        var scanRequest = new ScanRequest()
        {
            TableName = _tableName,
            ConsistentRead = true
        };

        // scan
        var scanResponse = await _client.ScanAsync(scanRequest, default);

        return scanResponse.Items
            .Select(attributeMap => attributeMap
                .ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.S))
            .ToList();
    }
}
