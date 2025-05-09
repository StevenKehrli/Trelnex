using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Authentication.Tests;

[Category("Authentication")]
public class AuthenticationTests : BaseApiTests
{
    [Test]
    [Description("Tests the /anonymous endpoint, which should always succeed")]
    public async Task Anonymous()
    {
        // Create a request to the /anonymous endpoint
        var request = new HttpRequestMessage(HttpMethod.Get, "/anonymous");

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response is successful and returns "anonymous"
        Assert.That(
            await response.Content.ReadAsStringAsync(),
            Is.EqualTo("anonymous"));
    }

    [Test]
    [Description("Tests that /testRolePolicy1 requires authentication (no header)")]
    public async Task RequirePermission_Unauthorized_NoHeader_1()
    {
        // Create a request to /testRolePolicy1 without the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Unauthorized
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    [Description("Tests that /testRolePolicy2 requires authentication (no header)")]
    public async Task RequirePermission_Unauthorized_NoHeader_2()
    {
        // Create a request to /testRolePolicy2 without the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Unauthorized
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    [Description("Tests that /testRolePolicy1 returns Forbidden when missing scope")]
    public async Task RequirePermission_Forbidden_MissingScope_1()
    {
        // Create a JWT token with the correct audience and role but missing scope
        // This tests that endpoints properly enforce scope requirements (should return 403 Forbidden)
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1", // Correct audience for TestPermission1
            principalId: "PrincipalId.RequirePermission_Forbidden_MissingScope_1", // Identifying user ID
            scopes: [], // Empty scopes array - this should cause authorization to fail
            roles: ["test.role.1"]); // Correct role for TestPermission1.TestRolePolicy

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy1 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Forbidden
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Description("Tests that /testRolePolicy2 returns Forbidden when missing scope")]
    public async Task RequirePermission_Forbidden_MissingScope_2()
    {
        // Create a JWT token with the correct audience and role but missing scope
        // This tests that endpoints properly enforce scope requirements (should return 403 Forbidden)
        var accessToken = _jwtProvider2.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2", // Correct audience for TestPermission2
            principalId: "PrincipalId.RequirePermission_Forbidden_MissingScope_2", // Identifying user ID
            scopes: [], // Empty scopes array - this should cause authorization to fail
            roles: ["test.role.2a"]); // Correct role for TestPermission2.TestRolePolicy

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy2 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Forbidden
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Description("Tests that /testRolePolicy1 returns Forbidden when missing role")]
    public async Task RequirePermission_Forbidden_MissingRole_1()
    {
        // Create a JWT token with a missing role
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_MissingRole_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: []);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy1 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Forbidden
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Description("Tests that /testRolePolicy2 returns Forbidden when missing role")]
    public async Task RequirePermission_Forbidden_MissingRole_2()
    {
        // Create a JWT token with a missing role
        var accessToken = _jwtProvider2.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_MissingRole_2",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-2"],
            roles: []);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy2 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Forbidden
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Description("Tests that /testRolePolicy1 returns Unauthorized when wrong audience")]
    public async Task RequirePermission_Forbidden_WrongAudience_1()
    {
        // Create a JWT token with a wrong audience
        var accessToken = _jwtProvider2.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongAudience_2",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-2"],
            roles: ["test.role.2a"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy1 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Unauthorized
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    [Description("Tests that /testRolePolicy2 returns Unauthorized when wrong audience")]
    public async Task RequirePermission_Forbidden_WrongAudience_2()
    {
        // Create a JWT token with a wrong audience
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongAudience_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["test.role.1"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy2 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Unauthorized
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    [Description("Tests that /testRolePolicy1 returns Forbidden when wrong scope")]
    public async Task RequirePermission_Forbidden_WrongScope_1()
    {
        // Create a JWT token with a wrong scope
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongScope_1",
            scopes: ["wrong.scope"],
            roles: ["test.role.1"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy1 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Forbidden
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Description("Tests that /testRolePolicy2 returns Forbidden when wrong scope")]
    public async Task RequirePermission_Forbidden_WrongScope_2()
    {
        // Create a JWT token with a wrong scope
        var accessToken = _jwtProvider2.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongScope_2",
            scopes: ["wrong.scope"],
            roles: ["test.role.2a"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy2 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Forbidden
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Description("Tests that /testRolePolicy1 returns Forbidden when wrong role")]
    public async Task RequirePermission_Forbidden_WrongRole_1()
    {
        // Create a JWT token with a wrong role
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongRole_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["wrong.role"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy1 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Forbidden
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Description("Tests that /testRolePolicy2 returns Forbidden when wrong role")]
    public async Task RequirePermission_Forbidden_WrongRole_2()
    {
        // Create a JWT token with a wrong role
        var accessToken = _jwtProvider2.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongRole_2",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-2"],
            roles: ["wrong.role"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy2 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response status code is Forbidden
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    [Description("Tests that /testRolePolicy1 returns the correct response when authorized")]
    public async Task RequirePermission_Response_1()
    {
        // Create a JWT token with the correct claims, scopes, and roles
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["test.role.1"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy1 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response is successful and returns the correct principal ID and role
        var testResponse = await response.Content.ReadFromJsonAsync<TestResponse>();
        Assert.That(testResponse, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(testResponse.Message, Is.EqualTo("PrincipalId.RequirePermission_Response_1"));
            Assert.That(testResponse.Role, Is.EqualTo("test.role.1"));
        });
    }

    [Test]
    [Description("Tests that /testRolePolicy2 returns the correct response when authorized with test.role.2a")]
    public async Task RequirePermission_Response_2a()
    {
        // Create a JWT token with the correct claims, scopes, and roles
        var accessToken = _jwtProvider2.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Response_2a",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-2"],
            roles: ["test.role.2a"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy2 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response is successful and returns the correct principal ID and role
        var testResponse = await response.Content.ReadFromJsonAsync<TestResponse>();
        Assert.That(testResponse, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(testResponse.Message, Is.EqualTo("PrincipalId.RequirePermission_Response_2a"));
            Assert.That(testResponse.Role, Is.EqualTo("test.role.2a"));
        });
    }

    [Test]
    [Description("Tests that /testRolePolicy2 returns the correct response when authorized with test.role.2b")]
    public async Task RequirePermission_Response_2b()
    {
        // Create a JWT token with the correct claims, scopes, and roles
        var accessToken = _jwtProvider2.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Response_2b",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-2"],
            roles: ["test.role.2b"]);

        // Create an authorization header with the JWT token
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        // Create a request to /testRolePolicy2 with the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");
        request.Headers.Authorization = authorizationHeader;

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Verify that the response is successful and returns the correct principal ID and role
        var testResponse = await response.Content.ReadFromJsonAsync<TestResponse>();
        Assert.That(testResponse, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(testResponse.Message, Is.EqualTo("PrincipalId.RequirePermission_Response_2b"));
            Assert.That(testResponse.Role, Is.EqualTo("test.role.2b"));
        });
    }
}
