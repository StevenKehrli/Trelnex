# Identity

The Identity directory provides a flexible credential management system for handling various authentication providers and access tokens across different cloud platforms and services.

## Core Components

### Credential System

- **ICredential**: Base interface for credential objects that can provide access tokens for specified scopes.
- **ICredentialProvider**: Generic interface to obtain credentials and access token providers for different cloud platforms.
- **AccessToken**: Represents an authentication token with metadata such as expiration time and token type.
- **AccessTokenProvider**: Manages the retrieval of access tokens for specific client types and scopes.

### Status Monitoring

- **AccessTokenHealth**: Enum to track token validity status (Valid, Expired).
- **AccessTokenStatus**: Record containing detailed token status information including health, scopes, and expiration.
- **CredentialStatus**: Aggregates the status of all access tokens for a credential.

### Health Checks Integration

- **CredentialStatusHealthCheck**: ASP.NET Core health check that monitors credential validity.
- **HealthChecksExtensions**: Extensions to register credential health checks with the application.

### Dependency Injection Support

- **CredentialProviderExtensions**: Provides DI extensions for registering and retrieving credential providers.

## Features

- **Cloud-Agnostic Design**: The architecture supports credentials for multiple cloud providers through a unified interface.
- **Token Lifecycle Management**: Automatic tracking of token expiration and health status.
- **Health Monitoring**: Built-in health checks to monitor credential validity at runtime.
- **Strongly-Typed Clients**: Generic support for client-specific token providers.
- **Service Registration**: DI extensions for simplified setup in ASP.NET Core applications.

## Usage

```csharp
// Register a credential provider
services.AddCredentialProvider(myCredentialProvider);

// Retrieve an access token provider for a specific client type
var tokenProvider = credentialProvider.GetAccessTokenProvider<MyApiClient>("my-api-scope");

// Get an access token
AccessToken token = tokenProvider.GetAccessToken();

// Use the token in HTTP requests
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
    token.TokenType, token.Token);
```

The identity system is designed to work with the specific credential implementations provided in cloud-specific packages like Trelnex.Core.Amazon and Trelnex.Core.Azure.
