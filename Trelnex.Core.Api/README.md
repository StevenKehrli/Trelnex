# Trelnex.Core.Api

A comprehensive .NET API framework that provides essential building blocks for creating robust, secure, and observable web APIs.

## Overview

Trelnex.Core.Api is a library that simplifies the development of ASP.NET Core web APIs by providing pre-configured components for:

- Authentication and authorization
- Configuration management
- Request context handling
- Exception handling
- Health checks
- Observability (Prometheus metrics and OpenTelemetry)
- Logging with Serilog

## Features

### Application Bootstrapping

The `Application` class provides a simple way to bootstrap your API application with a single call:

```csharp
Application.Run(
    args,
    (services, configuration, logger) => 
    {
        // Add your application-specific services here
    },
    app => 
    {
        // Configure your application-specific endpoints here
    },
    (healthChecks, configuration) => 
    {
        // Add your application-specific health checks here
    }
);
```

### Configuration

Automatically loads configuration from multiple sources in the following order:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. `appsettings.User.json`
4. Environment variables

```csharp
// Add configuration to your WebApplicationBuilder
builder.AddConfiguration();
```

### Authentication and Authorization

Simple configuration for authentication:

```csharp
// Add authentication with permissions
services.AddAuthentication(configuration)
    .AddPermissions("your-permission-scheme")
    .AddJwtBearer(/* ... */);

// Or disable authentication for development
services.NoAuthentication();
```

### Observability

Built-in support for Prometheus metrics and OpenTelemetry:

```csharp
// Add observability to your services
services.AddObservability(configuration);

// Enable observability in your application
app.UseObservability();
```

Configuration example in appsettings.json:

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
      "ServiceName": "YourServiceName",
      "ServiceVersion": "1.0.0",
      "Sources": [ "YourSource.ActivitySource" ]
    }
  }
}
```

### Health Checks

Built-in health check endpoints with Prometheus integration:

```csharp
// Map health check endpoints
app.MapHealthChecks();
```

## Getting Started

1. Add the Trelnex.Core.Api package to your project:

```xml
<ItemGroup>
    <PackageReference Include="Trelnex.Core.Api" Version="x.y.z" />
</ItemGroup>
```

2. Create a Program.cs file with the following structure:

```csharp
using Trelnex.Core.Api;

Application.Run(
    args,
    (services, configuration, logger) =>
    {
        // Register your services
        services.AddAuthentication(configuration)
            .AddPermissions("your-permissions-scheme")
            .AddJwtBearer(/* ... */);
            
        // Add your controllers/endpoints
        services.AddControllers();
    },
    app =>
    {
        // Map your controllers/endpoints
        app.MapControllers();
    },
    (healthChecks, configuration) =>
    {
        // Add custom health checks
        healthChecks.AddCheck("example", () => HealthCheckResult.Healthy());
    }
);
```
