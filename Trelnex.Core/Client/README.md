# Client

This directory contains the core components for building HTTP clients in the Trelnex framework.

## Overview

The `Trelnex.Core.Client` namespace provides a standardized approach to creating HTTP clients for service-to-service communication. It includes:

- A base client class with common HTTP functionality
- Extension methods for URI manipulation and HTTP headers
- Configuration structures
- DI (dependency injection) integration

## Key Components

### BaseClient

The `BaseClient` abstract class serves as the foundation for all HTTP clients in the Trelnex ecosystem. It provides:

- Standardized methods for HTTP operations (GET, POST, PUT, PATCH, DELETE)
- Consistent error handling
- JSON serialization/deserialization
- Header management

```csharp
// Example usage in a derived client
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

### UriExtensions

Provides extension methods for manipulating URIs:

- `AppendPath`: Adds a path segment to an existing URI
- `AddQueryString`: Adds a query parameter to an existing URI

### HeadersExtensions

Provides extension methods for HTTP headers:

- `AddAuthorizationHeader`: Adds an authorization header to a request

### ClientConfiguration

A record that defines the configuration parameters required for HTTP clients:

- `CredentialProviderName`: The name of the credential provider to use
- `Scope`: The OAuth scope required for access
- `BaseAddress`: The base URL for the service

### ClientExtensions

Extension methods for registering HTTP clients with the dependency injection container:

- `AddClient<IClient, TClient>`: Registers a client interface and implementation

## Setup and Configuration

To register a client in your application:

```csharp
// In your startup/program class
services.AddClient<IMyClient, MyClient>(configuration);
```

Configuration example in appsettings.json:

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

## Error Handling

The `BaseClient` provides standardized error handling using `HttpStatusCodeException`. Custom error handlers can be provided to each request method to parse service-specific error responses.

## Authentication

Authentication is handled through credential providers, which supply access tokens. The client automatically uses the configured credential provider to obtain the necessary tokens for each request.