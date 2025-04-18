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
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Tests.Authentication;

public class AuthenticationTests
{    private IJwtProvider _jwtProvider;

    private WebApplication _webApplication;

    private HttpClient _httpClient;

    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        _jwtProvider = JwtProvider.Create(
            jwtAlgorithm: new TestAlgorithm(),
            keyId: "KeyId.trelnex-auth-amazon-tests-authentication",
            issuer: "Issuer.trelnex-auth-amazon-tests-authentication",
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
                    .AddPermissions<TestPermission>(logger);
            },
            useApplication: app =>
            {
                app
                    .MapGet("/testRolePolicy", () => "testRolePolicy")
                    .RequirePermission<TestPermission.TestRolePolicy>();
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
    public void RequirePermission_Unauthorized_NoHeader()
    {
        // create the request without the authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy");
        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have not set the authorization header
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public void RequirePermission_Forbidden_MissingRole()
    {
        // create a JWT token with a missing role
        var accessToken = CreateAccessToken(
            scope: "Scope.trelnex-auth-amazon-tests-authentication");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong role
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_MissingScope()
    {
        // create a JWT token with a wrong scope
        var accessToken = CreateAccessToken(
            role: "wrong.role");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong scope
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_WrongRole()
    {
        // create a JWT token with a wrong role
        var accessToken = CreateAccessToken(
            scope: "Scope.trelnex-auth-amazon-tests-authentication",
            role: "wrong.role");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong role
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Forbidden_WrongScope()
    {
        // create a JWT token with a wrong scope
        var accessToken = CreateAccessToken(
            scope: "wrong.scope",
            role: "test.role");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have the wrong scope
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void RequirePermission_Response()
    {
        var accessToken = CreateAccessToken(
            scope: "Scope.trelnex-auth-amazon-tests-authentication",
            role: "test.role");

        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should succeed because we have set the authorization header
        Assert.That(
            response.Content.ReadAsStringAsync().Result,
            Is.EqualTo("testRolePolicy"));
    }

    private AccessToken CreateAccessToken(
        string? scope = null,
        string? role = null)
    {
        return _jwtProvider.Encode(
            principalId: "principalId",
            audience: "Audience.trelnex-auth-amazon-tests-authentication",
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
