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
/// The foundation class for all API integration tests in the Trelnex test framework.
///
/// BaseApiTests is the cornerstone of the test architecture, providing a complete
/// in-memory test environment that simulates a fully functional API with authentication,
/// authorization, and various endpoints. It serves as:
///
/// 1. Test Infrastructure Provider - Creates and configures an in-memory web application
///    with TestServer, eliminating the need for external servers or dependencies.
///
/// 2. Authentication Framework - Sets up two parallel authentication schemes using
///    TestPermission1 and TestPermission2, enabling comprehensive authentication testing.
///
/// 3. Token Generation System - Provides TestJwtProvider instances for creating tokens
///    with various claims combinations to test different authentication scenarios.
///
/// 4. Endpoint Configuration - Defines multiple test endpoints with different authorization
///    requirements and response patterns to test various API behaviors.
///
/// 5. Resource Management - Handles proper initialization and cleanup of all test resources,
///    ensuring tests don't leak resources or affect each other.
///
/// Key components established by this class:
/// - TestJwtProvider instances for token generation
/// - In-memory web application with TestServer
/// - Authentication and authorization configuration
/// - Test endpoints with various protection levels
/// - HTTP client for making requests to test endpoints
///
/// All specialized test classes (AuthenticationTests, ClientTests, etc.) inherit from
/// this base class to leverage its pre-configured test environment, ensuring consistent
/// baseline behavior across all test scenarios.
/// </summary>
public abstract class BaseApiTests
{
    #region Internal Fields

    /// <summary>
    /// JWT token provider for scheme 1 (TestPermission1) authentication testing.
    ///
    /// This provider creates tokens with the issuer "Issuer.trelnex-auth-amazon-tests-authentication-1"
    /// that can be used to access endpoints protected by TestPermission1.TestRolePolicy.
    ///
    /// It's used by:
    /// - AuthenticationTests to test authentication and authorization rules
    /// - ClientTests to create tokens for authenticated client requests
    ///
    /// The field is made internal rather than private to allow derived test classes
    /// direct access for creating tokens with specific claims.
    /// </summary>
    internal TestJwtProvider _jwtProvider1;

    /// <summary>
    /// JWT token provider for scheme 2 (TestPermission2) authentication testing.
    ///
    /// This provider creates tokens with the issuer "Issuer.trelnex-auth-amazon-tests-authentication-2"
    /// that can be used to access endpoints protected by TestPermission2.TestRolePolicy.
    ///
    /// It complements _jwtProvider1 to enable testing of:
    /// - Multiple authentication schemes operating in parallel
    /// - Proper authorization enforcement for each scheme
    /// - Cross-scheme access attempts (which should fail)
    ///
    /// This dual-provider approach creates a comprehensive test environment for
    /// multi-tenant or multi-service authentication scenarios.
    /// </summary>
    internal TestJwtProvider _jwtProvider2;

    #endregion

    #region Protected Fields

    /// <summary>
    /// HTTP client for making requests to the in-memory test application.
    ///
    /// This client is created by _webApplication.GetTestClient() and is connected directly
    /// to the in-memory TestServer, allowing tests to make HTTP requests that are processed
    /// by the test web application without going through the network stack.
    ///
    /// This client is used by:
    /// - All test classes inherited from BaseApiTests to make direct HTTP requests to test endpoints
    /// - TestClient1 in client tests as its underlying HTTP client
    ///
    /// The field is protected to allow derived test classes to use it for making requests
    /// while preventing them from replacing it with a different client.
    /// </summary>
    protected HttpClient _httpClient;

    /// <summary>
    /// The in-memory ASP.NET Core web application used for testing.
    ///
    /// This application hosts:
    /// - The TestServer for in-memory HTTP processing
    /// - All authentication and authorization services
    /// - All test endpoints for various test scenarios
    ///
    /// It's configured in TestFixtureSetup and torn down in TestFixtureCleanup.
    /// The application runs entirely in-memory, eliminating external dependencies
    /// and allowing tests to run in complete isolation.
    ///
    /// The field is private because derived test classes shouldn't need to interact
    /// with it directly; they should use _httpClient instead.
    /// </summary>
    private WebApplication _webApplication;

    #endregion

    #region Setup

    /// <summary>
    /// Initializes the complete test environment for all derived test classes.
    ///
    /// This method performs the critical setup work that makes the test framework possible:
    ///
    /// 1. Token Generation Setup - Creates two TestJwtProvider instances:
    ///    - _jwtProvider1 for TestPermission1-protected endpoints
    ///    - _jwtProvider2 for TestPermission2-protected endpoints
    ///    These providers enable tests to create tokens with precise claim combinations.
    ///
    /// 2. Web Application Creation - Builds an in-memory ASP.NET Core application with:
    ///    - TestServer for in-memory HTTP hosting
    ///    - Authentication services with TestPermission1 and TestPermission2
    ///    - Swagger documentation services
    ///    - In-memory command providers for data operations
    ///
    /// 3. Endpoint Configuration - Defines various test endpoints:
    ///    - Authentication test endpoints (/anonymous, /testRolePolicy1, /testRolePolicy2)
    ///    - HTTP method test endpoints (/delete1, /get1, /post1, etc.)
    ///    - Exception testing endpoint (/exception)
    ///    - Query string parameter endpoint (/queryString)
    ///    Each endpoint has specific authorization requirements and response patterns.
    ///
    /// 4. Application Initialization - Starts the web application and creates an HTTP client,
    ///    making the test environment ready for requests from test methods.
    ///
    /// This comprehensive setup is performed once per test class (via [OneTimeSetUp]),
    /// ensuring efficient test execution while maintaining complete isolation between
    /// different test classes.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create test JWT providers using TestAlgorithm for token generation
        // These providers create tokens for different authentication schemes

        // Provider for TestPermission1-protected endpoints
        // Uses issuer 1 to create tokens that will be accepted by TestPermission1 validators
        _jwtProvider1 = new TestJwtProvider(
            jwtAlgorithm: new TestAlgorithm(), // RSA-SHA256 algorithm that creates cryptographically valid signatures
            keyId: "KeyId.trelnex-auth-amazon-tests-authentication", // Key ID in token header
            issuer: "Issuer.trelnex-auth-amazon-tests-authentication-1", // Issuer for scheme 1
            expirationInMinutes: 15); // Valid for 15 minutes

        // Provider for TestPermission2-protected endpoints
        // Uses issuer 2 to create tokens that will be accepted by TestPermission2 validators
        _jwtProvider2 = new TestJwtProvider(
            jwtAlgorithm: new TestAlgorithm(), // Same algorithm instance ensures consistent signature generation
            keyId: "KeyId.trelnex-auth-amazon-tests-authentication", // Same key ID
            issuer: "Issuer.trelnex-auth-amazon-tests-authentication-2", // Different issuer
            expirationInMinutes: 15); // Same expiration time

        // Create the web application using a test configuration
        _webApplication = Application.CreateWebApplication(
            args: [],
            addApplication: (services, configuration, logger) =>
            {
                // Add the test server (in-memory)
                services.AddSingleton<IHostLifetime, NoopHostLifetime>();
                services.AddSingleton<IServer, TestServer>();

                // Add authentication and permissions using test permissions
                // This sets up two parallel authentication schemes with different validation criteria:
                // 1. TestPermission1 - Validates tokens for audience 1, issuer 1, with role "test.role.1"
                // 2. TestPermission2 - Validates tokens for audience 2, issuer 2, with role "test.role.2"
                // This allows tests to verify proper authentication and authorization enforcement
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

                // Endpoint requiring TestPermission1.TestRolePolicy
                // This endpoint requires tokens with:
                // - Audience: "Audience.trelnex-auth-amazon-tests-authentication-1"
                // - Issuer: "Issuer.trelnex-auth-amazon-tests-authentication-1"
                // - Role: "test.role.1"
                // It returns the ObjectId claim from the authenticated user's token
                app.MapGet("/testRolePolicy1", (IRequestContext context) => context.ObjectId)
                    .RequirePermission<TestPermission1.TestRolePolicy>()
                    .Produces<string>();

                // Endpoint requiring TestPermission2.TestRolePolicy
                // This endpoint requires tokens with:
                // - Audience: "Audience.trelnex-auth-amazon-tests-authentication-2"
                // - Issuer: "Issuer.trelnex-auth-amazon-tests-authentication-2"
                // - Role: "test.role.2"
                // It returns the ObjectId claim from the authenticated user's token
                // This endpoint is intentionally parallel to /testRolePolicy1 but with different auth requirements
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
                // Each endpoint returns a TestResponse with a unique message value that identifies the endpoint
                // This allows tests to verify they're receiving responses from the correct endpoint
                // All these endpoints are protected by TestPermission1.TestRolePolicy
                // which requires tokens with the role "test.role.1", audience 1, and issuer 1
                // They return TestResponse objects with unique message values to verify correct routing
                // These endpoints are used by ClientTests to test all HTTP methods with authentication

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
                // This endpoint echoes back the query parameter value in the TestResponse
                // to verify proper query string parameter handling
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
    /// Performs proper cleanup of all test resources when test execution completes.
    ///
    /// This method ensures that all resources created during TestFixtureSetup are
    /// properly released and disposed, preventing resource leaks and potential
    /// interference between test classes. It handles:
    ///
    /// 1. Web Application Shutdown - Stops the ASP.NET Core web application gracefully,
    ///    allowing any in-progress operations to complete and resources to be released.
    ///
    /// 2. Resource Disposal - Properly disposes of:
    ///    - The web application (releasing all registered services)
    ///    - The HTTP client (closing connections and releasing network resources)
    ///
    /// This cleanup method is executed once per test class (via [OneTimeTearDown]),
    /// ensuring that each test class starts with a completely fresh environment and
    /// that resources aren't leaked between test runs.
    ///
    /// Proper cleanup is essential for:
    /// - Preventing memory leaks in the test process
    /// - Ensuring network ports and connections are released
    /// - Maintaining test isolation and reproducibility
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

    /// <summary>
    /// A minimal IHostLifetime implementation that does nothing, used for in-memory testing.
    ///
    /// This class provides a no-operation implementation of the IHostLifetime interface,
    /// which is required by the ASP.NET Core hosting system. In a normal application,
    /// this interface is implemented by classes that handle application lifetime events
    /// like graceful shutdown, but in the test environment, we don't need this functionality.
    ///
    /// This implementation:
    /// - Returns immediately from WaitForStartAsync (no need to wait for external events)
    /// - Returns immediately from StopAsync (no cleanup needed for external hosting)
    ///
    /// Using this implementation allows the test framework to:
    /// - Run entirely in-memory without external hosting dependencies
    /// - Avoid unnecessary complexity in the test environment
    /// - Maintain full control over application lifetime within the test process
    /// </summary>
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
