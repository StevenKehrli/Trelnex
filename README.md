# Trelnex.Core

Trelnex.Core is a REST API framework that demonstrates the following concepts:

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Vertical Slice Architecture

<details>

<summary>Expand</summary>

&nbsp;

[https://www.jimmybogard.com/vertical-slice-architecture/](https://www.jimmybogard.com/vertical-slice-architecture/)

**Vertical Slice Architecture** is a software design approach that focuses on organizing a system into small, self-contained slices that represent complete functionalities. Each slice encompasses the REST API, business logic, and data access.

Each slice can be developed and iterated upon independently. This promotes better modularity, enhances maintainability, and facilitates easier testing and deployment.

### Vertical Slice Example

Consider the below [Vertical Slice Code](#vertical-slice-code) for an API to create a user: `POST /users`.

The REST API is defined in the `Map` method where it maps the `POST /users` endpoint into `IEndpointRouteBuilder`.

The business logic and data access are defined in the `HandleRequest` method.

1. Create a new user id
2. Create the new `IUser` DTO using the `IDataProvider<IUser>`. See [Command Pattern](#command-pattern) for more information.
3. Set the user name.
4. Save the `IUser` DTO to the data store.
5. Convert the `IUser` DTO to a `UserModel` and return.

This is a small, self-contained slice that represents the complete functionality to create a user.

In addition, it is easily tested by calling the `HandleRequest` method. See [Reqnroll](#reqnroll) for more information.

### Vertical Slice Code

```csharp
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Core;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.Responses;
using Trelnex.Core.Data;
using Trelnex.Users.Api.Objects;
using Trelnex.Users.Client;

namespace Trelnex.Users.Api.Endpoints;

internal static class CreateUserEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapPost(
                "/users",
                HandleRequest)
            .RequirePermission<UsersPermission.UsersCreatePolicy>()
            .Accepts<CreateUserRequest>(MediaTypeNames.Application.Json)
            .Produces<UserModel>()
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status401Unauthorized)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status403Forbidden)
            .WithName("CreateUser")
            .WithDescription("Creates a new user")
            .WithTags("Users");
    }

    public static async Task<UserModel> HandleRequest(
        [FromServices] IDataProvider<IUser> userProvider,
        [FromServices] IRequestContext requestContext,
        [AsParameters] RequestParameters parameters)
    {
        // create a new user id
        var id = Guid.NewGuid().ToString();
        var partitionKey = id;

        // create the user dto
        var userCreateCommand = userProvider.Create(
            id: id,
            partitionKey: partitionKey);

        userCreateCommand.Item.UserName = parameters.Request.UserName;

        // save in data store
        var userCreateResult = await userCreateCommand.SaveAsync(requestContext, default);

        // return the user model
        return userCreateResult.Item.ConvertToModel();
    }

    public class RequestParameters
    {
        [FromBody]
        public required CreateUserRequest Request { get; init; }
    }
}
```

</details>

## Authentication and Authorization

<details>

<summary>Expand</summary>

&nbsp;

[Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-8.0) and [Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-8.0) are well documented.

The challenge is implementing [Policy based role checks](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-8.0#policy-based-role-checks). The documentation highlights several problems:

- When creating the policy, the policy name `RequireAdministratorRole` and its required role `Administrator` are magic strings.
- When referencing that policy in the `AuthorizeAttribute` we again see the `RequireAdministratorRole` magic string.
- It is a challenge to add the security schemes and security requirements to the OpenAPI specification (Swagger).

Trelnex.Core.Api exposes a friendlier approach to implementing RBAC that solves these problems.

Trelnex.Core.Api currently supports Microsoft Identity Web App Authentication and JWT Bearer Authentication. This is easily extensible to support any authentication / authorization provider.

### Configuration

#### Configuration - Microsoft Identity Web App Authentication

`appsettings.json` specifies the configuration for Microsoft Identity Web App Authentication.
  - `TenantId` - Your Azure subscription Microsoft Entra Tenant ID.
  - `ClientId` - Your Azure App Registration Application (Client) ID.
  - `Audience` - Your Azure App Registration Application ID URI (from Expose an API)
  - `Scope` - The Scope defined by your Azure App Registration API (from Expose an API)

```json
  "Auth": {
    "trelnex-api-users": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "d3ec543c-3a0b-4e07-9992-598e311c8ee5",
      "ClientId": "9d931409-5ed7-4917-bf90-56b1b13e4830",
      "Audience": "api://9d931409-5ed7-4917-bf90-56b1b13e4830",
      "Scope": "users"
    }
  }
```

#### Configuration - JWT Bearer Authentication

This example demonstrates the `Trelnex.Auth.Amazon` OAuth 2.0 Authorization Server configuration. Other providers (such as Okta) are supported.

`appsettings.json` specifies the configuration for JWT Bearer Authentication.
  - `Audience` - Your Resource Name registered with your `Trelnex.Auth.Amazon` instance.
  - `Authority` - The URI of your `Trelnex.Auth.Amazon` instance.
  - `MetadataAddress` - The URI of you Well-Known Configuration endpoint.
  - `Scope` - The Scope registered with your `Trelnex.Auth.Amazon` instance.

```json
  "Auth": {
    "trelnex-api-users": {
      "Audience": "api://9d931409-5ed7-4917-bf90-56b1b13e4830",
      "Authority": "https://amazon.auth.trelnex.com",
      "MetadataAddress": "https://amazon.auth.trelnex.com/.well-known/openid-configuration",
      "Scope": "users"
    }
  }
```

### Permission and Policy Definition

The below code defines two policies:

- `UsersCreatePolicy` with required role `users.create`
- `UsersReadPolicy` with required role `users.read`

The first example uses Microsoft Identity Web App Authentication and the second example uses JWT Bearer Authentication. Notice both examples are nearly identical, with the only difference are the reference to `MicrosoftIdentityPermission` or `JwtBearerPermission` base class.

#### Permission and Policy Definition - Microsoft Identity Web App Authentication

These two policies are exposed through the `UsersPermission` which is an implementation of `MicrosoftIdentityPermission` (Microsoft Identity Web App Authentication). This permission uses the JWT Bearer scheme `Bearer.trelnex-api-users` and its necessary configuration is found in the `Auth:trelnex-api-users` section.

```csharp
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Users.Api.Endpoints;

internal class UsersPermission : MicrosoftIdentityPermission
{
    protected override string ConfigSectionName => "Auth:trelnex-api-users";

    public override string JwtBearerScheme => "Bearer.trelnex-api-users";

    public override void AddAuthorization(
        IPoliciesBuilder policiesBuilder)
    {
        policiesBuilder
            .AddPolicy<UsersCreatePolicy>()
            .AddPolicy<UsersReadPolicy>();
    }

    public class UsersCreatePolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => ["users.create"];
    }

    public class UsersReadPolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => ["users.read"];
    }
}
```

#### Permission and Policy Definition - JWT Bearer Authentication

These two policies are exposed through the `UsersPermission` which is an implementation of `JwtBearerPermissions` (JWT Bearer Authentication). This permission uses the JWT Bearer scheme `Bearer.trelnex-api-users` and its necessary configuration is found in the `Auth:trelnex-api-users` section.

```csharp
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Users.Api.Endpoints;

internal class UsersPermission : MicrosoftIdentityPermission
{
    protected override string ConfigSectionName => "Auth:trelnex-api-users";

    public override string JwtBearerScheme => "Bearer.trelnex-api-users";

    public override void AddAuthorization(
        IPoliciesBuilder policiesBuilder)
    {
        policiesBuilder
            .AddPolicy<UsersCreatePolicy>()
            .AddPolicy<UsersReadPolicy>();
    }

    public class UsersCreatePolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => ["users.create"];
    }

    public class UsersReadPolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => ["users.read"];
    }
}
```

### Authentication and Authorization Injection

The below code injects the `UsersPermission` and its two policies: `UsersCreatePolicy` and `UsersReadPolicy`.

```csharp
    public static void Add(
        IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        services
            .AddAuthentication(configuration)
            .AddPermissions(bootstrapLogger);
    }
```

```csharp
    private static IPermissionsBuilder AddPermissions(
        this IPermissionsBuilder permissionsBuilder,
        ILogger bootstrapLogger)
    {
        permissionsBuilder
            .AddPermissions<UsersPermission>(bootstrapLogger);

        return permissionsBuilder;
    }
```

### Authentication and Authorization Usage

The `RequirePermission<IPermissionPolicy>` extension method adds the specified authorization policy (`UsersCreatePolicy`) to the endpoint(s).

The endpoint will now require authentication and authorization of the required role (`users.create`) to access the endpoint.

```csharp
    erb.MapPost(
            "/users",
            HandleRequest)
        .RequirePermission<UsersPermission.UsersCreatePolicy>()
```

### Swagger Security Schemes and Security Requirements

An `ISecurityProvider` instance is created an inject during the `AddAuthentication` method. This `ISecurityProvider` exposes the security schemes and security requirements that were created when injecting the permissions and their policies during the `AddPermissions` method.

The `ISecurityProvider` instance is referenced by:

- `SecurityFilter : IDocumentFilter` to add the security schemes to the `OpenApiDocument`
- `AuthorizeFilter : IOperationFilter` to add the security requirements to the `OpenApiOperation`.

### More Details in Authentication and Authorization Implementation

See [Authentication and Authorization](Trelnex.Core.Api/Authentication/README.md) for more information.

</details>

## Command Pattern

<details>

<summary>Expand</summary>

&nbsp;

[https://en.wikipedia.org/wiki/Command_pattern](https://en.wikipedia.org/wiki/Command_pattern)

The REST APIs expose five basic operations: create, read, update, delete, and query. The data access supports those five basic operations.

These data access operations and the DTOs on which they operate are encapsulated in a command:

- `ISaveCommand` for create, update, and delete
- `IBatchCommand` for batch
- `IReadResult` for read
- `IQueryCommand` for query

This encapsulation ensures data integrity of the DTO. In addition, the command can invoke related business logic, such as creating and saving an audit event within `ISaveCommand`.

### IDataProvider

An `IDataProvider` exposes the commands against a backing data store. This is easily extensible to support other data stores. Trelnex.Core.Data implements a data provider for an in-memory data store for development and testing. Trelnex.Core.Amazon implements data providers for DynamoDB and Postgres. Trelnex.Core.Azure implements data providers for CosmosDB NoSQL and SqlServer.

The `IDataProvider` interface defines five methods:

- `ISaveCommand<TInterface> Create()`: create an `ISaveCommand<TInterface>` to create a new item
- `ISaveCommand<TInterface>?> UpdateAsync()`: create an `ISaveCommand<TInterface>` to update the specified item
- `ISaveCommand<TInterface>?> DeleteAsync()`: create an `ISaveCommand<TInterface>` to delete the specified item

- `IBatchCommand<TInterface>?> Batch()`: create an `IBatchCommand<TInterface>` to save a batch of `ISaveCommand<TInterface>`

- `IReadResult<TInterface>?> ReadAsync()`: read the specified item

- `IQueryCommand<TInterface> Query()`: create a LINQ query for items

### ISaveCommand\<TInterface\>

The `ISaveCommand<TInterface>` interface defines one property and two methods:

- `TInterface Item`: the item to create, update, or delete
- `IReadResult<TInterface> SaveAsync()`: save the item and return the saved item as a `IReadResult<TInterface>`
- `ValidationResult ValidateAsync()`: validate the item

### IBatchCommand\<TInterface\>

The `IBatchCommand<TInterface>` interface defines three methods:

- `IBatchCommand<TInterface> Add(ISaveCommand<TInterface> saveCommand)`: add the specified `ISaveCommand<TInterface>` to the batch
- `IBatchResult<TInterface>[] SaveAsync()`: save the batch and return the saved items as an array of `IBatchResult<TInterface>`
- `ValidationResult[] ValidateAsync()`: validate the batch

### IReadResult\<TInterface\>

The `IReadResult<TInterface>` interface defines one property and one method:

- `TInterface Item`: the item read
- `ValidationResult ValidateAsync()`: validate the item

### IBatchResult\<TInterface\>

The `IBatchResult<TInterface>` interface defines two properties:

- `HttpStatusCode`: the status code of this save
- `IReadResult<TInterface>`: the saved item, if the save was successful

### IQueryCommand\<TInterface\>

The `IQueryCommand<TInterface>` interface defines methods for building and executing queries:

- `IQueryCommand<TInterface> OrderBy()`: adds ascending sort by the specified key
- `IQueryCommand<TInterface> OrderByDescending()`: adds descending sort by the specified key
- `IQueryCommand<TInterface> Skip()`: skips a specified number of items
- `IQueryCommand<TInterface> Take()`: takes a maximum number of items
- `IQueryCommand<TInterface> Where()`: adds a predicate to filter the items
- `IAsyncDisposableEnumerable<IQueryResult<TInterface>> ToAsyncDisposableEnumerable()`: executes the query with lazy async enumeration and automatic disposal management
- `Task<IDisposableEnumerable<IQueryResult<TInterface>>> ToDisposableEnumerableAsync()`: executes the query with eager materialization and automatic disposal management

#### Query Execution Patterns

**Lazy Async Enumeration** - Items are materialized one by one as enumerated:
```csharp
using var lazyResults = queryCommand.ToAsyncDisposableEnumerable();
await foreach (var item in lazyResults)
{
}
// All enumerated items are automatically disposed when lazyResults goes out of scope
```

**Eager Materialization** - All items are loaded upfront with array-like access:
```csharp
using var eagerResults = await queryCommand.ToDisposableEnumerableAsync();
foreach (var item in eagerResults)
{
}
// All materialized items are automatically disposed when eagerResults goes out of scope
```

Both patterns automatically dispose all materialized items when the returned enumerable is disposed.

### ISaveCommand\<TInterface\> vs IBatchCommand\<TInterface\>

`ISaveCommand<TInterface>` operates on a single item, whereas `IBatchCommand<TInterface>` operates on a batch of items.

If `ISaveCommand<TInterface>` faults, it will throw an exception:

- `ValidationException`: The item failed validation
- `CommandException`: The item failed to save

    - `Conflict`: The item conflicts with an existing item in the backing store
    - `PreconditionFailed`: The item has a different version from the version available in the backing store

Otherwise, `ISaveCommand<TInterface>` will return an `IReadResult<TInterface>`.

If `IBatchCommand<TInterface>` faults, it will throw an exception:

- `ValidationException`: One or more items in the batch failed validation

Otherwise, `IBatchCommand<TInterface>` will return an array of `IBatchResult<TInterface>`. Each `IBatchResult<TInterface>` corresponds to the respective `ISaveCommand<TInterface>` in the batch.

`IBatchResult<TInterface>`:

- `HttpStatusCode`:

    - `OK`: The save was successful
    - `BadRequest`: The save command is not valid
    - `Conflict`: The item conflicts with an existing item in the backing store
    - `FailedDependency`: An item in the batch faulted
    - `PreconditionFailed`: The item has a different version from the version available in the backing store

- `ReadResult`: the saved item, if the save was successful

### TInterface vs TItem

All generic type definitions above are `TInterface` which is constrained to `IBaseItem`, where `IBaseItem` is an `interface.`

Using `TInterface` (an `interface`) instead of `TItem` (a `class` that implements `TInterface`) enforces the data integrity of the DTO.

The `IBaseItem` interface does not expose any set accessors for its properties. This makes it impossible for a developer to set a property incorrectly. Instead, it is the responsibility of the `IDataProvider` to set these properties correctly.

### DispatchProxy

The `TInterface Item` property on each command is a [DispatchProxy](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.dispatchproxy?view=net-8.0). This allows us to handle any method dispatch (generally, the set accessor) on the item to enforce its data integrity.

For example, the `TInterface Item` for `IReadResult<TInterface>` is set as read-only. Any call to a set accessor will throw an `InvalidOperationException`.

### TrackChangeAttribute

The `TrackChangeAttribute` on any property informs the `DispatchProxy` to track any changes to that property value. These changes are then added to the audit event that is created and saved within `ISaveCommand`.

### Usage

#### TInterface and TItem

Create a new `TInterface` and `TItem`. Notice the `TrackChangeAttribute` and `JsonPropertyNameAttribute` are on the `TItem`. The `TItem` is the DTO for the `IDataProvider`; the `TInterface` is the proxy for the developer.

```csharp
internal interface ITestItem : IBaseItem
{
    string PublicMessage { get; set; }

    string PrivateMessage { get; set; }
}

internal class TestItem : BaseItem, ITestItem
{
    [TrackChange]
    [JsonPropertyName("publicMessage")]
    public string PublicMessage { get; set; } = null!;

    [JsonPropertyName("privateMessage")]
    public string PrivateMessage { get; set; } = null!;
}
```

#### Create an Item

Call the `IDataProvider` `Create()` method to create the `ISaveCommand<TInterface>`.

```csharp
    var createCommand = dataProvider.Create(
        id: id,
        partitionKey: partitionKey);

    createCommand.Item.PublicMessage = "Public #1";
    createCommand.Item.PrivateMessage = "Private #1";
```

### Save the Item

Call the `SaveAsync()` method to save the item. This returns an `IReadResult<TInterface>` of the saved item.

```csharp
    var result = await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);
```

#### Behind the Scenes

The `IDataProvider` will save the item to the backing data store.

```json
{
    "Id": "0346bbe4-0154-449f-860d-f3c1819aa174",
    "PartitionKey": "c8a6b519-3323-4bcb-9945-ab30d8ff96ff",
    "TypeName": "test-item",
    "CreatedDateTimeOffset": "2025-05-21T05:08:15.717886+00:00",
    "UpdatedDateTimeOffset": "2025-05-21T05:08:15.717886+00:00",
    "DeletedDateTimeOffset": null,
    "IsDeleted": null,
    "ETag": "e7622db2-c465-44bc-9d0a-e643882e8f38",
    "PublicMessage": "Public #1",
    "PrivateMessage": "Private #1"
}
```

It will concurrently save an audit event to the backing data store.

Notice the `Changes` element includes a property change for `publicMessage` from `null` to `Public #1`. The `PublicMessage` property is decorated with the `TrackChangeAttribute`.

Notice the `Changes` element does not include a property change for `privateMessage`. The `PrivateMessage` property is not decorated with the `TrackChangeAttribute`.

```json
{
    "SaveAction": "CREATED",
    "RelatedId": "0346bbe4-0154-449f-860d-f3c1819aa174",
    "RelatedTypeName": "test-item",
    "Changes": [
      {
        "PropertyName": "publicMessage",
        "OldValue": null,
        "NewValue": "Public #1"
      }
    ],
    "Context": {
      "ObjectId": "7254e574-a446-48f7-b07c-ac34ace801c2",
      "HttpTraceIdentifier": "fa1c0011-f536-427b-a091-07e39a9bc192",
      "HttpRequestPath": "5fcdd52c-acdc-4f10-b7f4-17d056c57697"
    },
    "Id": "6e00fae0-2229-42e2-99b8-9ca14fdaa5f1",
    "PartitionKey": "c8a6b519-3323-4bcb-9945-ab30d8ff96ff",
    "TypeName": "event",
    "CreatedDateTimeOffset": "2025-05-21T05:08:15.717886+00:00",
    "UpdatedDateTimeOffset": "2025-05-21T05:08:15.717886+00:00",
    "DeletedDateTimeOffset": null,
    "IsDeleted": null,
    "ETag": "2cc85775-2d92-49f7-acf2-7abb17d46227"
  }
```

</details>

## Data Providers

<details>

<summary>Expand</summary>

&nbsp;

### InMemoryDataProvider

`InMemoryDataProvider` is an `IDataProvider` that uses memory as a temporary backing store. It does not provide persistence. It is intended to assist development and testing of their business logic.

#### InMemoryDataProvider - Dependency Injection

The `AddInMemoryDataProviders` method takes a `Action<IDataProviderOptions>` `configureDataProviders` delegate. This delegate will configure any necessary [Data Providers](#idataprovider) for the application.

In this example, we configure a data provider for the `IUser` interface and its `User` DTO.

```csharp
    public static void Add(
        IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        services
            .AddAuthentication(configuration)
            .AddPermissions(bootstrapLogger);

        services
            .AddSwaggerToServices()
            .AddInMemoryDataProviders(
                configuration,
                bootstrapLogger,
                options => options.AddUsersDataProviders());
    }
```

```csharp
    public static IDataProviderOptions AddUsersDataProviders(
        this IDataProviderOptions options)
    {
        return options
            .Add<IUser, User>(
                typeName: "user",
                validator: User.Validator,
                commandOperations: CommandOperations.All);
    }
```

</details>

## Creating a New Web Application

<details>

<summary>Expand</summary>

&nbsp;

Much of an ASP.NET Core application startup is boilerplate: Serilog, configuration, exception handlers, metrics, Swagger, etc.

Trelnex.Core.Api handles this boilerplate, leaving the developer to focus on the business logic: [Authentication and Authorization](#authentication-and-authorization), [Data Providers](#idataprovider), and the endpoints.

### Application.Run

The `Application.Run` method takes four parameters:

- `args`: the command line arguments
- `addApplication`: the delegate to inject necessary services to `IServiceCollection`
- `useApplication`: the delegate to add the endpoints to the `WebApplication`
- `addHealthChecks`: an optional delegate to inject additional health checks to the `IServiceCollection`


```csharp
Application.Run(args, UsersApplication.Add, UsersApplication.Use);
```

### addApplication Delegate

This delegate is called to inject necessary services to `IServiceCollection`. This is generally [Authentication and Authorization](#authentication-and-authorization), [Data Providers](#idataprovider), and Swagger.

```csharp
    public static void Add(
        IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        services
            .AddAuthentication(configuration)
            .AddPermissions(bootstrapLogger);

        services
            .AddSwaggerToServices()
            .AddCosmosDataProviders(
                configuration,
                bootstrapLogger,
                options => options.AddUsersDataProviders());
    }
```

### useApplication Delegate

This delegate is called to configure the `WebApplication`. This is generally Swagger and the endpoints.


```csharp
    public static void Use(
        WebApplication app)
    {
        app
            .AddSwaggerToWebApplication()
            .UseEndpoints();
    }

    private static IEndpointRouteBuilder UseEndpoints(
        this IEndpointRouteBuilder erb)
    {
        CreateUserEndpoint.Map(erb);
        GetUserEndpoint.Map(erb);

        return erb;
    }
```

</details>

## Reqnroll

<details>

<summary>Expand</summary>

&nbsp;

[Reqnroll](https://docs.reqnroll.net/latest/index.html) is a powerful BDD (Behavior-Driven Development) framework using Gherkin to describe test cases.

The [Vertical Slice Architecture](#vertical-slice-architecture) simplifies this integration testing of the HTTP REST APIs. Instead of invoke the HTTP endpoint, we invoke the `HandleRequest` method.

See [Trelnex.Samples](https://github.com/StevenKehrli/Trelnex.Samples?tab=readme-ov-file#trelnexintegrationtests) for more information.

</details>
