using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Responses;
using Trelnex.Core.Api.Swagger;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.Tests;

public abstract class BaseApiTests
{
    internal TestJwtProvider _jwtProvider1;
    internal TestJwtProvider _jwtProvider2;

    protected HttpClient _httpClient;

    private WebApplication _webApplication;

    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        _jwtProvider1 = new TestJwtProvider(
            jwtAlgorithm: new TestAlgorithm(),
            keyId: "KeyId.trelnex-auth-amazon-tests-authentication",
            issuer: "Issuer.trelnex-auth-amazon-tests-authentication-1",
            expirationInMinutes: 15);

        _jwtProvider2 = new TestJwtProvider(
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
                    .MapGet("/anonymous", () => "anonymous")
                    .Produces<string>();

                app
                    .MapGet("/testRolePolicy1", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<string>();

                app
                    .MapGet("/testRolePolicy2", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission2.TestRolePolicy>()
                    .Produces<string>();

                app
                    .MapGet("/exception", () =>
                    {
                        throw new HttpStatusCodeException(HttpStatusCode.BadRequest);
                    })
                    .Produces<HttpStatusCodeResponse>(StatusCodes.Status400BadRequest);

                app
                    .MapDelete("/delete1", () =>
                    {
                        return new TestResponse
                        {
                            Message = "delete1"
                        };
                    })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app
                    .MapGet("/get1", () =>
                    {
                        return new TestResponse
                        {
                            Message = "get1"
                        };
                    })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app
                    .MapPatch("/patch1", () =>
                    {
                        return new TestResponse
                        {
                            Message = "patch1"
                        };
                    })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app
                    .MapPost("/post1", () =>
                    {
                        return new TestResponse
                        {
                            Message = "post1"
                        };
                    })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app
                    .MapPut("/put1", () =>
                    {
                        return new TestResponse
                        {
                            Message = "put1"
                        };
                    })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app
                    .MapGet("/queryString", ([FromQuery] string value) =>
                    {
                        return new TestResponse
                        {
                            Message = value
                        };
                    })
                    .Produces<TestResponse>();

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
