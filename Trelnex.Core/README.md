# Trelnex.Core

`Trelnex.Core` provides foundational components for HTTP clients, identity management, validation, and observability.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Client

<details>

<summary>Expand</summary>

&nbsp;

The `Trelnex.Core.Client` namespace simplifies HTTP client operations with a focus on:

- Type-safe HTTP requests and responses
- Standardized error handling
- HTTP header management
- URI manipulation

### Components

#### BaseClient

`BaseClient` is an abstract base class for building HTTP clients:

- Type-safe HTTP method implementations (GET, POST, PUT, PATCH, DELETE)
- JSON serialization and deserialization
- Standardized error handling and status code processing
- Header management
- Streamlined request and response pipeline

Usage example:
```csharp
public class MyApiClient : BaseClient
{
    public MyApiClient(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
        : base(httpClient, accessTokenProvider)
    {
    }

    public async Task<ResponseModel> GetResourceAsync(string id)
    {
        var uri = BaseAddress.AppendPath($"resources/{id}");
        return await Get<ResponseModel>(uri);
    }
}
```

#### Error Handling

The `BaseClient` provides standardized error handling through:

- HTTP status code processing
- Custom error handler support via delegates
- Exception throwing with `HttpStatusCodeException` for non-success status codes

#### HeadersExtensions

Extension methods for HTTP headers:

- `AddAuthorizationHeader(string authorizationHeader)`: Adds an authorization header to an HTTP request

#### UriExtensions

Extension methods for the `Uri` class that simplify URL manipulation:

- `AppendPath(string path)`: Safely appends a path segment to a URI
- `AddQueryString(string key, string value)`: Adds or appends a query string parameter to a URI

#### IClientFactory

Interface for creating API client instances:

- `Create(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider)`: Creates a new client instance
- `Name`: Gets the unique identifier for this client factory

### Best Practices

When using these components:

1. Derive from `BaseClient` to build service-specific clients
2. Leverage the type-safe HTTP methods for request and response handling
3. Implement custom error handlers for service-specific error responses
4. Use the extension methods to add headers and manipulate URIs

</details>

## Identity

<details>

<summary>Expand</summary>

&nbsp;

The `Trelnex.Core.Identity` namespace offers a flexible and provider-agnostic approach to handling authentication credentials, access tokens, and token health monitoring. This design allows applications to work with different authentication providers (like AWS, Azure, or custom providers) through a unified interface.

### Components

#### AccessToken

The `AccessToken` class represents an authentication token that can be used to access secured resources:

- `Token`: The actual token string value
- `TokenType`: Identifies the token type (e.g., "Bearer")
- `ExpiresOn`: Timestamp when the token expires
- `RefreshOn`: Optional timestamp indicating when the token should be refreshed
- `GetAuthorizationHeader()`: Utility method that formats the token for use in HTTP headers

#### ICredential

The `ICredential` interface defines the contract for obtaining access tokens:

- `GetAccessToken(string scope)`: Retrieves an access token for the specified scope

#### ICredentialProvider

The `ICredentialProvider` interface is the primary entry point for obtaining credentials:

- `Name`: Gets the name of the credential provider
- `GetAccessTokenProvider(string scope)`: Returns an access token provider for the specified scope
- `GetStatus()`: Retrieves the current status of the credential

The generic variant `ICredentialProvider<TCredential>` extends this interface with:

- `GetCredential()`: Returns the specific credential type (e.g., AWS credentials, Azure credentials)

#### AccessTokenProvider

The `AccessTokenProvider` implements `IAccessTokenProvider` to provide:

- `Scope`: The scope of the access token
- `GetAccessToken()`: Returns the access token for the configured scope
- `Create()`: Factory method to create and warm up an access token provider

#### Health & Status Monitoring

The Identity system includes components for monitoring credential health:

- `AccessTokenHealth`: An enum indicating if a token is `Valid` or `Expired`
- `AccessTokenStatus`: Records the health, scopes, expiration, and additional metadata for a token
- `CredentialStatus`: Collects the status of multiple access tokens for a credential

#### Exception Handling

- `AccessTokenUnavailableException`: Thrown when an access token cannot be obtained

### Integration with Cloud Providers

The Identity system serves as the foundation for provider-specific implementations:

- `Trelnex.Core.Amazon/Identity`: AWS-specific credential providers
- `Trelnex.Core.Azure/Identity`: Azure-specific credential providers

### Usage Example

```csharp
// Get a credential provider (implementation varies by cloud provider)
var credentialProvider = serviceProvider.GetRequiredService<ICredentialProvider>();

// Get an access token provider for the specified scope
var tokenProvider = credentialProvider.GetAccessTokenProvider("https://api.example.com/.default");

// Use the token provider to get an access token
var token = tokenProvider.GetAccessToken();

// Use the token in an HTTP request
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.Token);
```

### Health Monitoring

The Identity system supports health checks to monitor the status of credentials:

```csharp
// Get the status of a credential
var status = credentialProvider.GetStatus();

// Check if all tokens are valid
bool allValid = status.Statuses.All(s => s.Health == AccessTokenHealth.Valid);
```

This enables applications to proactively detect and respond to authentication issues before they cause failures.

</details>

## Validation

<details>

<summary>Expand</summary>

&nbsp;

The `Trelnex.Core.Validation` namespace provides utilities for implementing validation using FluentValidation.

### Components

#### CompositeValidator

`CompositeValidator<T>` is a class that combines multiple FluentValidation validators:

- Extends `AbstractValidator<T>` from FluentValidation
- Allows combining two or more validators for a single type
- Useful for applying both core and domain-specific validation rules

#### ValidationException

`ValidationException` extends `HttpStatusCodeException` for validation-specific error handling:

- Uses HTTP status code 422 (Unprocessable Content)
- Includes structured validation errors for detailed client feedback
- Works well with API response handling

#### ValidationResultExtensions

Extension methods for FluentValidation's `ValidationResult`:

- `ValidateOrThrow`: Throws a `ValidationException` if validation fails
- `ValidateOrThrow<T>`: Type-safe version that includes the type name in error messages
- Support for validating collections with proper error formatting and grouping

#### ValidatorExtensions

Adds additional validation rules to FluentValidation:

- `NotDefault<T>` for `DateTime`: Ensures a DateTime property is not the default value
- `NotDefault<T>` for `DateTimeOffset`: Ensures a DateTimeOffset property is not the default value
- `NotDefault<T>` for `Guid`: Ensures a Guid property is not the default/empty value

### Usage Example

```csharp
// Create a composite validator
var validator = new CompositeValidator<User>(
    new CoreUserValidator(),    // Basic validation rules
    new DomainUserValidator()   // Domain-specific rules
);

// Validate and throw if invalid
var result = validator.Validate(user);
result.ValidateOrThrow<User>();

// Validate a collection
var results = users.Select(user => validator.Validate(user));
results.ValidateOrThrow<User>();
```

</details>

## Observability

<details>

<summary>Expand</summary>

&nbsp;

The `Trelnex.Core.Observability` namespace provides attributes and functionality for implementing distributed tracing in applications.

### Components

#### TraceMethodAttribute

`TraceMethodAttribute` is an attribute that enables automatic method-level tracing. When applied to a method, it creates and manages an [Activity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) that tracks the method's execution in the context of distributed tracing.

Features:

- Creates Activities that integrate with OpenTelemetry
- Supports customizing the ActivitySource name
- Automatically names Activities based on class and method name
- Handles exceptions by marking Activities with error status
- Thread-safe Activity source management

Example Usage:
```csharp
[TraceMethod]
public void ProcessOrder(int orderId)
{
    // Method will be automatically traced
    // ...
}

[TraceMethod(sourceName: "CustomSource")]
public void ImportantOperation()
{
    // Method will be traced with a custom source name
    // ...
}
```

#### TraceParameterAttribute

`TraceParameterAttribute` allows marking specific method parameters to be included in the trace. When used in conjunction with `TraceMethodAttribute`, this attribute identifies which parameters should be captured as tags in the Activity.

Example Usage:
```csharp
[TraceMethod]
public void ProcessPayment(
    [TraceParameter] string transactionId,
    [TraceParameter] decimal amount,
    CreditCardInfo cardInfo) // Not traced for security reasons
{
    // Only transactionId and amount will be added to the trace
    // cardInfo is not traced as it may contain sensitive information
    // ...
}
```

### Integration with OpenTelemetry

The Observability components integrate with OpenTelemetry to provide:

- Distributed tracing
- Correlation of related operations across services
- Performance monitoring
- Error tracking and diagnostics

### PostSharp Integration

The tracing implementation uses [PostSharp](https://www.postsharp.net/) to apply method boundary aspects, which enables the automatic tracing without manual instrumentation in each method.

### Best Practices

1. Use `TraceMethodAttribute` for important methods that provide business value when traced
2. Use `TraceParameterAttribute` only on non-sensitive parameters to avoid exposing secrets
3. Consider performance impact when tracing high-frequency methods
4. Configure appropriate sampling in production environments
