# Trelnex.Core.Api.Tests

## Overview

This project contains integration tests for the Trelnex.Core.Api framework. It provides a comprehensive test suite that validates the API's authentication, authorization, endpoint handling, client functionality, and other core features using an in-memory test environment.

## Architecture

The test framework is designed around a layered architecture that simulates a real API environment while remaining entirely in-memory:

```
┌───────────────────────────────────────────────────────┐
│        Test Classes (e.g., AuthenticationTests)       │
├───────────────────────────────────────────────────────┤
│                      BaseApiTests                     │
├───────────────────┬───────────────────────────────────┤
│  TestJwtProvider  │  In-Memory Web App w/ TestServer  │
├───────────────────┼───────────────────────────────────┤
│   TestAlgorithm   │           TestPermission          │
└───────────────────┴───────────────────────────────────┘
```

### Key Components

- **BaseApiTests**: Foundation class that creates the in-memory test environment
- **TestJwtProvider**: Generates JWT tokens with configurable claims for authentication testing
- **TestAlgorithm**: Provides cryptographic signing for JWT tokens
- **TestPermission**: Configures authentication and authorization schemes
- **TestAccessTokenProvider**: Supplies tokens to client tests
- **TestClient1**: Client implementation for testing API consumption
- **TestResponse**: Standard response object for endpoint testing

## Test Categories

The framework includes tests for several key areas:

1. **Authentication Tests**: Verify token validation, audience validation, and role-based access
2. **Client Tests**: Validate client behavior for different HTTP methods
3. **Exception Tests**: Ensure proper error handling and status codes
4. **HealthCheck Tests**: Verify API health endpoint functionality
5. **Swagger Tests**: Validate API documentation generation
6. **DataProvider Tests**: Test in-memory data provider functionality

## Technical Design

### In-Memory Testing

The framework uses ASP.NET Core's TestServer to host a complete web application in-memory, eliminating the need for external servers or dependencies. This approach provides:

- Fast test execution
- Consistent test environment
- Full isolation between test runs
- Deterministic test behavior

### Authentication Framework

The authentication system simulates a multi-scheme JWT authentication setup:

- **Dual Authentication Schemes**: Two parallel schemes (TestPermission1 and TestPermission2) with different requirements
- **JWT Token Generation**: Configurable token creation with custom claims, scopes, and roles
- **Role-Based Authorization**: Endpoints protected with different role policies
- **Multiple Test Scenarios**: Tests for missing tokens, invalid signatures, wrong roles, etc.

### Test Endpoints

The framework configures various endpoints for testing different aspects of the API:

- **/anonymous**: No authentication required
- **/testRolePolicy1**: Protected by TestPermission1 with role "test.role.1"
- **/testRolePolicy2**: Protected by TestPermission2 with role "test.role.2a" or "test.role.2b"
- **/exception**: Throws an exception for error handling testing
- HTTP method endpoints (**/delete1**, **/get1**, etc.): Test different HTTP methods
- **/queryString**: Tests query parameter handling

### Client Testing

The framework includes a client testing setup to validate API consumption patterns:

- **TestClient1**: Implementation of BaseClient for accessing API endpoints
- **TestAccessTokenProvider**: Supplies authentication tokens to client
- **TestResponse**: Standard response object returned from test endpoints

## Setup and Teardown

Each test derived from BaseApiTests benefits from:

1. **One-Time Setup**:
   - JWT token providers setup
   - In-memory web application configuration
   - Endpoint registration
   - HTTP client creation

2. **One-Time Teardown**:
   - Application graceful shutdown
   - Resource disposal

This approach ensures efficient test execution while maintaining isolation between test classes.

## Best Practices Demonstrated

The test framework demonstrates several testing best practices:

- **Isolation**: Tests run in-memory with no external dependencies
- **Comprehensive Coverage**: Tests authentication, client functionality, error handling, etc.
- **Readability**: Clear test naming and organization by feature
- **Resource Management**: Proper setup and teardown of test resources
- **Documentation**: Well-documented test components and methods

## Usage Example

To create a new test class:

```csharp
[Category("YourFeature")]
public class YourFeatureTests : BaseApiTests
{
    [Test]
    [Description("Tests some aspect of your feature")]
    public async Task YourFeature_SomeScenario()
    {
        // Generate a token with appropriate claims
        var accessToken = _jwtProvider1.Encode(
            audience: "Audience.trelnex-auth-amazon-tests-authentication-1",
            principalId: "PrincipalId.YourTest",
            scopes: ["Scope.trelnex-auth-amazon-tests-authentication-1"],
            roles: ["test.role.1"]);

        // Create a request with the token
        var request = new HttpRequestMessage(HttpMethod.Get, "/your-endpoint");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

        // Send the request and verify the response
        var response = await _httpClient.SendAsync(request);

        // Assert expected behavior
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        // Additional assertions...
    }
}
```

## Extending the Framework

To add new test endpoints:

1. Modify the `useApplication` callback in BaseApiTests.TestFixtureSetup
2. Add new endpoints with appropriate authentication requirements
3. Create corresponding test methods in your test class

To add new authentication schemes:

1. Create a new TestPermission class modeled after TestPermission1/2
2. Add it to the services configuration in BaseApiTests.TestFixtureSetup
3. Create a new JWT provider in BaseApiTests.TestFixtureSetup
4. Add test endpoints protected by the new permission