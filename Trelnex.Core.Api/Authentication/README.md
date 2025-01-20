# Authentication and Authorization

## AuthenticationExtensions.AddAuthentication

Add Authentication and Authorization to the `IServiceCollection`.

    builder.Services.AddAuthentication(builder.Configuration)

Create and inject the `ISecurityProvider`.

The security provider is used by the Swagger `SecurityFilter` to get the collection of `ISecurityDefinition` to add to the Swagger documentation.

The security provider is used by the Swagger `AuthorizeFilter` to get the collection of `ISecurityRequirement` to add to the Swagger operation (endpoint) documentation.

Create and return the `IPermissionsBuilder`.

## IPermissionsBuilder.AddPermissions

Add permissions to the `IPermissionsBuilder`.

    permissionsBuilder.AddPermissions<T>() where T : IPermission

Generally, our `IPermission` are derived from `MicrosoftIdentityPermission` which protects the endpoint with Authentication backed by Microsoft Entra ID.

Create an instance of the specified `IPermission`.

Adds the Authentication to the `IServiceCollection`.

    permission.AddAuthentication(services, configuration)

Generally, since our `IPermission` are derived from `MicrosoftIdentityPermission`, this calls `AddMicrosoftIdentityWebApiAuthentication`.

Creates and adds the `ISecurityDefinition` that represents the `IPermission` to the `ISecurityProvider`.

    securityProvider.AddSecurityDefinition(securityDefinition)

Creates an `IPoliciesBuilder`.

Adds the Authorization to the `IPoliciesBuilder`.

    permission.AddAuthorization(policiesBuilder)

### IPoliciesBuilder.AddPolicy

Add the `IPermissionPolicy` to the `IPoliciesBuilder`.

    policiesBuilder.AddPolicy<T>() where T : IPermissionPolicy

Create an instance of the specified `IPermissionPolicy`.

Creates and adds the `ISecurityRequirement` that represents the `IPermissionPolicy` to the `ISecurityProvider`.

    securityProvider.AddSecurityRequirement(securityRequirement);

## IPermissionsBuilder.AddPermissions - Continued

Add the authorization policies from `IPoliciesBuilder` to the `IServiceCollection`.

    services.AddAuthorization(policiesBuilder.Build);
