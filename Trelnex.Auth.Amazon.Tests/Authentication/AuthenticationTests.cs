using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Trelnex.Auth.Amazon.Services.JWT;
using Trelnex.Core.Api;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Data;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Tests.Authentication;

public class AuthenticationTests
{
    private IJwtProvider _jwtProvider1;
    private IJwtProvider _jwtProvider2;

    private WebApplication _webApplication;

    private HttpClient _httpClient;

    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        _jwtProvider1 = JwtProvider.Create(
            jwtAlgorithm: new TestAlgorithm(),
            keyId: "KeyId.trelnex-auth-amazon-tests-authentication",
            issuer: "Issuer.trelnex-auth-amazon-tests-authentication-1",
            expirationInMinutes: 15);

        _jwtProvider2 = JwtProvider.Create(
            jwtAlgorithm: new TestAlgorithm(),
            keyId: "KeyId.trelnex-auth-amazon-tests-authentication",
            issuer: "Issuer.trelnex-auth-amazon-tests-authentication-2",
            expirationInMinutes: 15);

        _webApplication = Application.CreateWebApplication(
            args: [],
            addApplication: (services, configuration, logger) =>
            {
                // add the test server
                services.AddSingleton<IHostLifetime, NoopHostLifetime>();
                services.AddSingleton<IServer, TestServer>();

                services
                    .AddAuthentication(configuration)
                    .AddPermissions<TestPermission1>(logger)
                    .AddPermissions<TestPermission2>(logger);
            },
            useApplication: app =>
            {
                app
                    .MapGet("/testRolePolicy1", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission1.TestRolePolicy>();

                app
                    .MapGet("/testRolePolicy2", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission2.TestRolePolicy>();
            });

        await _webApplication.StartAsync();

        // create the http client to our web application
        _httpClient = _webApplication.GetTestClient();
    }

    [OneTimeTearDown]
    public async Task TestFixtureCleanup()
    {
        if (_webApplication is not null)
        {
            await _webApplication.StopAsync();
            await _webApplication.DisposeAsync();
        }

        _httpClient?.Dispose();
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

    private static AccessToken CreateAccessToken(
        IJwtProvider jwtProvider,
        string audience,
        string principalId,
        string? scope = null,
        string? role = null)
    {
        return jwtProvider.Encode(
            principalId: principalId,
            audience: audience,
            scopes: scope is null ? [] : [ scope ],
            roles: role is null ? [] : [ role ]);
    }

    private sealed class NoopHostLifetime : IHostLifetime
    {
        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task WaitForStartAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
