using System.Net;
using System.Net.Http.Headers;
using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Authentication.Tests;

public class AuthenticationTests : BaseApiTests
{
    [Test]
    public void Anonymous()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/anonymous");
        var response = _httpClient.SendAsync(request).Result;

        // this should succeed because there is no authentication or authorization
        Assert.That(
            response.Content.ReadAsStringAsync().Result,
            Is.EqualTo("anonymous"));
    }

    [Test]
    public void RequirePermission_Unauthorized_NoHeader_1()
    {
        // create the request without the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");
        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have not set the authorization header
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public void RequirePermission_Unauthorized_NoHeader_2()
    {
        // create the request without the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");
        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have not set the authorization header
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public void RequirePermission_Forbidden_MissingScope_1()
    {
        // create a JWT token with a wrong scope
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider1,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_MissingScope_1",
            role: "test.role.1");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong scope
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_MissingScope_2()
    {
        // create a JWT token with a wrong scope
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider2,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_MissingScope_2",
            role: "test.role.2");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong scope
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_MissingRole_1()
    {
        // create a JWT token with a missing role
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider1,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_MissingRole_1",
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong role
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_MissingRole_2()
    {
        // create a JWT token with a missing role
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider2,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_MissingRole_2",
            scope: "Scope.trelnex-auth-amazon-tests-authentication-2");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong role
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_WrongAudience_1()
    {
        // create a JWT token with a wrong audience
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider2,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongAudience_2",
            scope: "Scope.trelnex-auth-amazon-tests-authentication-2",
            role: "test.role.2");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong audience
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public void RequirePermission_Forbidden_WrongAudience_2()
    {
        // create a JWT token with a wrong audience
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider1,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongAudience_1",
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            role: "test.role.1");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong audience
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public void RequirePermission_Forbidden_WrongScope_1()
    {
        // create a JWT token with a wrong scope
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider1,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongScope_1",
            scope: "wrong.scope",
            role: "test.role.1");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong scope
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_WrongScope_2()
    {
        // create a JWT token with a wrong scope
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider2,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongScope_2",
            scope: "wrong.scope",
            role: "test.role.2");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong scope
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_WrongRole_1()
    {
        // create a JWT token with a wrong role
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider1,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongRole_1",
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            role: "wrong.role");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong role
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_WrongRole_2()
    {
        // create a JWT token with a wrong role
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider2,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Forbidden_WrongRole_2",
            scope: "Scope.trelnex-auth-amazon-tests-authentication-2",
            role: "wrong.role");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong role
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Response_1()
    {
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider1,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.RequirePermission_Response_1",
            scope: "Scope.trelnex-auth-amazon-tests-authentication-1",
            role: "test.role.1");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy1");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should succeed because we have set the authorization header
        Assert.That(
            response.Content.ReadAsStringAsync().Result,
            Is.EqualTo("PrincipalId.RequirePermission_Response_1"));
    }

    [Test]
    public void RequirePermission_Response_2()
    {
        var accessToken = CreateAccessToken(
            jwtProvider: _jwtProvider2,
            audience: "Audience.trelnex-auth-amazon-tests-authentication-2",
            principalId: "PrincipalId.RequirePermission_Response_2",
            scope: "Scope.trelnex-auth-amazon-tests-authentication-2",
            role: "test.role.2");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy2");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should succeed because we have set the authorization header
        Assert.That(
            response.Content.ReadAsStringAsync().Result,
            Is.EqualTo("PrincipalId.RequirePermission_Response_2"));
    }
}
