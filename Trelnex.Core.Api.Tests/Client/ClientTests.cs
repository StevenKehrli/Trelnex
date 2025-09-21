using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Client.Tests;

/// <summary>
/// Tests for the TestClient1 class that verify HTTP method behaviors and proper response handling.
///
/// These tests ensure that the TestClient1 methods correctly call the corresponding endpoints
/// defined in BaseApiTests.cs and properly deserialize the responses into TestResponse objects.
/// Each test verifies the correct TestResponse.Message value is received, confirming that:
/// 1. The request was sent to the correct endpoint
/// 2. Authentication was properly handled
/// 3. The response was correctly deserialized
///
/// The TestResponse pattern allows for consistent verification across different HTTP methods.
/// </summary>
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

        // Create a TestAccessTokenProvider that will supply the token to the client
        // This bridges between the token generation (TestJwtProvider) and the client authentication
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1", // Associated scope
            accessToken: accessToken); // The pre-generated token with necessary claims

        // Create authenticated HTTP client
        using var httpClient = CreateAuthenticatedHttpClient(accessTokenProvider);
        var testClient = new TestClient1(httpClient);

        // Call the Delete method on the test client
        var response = await testClient.DeleteAsync();

        // Verify that the message in the TestResponse is equal to "delete1"
        // This confirms the response came from the /delete1 endpoint
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

        // Create a TestAccessTokenProvider that will supply the token to the client
        // This bridges between the token generation (TestJwtProvider) and the client authentication
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1", // Associated scope
            accessToken: accessToken); // The pre-generated token with necessary claims

        // Create authenticated HTTP client and test client
        using var httpClient = CreateAuthenticatedHttpClient(accessTokenProvider);
        var testClient = new TestClient1(httpClient);

        // Call the Get method on the test client
        var response = await testClient.GetAsync();

        // Verify that the message in the TestResponse is equal to "get1"
        // This confirms the response came from the /get1 endpoint
        Assert.That(
            response?.Message,
            Is.EqualTo("get1"));
    }

    [Test]
    [Description("Tests the GetQueryString method of the TestClient")]
    public async Task Client_GetQueryString()
    {
        // Create anonymous HTTP client since this endpoint does not require authentication.
        // This demonstrates how TestClient1/BaseClient can handle both authenticated and unauthenticated
        // requests, and shows that TestAccessTokenProvider is only needed for protected endpoints.
        using var httpClient = CreateAnonymousHttpClient();
        var testClient = new TestClient1(httpClient);

        // Call the QueryString method on the test client
        var response = await testClient.QueryStringAsync("value");

        // Verify that the message in the TestResponse is equal to "value"
        // This confirms the query string parameter was correctly passed to and processed by the endpoint
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

        // Create a TestAccessTokenProvider that will supply the token to the client
        // This bridges between the token generation (TestJwtProvider) and the client authentication
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1", // Associated scope
            accessToken: accessToken); // The pre-generated token with necessary claims

        // Create authenticated HTTP client and test client
        using var httpClient = CreateAuthenticatedHttpClient(accessTokenProvider);
        var testClient = new TestClient1(httpClient);

        // Call the Patch method on the test client
        var response = await testClient.PatchAsync();

        // Verify that the message in the TestResponse is equal to "patch1"
        // This confirms the response came from the /patch1 endpoint
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

        // Create a TestAccessTokenProvider that will supply the token to the client
        // This bridges between the token generation (TestJwtProvider) and the client authentication
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1", // Associated scope
            accessToken: accessToken); // The pre-generated token with necessary claims

        // Create authenticated HTTP client and test client
        using var httpClient = CreateAuthenticatedHttpClient(accessTokenProvider);
        var testClient = new TestClient1(httpClient);

        // Call the Post method on the test client
        var response = await testClient.PostAsync();

        // Verify that the message in the TestResponse is equal to "post1"
        // This confirms the response came from the /post1 endpoint
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

        // Create a TestAccessTokenProvider that will supply the token to the client
        // This bridges between the token generation (TestJwtProvider) and the client authentication
        var accessTokenProvider = new TestAccessTokenProvider(
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1", // Associated scope
            accessToken: accessToken); // The pre-generated token with necessary claims

        // Create authenticated HTTP client and test client
        using var httpClient = CreateAuthenticatedHttpClient(accessTokenProvider);
        var testClient = new TestClient1(httpClient);

        // Call the Put method on the test client
        var response = await testClient.PutAsync();

        // Verify that the message in the TestResponse is equal to "put1"
        // This confirms the response came from the /put1 endpoint
        Assert.That(
            response?.Message,
            Is.EqualTo("put1"));
    }

    #endregion
}
