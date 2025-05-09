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
using Trelnex.Core.Api.Swagger;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// Base class for API integration tests.
/// Provides common setup and teardown logic for creating and configuring a test web application,
/// including JWT providers, authentication, permissions, Swagger, in-memory command providers,
/// and an HTTP client for making requests to the test application.
/// Inherit from this class to create API tests that require a fully configured test environment.
/// </summary>
public abstract class BaseApiTests
{
    #region Internal Fields

    // JWT providers for generating test tokens
    internal TestJwtProvider _jwtProvider1;
    internal TestJwtProvider _jwtProvider2;

    #endregion

    #region Protected Fields

    // HTTP client for making requests to the test application
    protected HttpClient _httpClient;

    // The test web application
    private WebApplication _webApplication;

    #endregion

    #region Setup

    /// <summary>
    /// Sets up the test fixture, creating JWT providers, a web application, and an HTTP client.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create test JWT providers using a test algorithm and key
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

        // Create the web application using a test configuration
        _webApplication = Application.CreateWebApplication(
            args: [],
            addApplication: (services, configuration, logger) =>
            {
                // Add the test server (in-memory)
                services.AddSingleton<IHostLifetime, NoopHostLifetime>();
                services.AddSingleton<IServer, TestServer>();

                // Add authentication and permissions using test permissions
                services
                    .AddAuthentication(configuration)
                    .AddPermissions<TestPermission1>(logger)
                    .AddPermissions<TestPermission2>(logger);

                // Add Swagger and in-memory command providers for testing
                services
                    .AddSwaggerToServices()
                    .AddInMemoryCommandProviders(
                        configuration,
                        logger,
                        options => { }); // No specific options for in-memory provider in base test
            },
            useApplication: app =>
            {
                // Map endpoints for testing authentication and authorization

                // Anonymous endpoint
                app.MapGet("/anonymous", () => "anonymous")
                    .Produces<string>();

                // Endpoint requiring TestRolePolicy1
                app.MapGet("/testRolePolicy1", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<string>();

                // Endpoint requiring TestRolePolicy2
                app.MapGet("/testRolePolicy2", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission2.TestRolePolicy>()
                    .Produces<string>();

                // Endpoint that throws an exception
                app.MapGet("/exception", () =>
                    {
                        throw new HttpStatusCodeException(HttpStatusCode.BadRequest);
                    })
                    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

                // Map endpoints for testing HTTP methods and responses
                app.MapDelete("/delete1", () => new TestResponse { Message = "delete1" })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app.MapGet("/get1", () => new TestResponse { Message = "get1" })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app.MapPatch("/patch1", () => new TestResponse { Message = "patch1" })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app.MapPost("/post1", () => new TestResponse { Message = "post1" })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                app.MapPut("/put1", () => new TestResponse { Message = "put1" })
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<TestResponse>();

                // Map endpoint for testing query strings
                app.MapGet("/queryString", ([FromQuery] string value) => new TestResponse { Message = value })
                    .Produces<TestResponse>();

                // Add Swagger UI to the web application
                app.AddSwaggerToWebApplication();
            });

        // Start the web application
        await _webApplication.StartAsync();

        // Create the HTTP client to our web application for making requests
        _httpClient = _webApplication.GetTestClient();
    }

    #endregion

    #region Teardown

    /// <summary>
    /// Tears down the test fixture, stopping and disposing of the web application and HTTP client.
    /// </summary>
    [OneTimeTearDown]
    public async Task TestFixtureCleanup()
    {
        // Dispose of the web application if it exists
        if (_webApplication is not null)
        {
            await _webApplication.StopAsync();
            await _webApplication.DisposeAsync();
        }

        // Dispose of the HTTP client if it exists
        _httpClient?.Dispose();
    }

    #endregion

    #region Nested Classes

    // A no-op host lifetime for testing purposes
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

    #endregion
}
