# Trelnex.Core

A comprehensive .NET library providing core components for building cloud-native applications with robust validation, authentication, HTTP communication, and observability features.

## Overview

Trelnex.Core is the foundation library for the Trelnex ecosystem, offering:

- **Validation**: Extended FluentValidation capabilities
- **HTTP Status Code Handling**: Standardized HTTP status code exceptions and utilities
- **Identity Management**: Flexible credential system for cloud authentication
- **Client Communication**: Base HTTP client components for service-to-service communication
- **Observability**: Tracing and monitoring through OpenTelemetry integration

## Components

### Validation

Components built on top of FluentValidation to enable robust validation capabilities throughout Trelnex applications.

#### Key Features

- **CompositeValidator\<T>**: Combines multiple validators into a single validation pipeline
  ```csharp
  var compositeValidator = new CompositeValidator<YourModel>(
      new DomainValidator(), 
      new BusinessRulesValidator());
  ```

- **ValidationException**: Specialized exception for validation failures with HTTP status code 422
  ```csharp
  throw new ValidationException(
      message: "The user data is not valid.",
      errors: new Dictionary<string, string[]> {
          { "email", new[] { "Invalid email format" } }
      });
  ```

- **ValidatorExtensions**: Custom validation methods like `NotDefault<T>()` for date and GUID types
  ```csharp
  RuleFor(x => x.CreatedDate).NotDefault();
  RuleFor(x => x.Id).NotDefault();
  ```

- **ValidationResultExtensions**: Handle validation results with methods like `ValidateOrThrow`
  ```csharp
  result.ValidateOrThrow<UserModel>();  // Throws if validation failed
  ```

### HTTP Status Code Handling

Utilities for standardized HTTP status code handling across applications.

#### Key Features

- **HttpStatusCodeExtensions**: Convert status codes to human-readable reason phrases
  ```csharp
  HttpStatusCode.NotFound.ToReason(); // Returns "Not Found"
  ```

- **HttpStatusCodeException**: Encapsulates HTTP status code information for API error handling
  ```csharp
  throw new HttpStatusCodeException(
      HttpStatusCode.BadRequest,
      "Validation failed",
      errors);
  ```

### Identity

A flexible credential management system for handling various authentication providers and access tokens across different cloud platforms and services.

#### Key Features

- **Credential System**:
  - `ICredential`: Base interface for credential objects that can provide access tokens for specified scopes
  - `ICredentialProvider`: Interface to obtain credentials and access token providers for different cloud platforms
  - `AccessToken`: Represents an authentication token with metadata such as expiration time and token type
  - `AccessTokenProvider`: Manages retrieval of access tokens for specific client types and scopes

- **Status Monitoring**:
  - `AccessTokenHealth`: Enum to track token validity status (Valid, Expired)
  - `AccessTokenStatus`: Contains detailed token status information including health, scopes, and expiration
  - `CredentialStatus`: Aggregates status of all access tokens for a credential

- **Health Checks Integration**:
  - `CredentialStatusHealthCheck`: ASP.NET Core health check that monitors credential validity
  - `HealthChecksExtensions`: Register credential health checks with the application

- **Dependency Injection Support**:
  - `CredentialProviderExtensions`: Provides DI extensions for registering and retrieving credential providers

- **Example Usage**:
  ```csharp
  // Register a credential provider
  services.AddCredentialProvider(myCredentialProvider);

  // Get an access token
  var tokenProvider = credentialProvider.GetAccessTokenProvider<MyApiClient>("my-api-scope");
  AccessToken token = tokenProvider.GetAccessToken();
  ```

- **Features**:
  - Cloud-Agnostic Design: The architecture supports credentials for multiple cloud providers through a unified interface
  - Token Lifecycle Management: Automatic tracking of token expiration and health status
  - Health Monitoring: Built-in health checks to monitor credential validity at runtime
  - Strongly-Typed Clients: Generic support for client-specific token providers
  - Service Registration: DI extensions for simplified setup in ASP.NET Core applications

### Client

Core components for building HTTP clients for service-to-service communication.

#### Key Features

- **BaseClient**: Foundation for all HTTP clients with standardized methods
  ```csharp
  public class MyApiClient : BaseClient
  {
      public MyApiClient(HttpClient httpClient) 
          : base(httpClient)
      {
      }
      
      public async Task<MyResponse> GetResourceAsync(string id)
      {
          var uri = BaseAddress.AppendPath($"resources/{id}");
          return await Get<MyResponse>(uri);
      }
  }
  ```

- **UriExtensions**: Methods for manipulating URIs
  - `AppendPath`: Adds a path segment to an existing URI
  - `AddQueryString`: Adds a query parameter to an existing URI

- **HeadersExtensions**: Methods for HTTP headers
  - `AddAuthorizationHeader`: Adds an authorization header to a request

- **ClientConfiguration**: Record that defines the configuration parameters for HTTP clients
  - `CredentialProviderName`: The name of the credential provider to use
  - `Scope`: The OAuth scope required for access
  - `BaseAddress`: The base URL for the service

- **ClientExtensions**: Register HTTP clients with DI
  - `AddClient<IClient, TClient>`: Registers a client interface and implementation

- **Configuration**:
  ```json
  {
    "Clients": {
      "MyClient": {
        "BaseAddress": "https://api.example.com/",
        "CredentialProviderName": "DefaultCredentialProvider",
        "Scope": "api://example/access"
      }
    }
  }
  ```

- **Setup and Registration**:
  ```csharp
  // In your startup/program class
  services.AddClient<IMyClient, MyClient>(configuration);
  ```

- **Error Handling**: The `BaseClient` provides standardized error handling using `HttpStatusCodeException`. Custom error handlers can be provided to each request method to parse service-specific error responses.

- **Authentication**: Authentication is handled through credential providers, which supply access tokens. The client automatically uses the configured credential provider to obtain the necessary tokens for each request.

### Observability

Components for monitoring and tracing application behavior using OpenTelemetry.

#### Key Features

- **TraceAttribute**: PostSharp aspect for automatically tracing method execution
  ```csharp
  [Trace]
  public void MyMethod([TraceInclude] string parameter1, string parameter2)
  {
      // Method execution is traced with parameter1 included in trace tags
  }
  ```

- **TraceIncludeAttribute**: Mark method parameters to include in traces

- **Features**:
  - Automatic Method Tracing: Add the `[Trace]` attribute to methods of interest
  - Parameter Capture: Choose which parameters to include in traces with `[TraceInclude]`
  - Performance Insights: Identify slow methods and performance bottlenecks
  - Distributed Tracing: Track requests across service boundaries
  - Error Correlation: See which methods failed and why in your traces
