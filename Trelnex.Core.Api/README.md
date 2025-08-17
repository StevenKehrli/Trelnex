# Trelnex.Core.Api

Trelnex.Core.Api is a comprehensive .NET framework that provides essential building blocks for developing robust, secure, and observable ASP.NET Core web APIs. It implements standardized patterns for authentication, configuration, health monitoring, observability, and exception handling to accelerate API development and promote best practices.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Key Features

- **Standardized Application Setup** - Streamlined API initialization with common middleware and best practices
- **Authentication Framework** - Flexible, policy-based security with JWT Bearer and Microsoft Identity integration
- **Observability** - Built-in support for metrics, tracing, and health checks
- **Swagger Integration** - Automatic API documentation with security requirements
- **Exception Handling** - Consistent error responses for better API usability
- **Health Monitoring** - Comprehensive health checks with Prometheus integration
- **Configuration Management** - Layered, environment-aware configuration system
- **Request Context** - Thread-safe access to request metadata without HttpContext coupling
- **Client Support** - Simplified HTTP client configuration with authentication
- **Data Provider Integration** - Built-in support for configurable event tracking with EventPolicy

## Architecture

Trelnex.Core.Api is designed around several core concepts:

### Application Bootstrap

The `Application` class provides a standardized way to configure and run ASP.NET Core applications with consistent middleware and services. It handles configuration, dependency injection, logging, health checks, authentication, observability, and exception handling.

```csharp
// Simple program.cs with standardized middleware
Application.Run(args,
    // Register application services
    (services, configuration, logger) =>
    {
        services
            .AddAuthentication(configuration)
            .AddPermissions<YourPermission>(logger);

        services.AddSwaggerToServices();
    },
    // Configure application endpoints
    app =>
    {
        app.AddSwaggerToWebApplication();

        app.MapGet("/data", () => "Hello World")
            .RequirePermission<ReadDataPolicy>();
    });
```

### Authentication System

The authentication system provides a strongly-typed, policy-based approach to securing API endpoints. It supports both JWT Bearer and Microsoft Identity authentication with integrated Swagger documentation. See [Authentication Documentation](Authentication/README.md) for details.

### Observability

The framework includes comprehensive observability features:

- **Prometheus Metrics** - Exposes HTTP metrics and health check status as Prometheus metrics
- **OpenTelemetry** - Distributed tracing with automatic instrumentation
- **Health Checks** - Customizable health monitoring with JSON formatting

### Exception Handling

The `HttpStatusCodeExceptionHandler` provides standardized error responses following RFC 7807 (Problem Details for HTTP APIs), converting exceptions to structured JSON with appropriate HTTP status codes.

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid input provided",
  "instance": "/api/resources/123",
  "id": ["The ID must be a valid GUID"],
  "name": ["The name field is required"]
}
```

### Configuration System

The configuration system uses a layered approach with well-defined precedence:

1. Base settings from `appsettings.json`
2. Environment-specific settings (`appsettings.Development.json`, etc.)
3. User-specific settings (`appsettings.User.json`) for local development
4. Environment variables (highest precedence)

### Data Provider Integration

The framework integrates with Trelnex.Core.Data data providers for data access:

```csharp
// Register data provider factories
services.AddInMemoryDataProviders(
    configuration,
    logger,
    options => options.AddCustomerDataProviders());

// Example customer data providers with EventPolicy
public static IDataProviderOptions AddCustomerDataProviders(
    this IDataProviderOptions options)
{
    return options
        .Add<Customer>(
            typeName: "customer",
            itemValidator: Customer.Validator,
            commandOperations: CommandOperations.All);
}
```

## Configuration

### Service Configuration

The `ServiceConfiguration` section is required in application settings:

```json
{
  "ServiceConfiguration": {
    "FullName": "YourService",
    "DisplayName": "Your Service Name",
    "Version": "1.0.0",
    "Description": "Description of your service."
  }
}
```

### Observability Configuration

Optional configuration for observability features:

```json
{
  "Observability": {
    "Prometheus": {
      "Enabled": true,
      "Url": "/metrics",
      "Port": 9090
    },
    "OpenTelemetry": {
      "Enabled": true,
      "Sources": ["YourService.*"]
    }
  }
}
```

### HTTP Client Configuration

Configuration for typed HTTP clients:

```json
{
  "Clients": {
    "ExampleClient": {
      "BaseAddress": "https://api.example.com",
      "Authentication": {
        "CredentialProviderName": "DefaultCredentialProvider",
        "Scope": "api://example/access"
      }
    }
  }
}
```

## Usage

### Using the Application Class

For simple setup, use the `Application` class:

```csharp
using Trelnex.Core.Api;

Application.Run(args,
    // Register services
    (services, configuration, logger) =>
    {
        services
            .AddAuthentication(configuration)
            .AddPermissions<YourPermission>(logger);

        services.AddSwaggerToServices();

        // Register typed HTTP clients
        services.AddClient(configuration, new YourClientFactory());
    },
    // Configure endpoints
    app =>
    {
        app.AddSwaggerToWebApplication();

        app.MapGet("/data", () => "Secured data")
            .RequirePermission<ReadDataPolicy>();
    });
```

### Registering HTTP Clients

```csharp
// Define client interface and factory
public interface IExampleClient
{
    Task<string> GetDataAsync();
}

public class ExampleClientFactory : IClientFactory<IExampleClient>
{
    public string Name => "ExampleClient";

    public IExampleClient Create(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider)
    {
        return new ExampleClient(httpClient, accessTokenProvider);
    }
}

// Register the client
services.AddClient<IExampleClient>(configuration, new ExampleClientFactory());

// Use the client in your services
public class YourService
{
    private readonly IExampleClient _client;

    public YourService(IExampleClient client)
    {
        _client = client;
    }

    public async Task ProcessDataAsync()
    {
        var data = await _client.GetDataAsync();
        // Process data
    }
}
```

## Best Practices

1. **Use the Application class** for new projects to get standardized setup and middleware
2. **Create strongly-typed permission policies** rather than using string-based claims
3. **Add health checks** for all external dependencies and critical services
4. **Configure observability** to monitor application performance
5. **Use typed HTTP clients** with the client extensions for external service integration
6. **Implement proper exception handling** with HttpStatusCodeException for RFC 7807-compliant API error responses
7. **Follow layered configuration** with environment-specific settings

## Extension Points

- **Custom Authentication**: Extend `JwtBearerPermission` or `MicrosoftIdentityPermission`
- **Typed HTTP Clients**: Implement `IClientFactory<T>` for custom client creation logic
- **Data Providers**: Register custom data provider factories for data access

## Related Libraries

- **Trelnex.Core**: Core abstractions for HTTP status codes, validation, and identity
- **Trelnex.Core.Data**: Data access library with CRUD operations and change tracking
- **Trelnex.Core.Amazon**: AWS-specific implementations for core services
- **Trelnex.Core.Azure**: Azure-specific implementations for core services
