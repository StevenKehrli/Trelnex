# Authentication and Authorization

The `Trelnex.Core.Api.Authentication` namespace provides a comprehensive framework for implementing and configuring JWT Bearer and Microsoft Identity authentication in ASP.NET Core applications. This system uses a strongly-typed, policy-based approach to authorization that integrates seamlessly with Swagger/OpenAPI documentation.

## Key Components

### Permission Framework

- **IPermission**: Base interface for all authentication schemes
  - **JwtBearerPermission**: Abstract base class for JWT Bearer token authentication
  - **MicrosoftIdentityPermission**: Abstract base class for Microsoft Entra ID (Azure AD) authentication

- **IPermissionPolicy**: Interface defining role-based authorization requirements
  - Used with `RequirePermission<T>()` to protect API endpoints

- **Security Infrastructure**:
  - **SecurityDefinition**: Describes authentication schemes with audience and scope requirements
  - **SecurityRequirement**: Specifies authorization conditions with required roles
  - **SecurityProvider**: Central registry for security definitions and requirements

## Configuration Flow

### 1. Setup Authentication

Register authentication services during application startup:

```csharp
// In Program.cs or Startup.cs
builder.Services
    .AddAuthentication(builder.Configuration)
    .AddPermissions<YourPermission>(logger);
```

This configures:
- HTTP context access for authentication
- Token caching (in-memory)
- Security provider registration
- Permission registration via the PermissionsBuilder

### 2. Define Permissions

Create custom permissions by inheriting from one of the base classes:

```csharp
// For Microsoft Identity (Azure AD)
public class YourPermission : MicrosoftIdentityPermission
{
    protected override string ConfigSectionName => "AzureAd";

    public override string JwtBearerScheme => "Bearer";

    public override void AddAuthorization(IPoliciesBuilder policiesBuilder)
    {
        policiesBuilder
            .AddPolicy<ReadDataPolicy>()
            .AddPolicy<WriteDataPolicy>();
    }
}

// For custom JWT Bearer
public class YourJwtPermission : JwtBearerPermission
{
    protected override string ConfigSectionName => "JwtSettings";

    public override string JwtBearerScheme => "JwtBearer";

    public override void AddAuthorization(IPoliciesBuilder policiesBuilder)
    {
        policiesBuilder
            .AddPolicy<AdminPolicy>();
    }
}
```

### 3. Define Permission Policies

Create policies by implementing the IPermissionPolicy interface:

```csharp
public class ReadDataPolicy : IPermissionPolicy
{
    public string[] RequiredRoles => ["Reader"];
}

public class WriteDataPolicy : IPermissionPolicy
{
    public string[] RequiredRoles => ["Writer"];
}

public class AdminPolicy : IPermissionPolicy
{
    public string[] RequiredRoles => ["Administrator"];
}
```

### 4. Apply Policies to Endpoints

Apply the policies to API endpoints:

```csharp
// For Minimal APIs
app.MapGet("/data", () => "Secure data")
    .RequirePermission<ReadDataPolicy>();

app.MapPost("/data", (Data data) => "Data saved")
    .RequirePermission<WriteDataPolicy>();

// For Controllers
[HttpGet]
[Authorize(Policy = PermissionPolicy.Name<ReadDataPolicy>())]
public IActionResult GetData() { ... }
```

## Configuration Settings

### JWT Bearer Configuration

The following settings are required in the configuration section specified by `ConfigSectionName`:

```json
{
  "JwtSettings": {
    "Authority": "https://your-auth-server.com",
    "Audience": "your-audience-value",
    "MetadataAddress": "https://your-auth-server.com/.well-known/openid-configuration",
    "Scope": "api://your-app-id/access"
  }
}
```

### Microsoft Identity Configuration

The following settings are required for Microsoft Entra ID authentication:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "Audience": "api://your-app-id",
    "Scope": "api://your-app-id/access"
  }
}
```

## Development/Testing Support

For development or testing scenarios, you can disable authentication:

```csharp
// In development environment only
if (builder.Environment.IsDevelopment())
{
    builder.Services.NoAuthentication();
}
else
{
    builder.Services
        .AddAuthentication(builder.Configuration)
        .AddPermissions<YourPermission>(logger);
}
```

## Swagger Integration

The authentication system automatically integrates with Swagger/OpenAPI documentation through:

- **SecurityFilter**: Adds security definitions to the Swagger document
- **AuthorizeFilter**: Adds security requirements to individual API operations

This ensures that the API documentation accurately reflects the authentication and authorization requirements of each endpoint, making it easier for API consumers to understand what credentials they need.

## Implementation Details

### Permission Registration Process

1. **AddAuthentication** creates a SecurityProvider and returns an IPermissionsBuilder
2. **AddPermissions<T>** instantiates the permission and:
   - Calls permission.AddAuthentication to configure JWT or Microsoft Identity
   - Gets audience and scope from the permission
   - Creates a SecurityDefinition and adds it to the SecurityProvider
   - Creates a PoliciesBuilder for the permission
   - Calls permission.AddAuthorization to register policy definitions

### Policy Registration Process

1. **AddPolicy<T>** instantiates the policy and:
   - Gets the policy name from the policy type
   - Creates a SecurityRequirement with scheme, audience, scope, and roles
   - Adds the SecurityRequirement to the SecurityProvider
2. **Build** configures ASP.NET Core authorization policies with:
   - The appropriate authentication scheme
   - Required scope claim
   - Required role claims