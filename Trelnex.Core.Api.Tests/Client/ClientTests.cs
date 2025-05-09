using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Client.Tests;

[Category("Client")]
public class ClientTests : BaseApiTests
{
    #region Tests

    [Test]
    [Description("Tests the Delete method of the TestClient")]
    public async Task Client_Delete()
    {
        // Encode the JWT token with specific claims, scopes and roles
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["test.role.1"]);

        // Set up the access token provider with the encoded token
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        // Create the test client with the http client and access token provider
        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        // Call the Delete method on the test client
        var response = await testClient.Delete();

        // Verify that the message in the response is equal to "delete1"
        Assert.That(
            response?.Message,
            Is.EqualTo("delete1"));
    }

    [Test]
    [Description("Tests the Get method of the TestClient")]
    public async Task Client_Get()
    {
        // Encode the JWT token with specific claims, scopes and roles
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["test.role.1"]);

        // Set up the access token provider with the encoded token
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        // Create the test client with the http client and access token provider
        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        // Call the Get method on the test client
        var response = await testClient.Get();

        // Verify that the message in the response is equal to "get1"
        Assert.That(
            response?.Message,
            Is.EqualTo("get1"));
    }

    [Test]
    [Description("Tests the GetQueryString method of the TestClient")]
    public async Task Client_GetQueryString()
    {
        // Create the test client with the http client and a null access token provider
        // because this method does not require authorization
        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: null!);

        // Call the QueryString method on the test client
        var response = await testClient.QueryString("value");

        // Verify that the message in the response is equal to "value"
        Assert.That(
            response?.Message,
            Is.EqualTo("value"));
    }

    [Test]
    [Description("Tests the Patch method of the TestClient")]
    public async Task Client_Patch()
    {
        // Encode the JWT token with specific claims, scopes and roles
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["test.role.1"]);

        // Set up the access token provider with the encoded token
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        // Create the test client with the http client and access token provider
        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        // Call the Patch method on the test client
        var response = await testClient.Patch();

        // Verify that the message in the response is equal to "patch1"
        Assert.That(
            response?.Message,
            Is.EqualTo("patch1"));
    }

    [Test]
    [Description("Tests the Post method of the TestClient")]
    public async Task Client_Post()
    {
        // Encode the JWT token with specific claims, scopes and roles
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["test.role.1"]);

        // Set up the access token provider with the encoded token
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        // Create the test client with the http client and access token provider
        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        // Call the Post method on the test client
        var response = await testClient.Post();

        // Verify that the message in the response is equal to "post1"
        Assert.That(
            response?.Message,
            Is.EqualTo("post1"));
    }

    [Test]
    [Description("Tests the Put method of the TestClient")]
    public async Task Client_Put()
    {
        // Encode the JWT token with specific claims, scopes and roles
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["test.role.1"]);

        // Set up the access token provider with the encoded token
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            accessToken: accessToken);

        // Create the test client with the http client and access token provider
        var testClient = new TestClient1(
            httpClient: _httpClient,
            accessTokenProvider: accessTokenProvider);

        // Call the Put method on the test client
        var response = await testClient.Put();

        // Verify that the message in the response is equal to "put1"
        Assert.That(
            response?.Message,
            Is.EqualTo("put1"));
    }

    #endregion
}
