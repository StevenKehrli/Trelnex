using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Trelnex.Auth.Amazon.Services.JWT;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Swagger;
using Trelnex.Core.Data;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Tests;

public abstract class BaseApiTests
{
    protected IJwtProvider _jwtProvider1;
    protected IJwtProvider _jwtProvider2;

    protected HttpClient _httpClient;

    private WebApplication _webApplication;

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
                
                services
                    .AddSwaggerToServices()
                    .AddInMemoryCommandProviders(
                        configuration,
                        logger,
                        options => { } );
            },
            useApplication: app =>
            {
                app
                    .MapGet("/anonymous", () => "anonymous");
                
                app
                    .MapGet("/testRolePolicy1", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission1.TestRolePolicy>();

                app
                    .MapGet("/testRolePolicy2", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission2.TestRolePolicy>();

                app
                    .MapGet("/exception", () => 
                    {
                        throw new HttpStatusCodeException(HttpStatusCode.BadRequest);
                    })
                    .Produces<HttpStatusCodeException>(StatusCodes.Status400BadRequest);

                app.AddSwaggerToWebApplication();
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

    protected static AccessToken CreateAccessToken(
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
