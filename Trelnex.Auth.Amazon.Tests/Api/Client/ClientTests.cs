using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Client.Tests;

public class ClientTests : BaseApiTests
{
    [Test]
    public async Task Client_Delete()
    {
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: [ "Scope.trelnex-auth-amazon-tests-authentication-1" ],
            roles: [ "test.role.1" ]);
        
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        var response = await testClient.Delete();

        Assert.That(
            response?.Message,
            Is.EqualTo("delete1"));
    }

    [Test]
    public async Task Client_Get()
    {
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: [ "Scope.trelnex-auth-amazon-tests-authentication-1" ],
            roles: [ "test.role.1" ]);
        
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        var response = await testClient.Get();

        Assert.That(
            response?.Message,
            Is.EqualTo("get1"));
    }

    [Test]
    public async Task Client_Patch()
    {
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: [ "Scope.trelnex-auth-amazon-tests-authentication-1" ],
            roles: [ "test.role.1" ]);
        
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        var response = await testClient.Patch();

        Assert.That(
            response?.Message,
            Is.EqualTo("patch1"));
    }

    [Test]
    public async Task Client_Post()
    {
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: [ "Scope.trelnex-auth-amazon-tests-authentication-1" ],
            roles: [ "test.role.1" ]);
        
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        var response = await testClient.Post();

        Assert.That(
            response?.Message,
            Is.EqualTo("post1"));
    }

    [Test]
    public async Task Client_Put()
    {
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: [ "Scope.trelnex-auth-amazon-tests-authentication-1" ],
            roles: [ "test.role.1" ]);
        
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        var response = await testClient.Put();

        Assert.That(
            response?.Message,
            Is.EqualTo("put1"));
    }

    [Test]
    public async Task Client_GetQueryString()
    {
        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: null!);

        var response = await testClient.QueryString("value");

        Assert.That(
            response?.Message,
            Is.EqualTo("value"));
    }
}
