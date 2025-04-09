using System.Net;
using System.Net.Http.Headers;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.Serilog;

namespace Trelnex.Core.Api.Tests.Authentication;

public class AuthenticationTests
{
    private WebApplication _webApplication;

    private HttpClient _httpClient;

    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        var builder = WebApplication.CreateBuilder();

        // configure test server
        builder.WebHost.UseTestServer();

        // add serilog
        var bootstrapLogger = builder.Services.AddSerilog(
            builder.Configuration,
            nameof(Application));

        builder.Services
            .AddAuthentication(builder.Configuration)
            .AddPermissions<TestPermission>(bootstrapLogger);

        _webApplication = builder.Build();

        _webApplication.UseAuthentication();
        _webApplication.UseAuthorization();

        _webApplication.MapGet("/testRolePolicy", () => "testRolePolicy")
            .RequirePermission<TestPermission.TestRolePolicy>();

        await _webApplication.StartAsync();

        // create the http client to our web application
        _httpClient = _webApplication.GetTestServer().CreateClient();
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
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwtToken(
                scope: "Scope.trelnex-core-api-tests-authentication"));

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
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwtToken(
                role: "wrong.role"));

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
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwtToken(
                role: "wrong.role",
                scope: "Scope.trelnex-core-api-tests-authentication"));

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
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwtToken(
                role: "test.role",
                scope: "wrong.scope"));

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
        var authorizationHeader = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwtToken(
                role: "test.role",
                scope: "Scope.trelnex-core-api-tests-authentication"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/testRolePolicy");

        request.Headers.Authorization = authorizationHeader;

        var response = _httpClient.SendAsync(request).Result;

        // this should succeed because we have set the authorization header
        Assert.That(
            response.Content.ReadAsStringAsync().Result,
            Is.EqualTo("testRolePolicy"));
    }

    private static string CreateJwtToken(
        string? role = null,
        string? scope = null)
    {
        // create the jwt builder with the specified algorithm
        var jwtBuilder = JwtBuilder
            .Create()
            .WithAlgorithm(new HMACSHA256Algorithm())
            .WithSecret(nameof(TestPermission));

        // add the roles claim
        if (role is not null)
        {
            jwtBuilder
                .AddClaim("roles", new[] { role });
        }

        // add the scope claim
        if (scope is not null)
        {
            jwtBuilder
                .AddClaim("scp", scope );
        }

        return jwtBuilder.Encode();
    }
}
