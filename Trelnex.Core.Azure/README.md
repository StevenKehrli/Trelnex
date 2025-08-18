# Trelnex.Core.Azure

`Trelnex.Core.Azure` is a .NET library that provides Azure-specific implementations for key components of the Trelnex framework. It includes integration with Azure services for data storage, authentication, and identity management, enabling applications to securely interact with Azure resources.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Key Features

- **Data Access Integration** - SQL Server and Cosmos DB data providers for the Trelnex.Core.Data system
- **Data Provider Factories** - Simplified registration of Azure-based data stores
- **Configurable Event Tracking** - EventPolicy support for controlling change tracking behavior
- **Azure Identity Integration** - Managed credential handling for Azure services with automatic token refresh

## Overview

Trelnex.Core.Azure bridges the gap between the Trelnex framework and Azure services, providing implementations that follow the patterns and interfaces defined in the core libraries while leveraging Azure-specific functionality.

## DataProviders

The library includes data providers for Azure data services that implement the `IDataProvider<TItem>` interface from Trelnex.Core.Data.

### CosmosDataProvider - CosmosDB NoSQL

<details>

<summary>Expand</summary>

&nbsp;

`CosmosDataProvider` is an `IDataProvider<TItem>` that uses Azure Cosmos DB NoSQL API as a backing store, providing scalable, globally distributed data access.

#### CosmosDataProvider - Dependency Injection

The `AddCosmosDataProviders` method takes a `Action<IDataProviderOptions>` `configureDataProviders` delegate. This delegate configures the necessary `IDataProvider` instances for the application.

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
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddCosmosDataProviders(
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
            .Add<User>(
                typeName: "user",
                itemValidator: User.Validator,
                commandOperations: CommandOperations.All);
    }
```

#### CosmosDataProvider - Configuration

`appsettings.json` specifies the configuration of a `CosmosDataProvider`. Values like connection strings can be sourced from environment variables for security.

```json
  "Azure.CosmosDataProviders": {
    "EndpointUri": "FROM_ENV",
    "DatabaseId": "trelnex-core-data-tests",
    "Containers": {
      "test-item": {
        "ContainerId": "test-items",
        "EventPolicy": "AllChanges",
        "EventTimeToLive": 31556952
      },
      "encrypted-test-item": {
        "ContainerId": "test-items",
        "EventPolicy": "DecoratedChanges",
        "Encryption": {
          "Primary": {
            "CipherName": "AesGcm",
            "Secret": "5709bb5e-8dc6-49cf-919c-7483acca06be"
          },
          "Secondary": [
            {
              "CipherName": "AesGcm",
              "Secret": "fa577f5c-4b7c-460f-ba56-9d3ec024b3d4"
            }
          ]
        }
      }
    }
  }
```

The `EventPolicy` property controls change tracking behavior. Options include:
- `Disabled` - No events generated
- `NoChanges` - Events without property changes
- `DecoratedChanges` - Only `[Track]` decorated properties tracked
- `AllChanges` - All properties tracked except `[DoNotTrack]` (default)

The `EventTimeToLive` property is optional and allows automatic expiration and deletion of the events from CosmosDB. The value is expressed in seconds.

The `Encryption` section is optional and enables client-side encryption for the specified type name. When provided, properties marked with the `[Encrypt]` attribute will be automatically encrypted before storage and decrypted when retrieved, ensuring sensitive data remains protected at rest. Encrypted properties maintain their encrypted values in event change tracking for complete security.

#### CosmosDataProvider - Container Schema

The document schema in Cosmos DB follows these conventions:
  - Document id = `/id`
  - Document partition key = `/partitionKey`
  - Standard properties from `BaseItem` are mapped to appropriate fields
  - Custom properties are serialized according to JSON property name attributes

</details>

### SqlDataProvider - SQL Server

<details>

<summary>Expand</summary>

&nbsp;

`SqlDataProvider` is an `IDataProvider<TItem>` that uses Azure SQL Database or SQL Server as a backing store, providing relational database capabilities while maintaining the same command-based interface.

#### SqlDataProvider - Dependency Injection

The `AddSqlDataProviders` method takes a `Action<IDataProviderOptions>` `configureDataProviders` delegate. This delegate configures the necessary `IDataProvider` instances for the application.

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
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddSqlDataProviders(
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
            .Add<User>(
                typeName: "user",
                itemValidator: User.Validator,
                commandOperations: CommandOperations.All);
    }
```

#### SqlDataProvider - Configuration

`appsettings.json` specifies the configuration of a `SqlDataProvider`. Connection strings can be securely loaded from environment variables.

```json
  "Azure.SqlDataProviders": {
    "DataSource": "FROM_ENV",
    "InitialCatalog": "trelnex-core-data-tests",
    "Tables": {
      "test-item": {
        "ItemTableName": "test-items",
        "EventTableName": "test-items-events",
        "EventPolicy": "AllChanges",
        "EventTimeToLive": 31556952
      },
      "encrypted-test-item": {
        "ItemTableName": "test-items",
        "EventTableName": "test-items-events",
        "EventPolicy": "DecoratedChanges",
        "Encryption": {
          "Primary": {
            "CipherName": "AesGcm",
            "Secret": "cf6d4e78-f4c6-4569-a6d0-de62d3aa6227"
          },
          "Secondary": [
            {
              "CipherName": "AesGcm",
              "Secret": "1c7595fe-8c36-456c-893f-f8208226249b"
            }
          ]
        }
      }
    }
  }
```

The `EventPolicy` property controls change tracking behavior. Options include:
- `Disabled` - No events generated
- `NoChanges` - Events without property changes
- `DecoratedChanges` - Only `[Track]` decorated properties tracked
- `AllChanges` - All properties tracked except `[DoNotTrack]` (default)

The `EventTableName` property is optional and defaults to `{ItemTableName}-events` if not specified.

The `EventTimeToLive` property is optional. When provided, it will set the expireAtDateTimeOffset value in the table. A cron job can be developed to automatically delete the events from SQL. The value is expressed in seconds.

The `Encryption` section is optional and enables client-side encryption for the specified type name. When provided, properties marked with the `[Encrypt]` attribute will be automatically encrypted before storage and decrypted when retrieved, ensuring sensitive data remains protected at rest. Encrypted properties maintain their encrypted values in event change tracking for complete security.

#### SqlDataProvider - Item Schema

The table for the items must follow the following schema.

```sql
CREATE TABLE [test-items] (
    [id] nvarchar(255) NOT NULL UNIQUE,
    [partitionKey] nvarchar(255) NOT NULL,
    [typeName] nvarchar(max) NOT NULL,
    [version] int NOT NULL,
    [createdDateTimeOffset] datetimeoffset NOT NULL,
    [updatedDateTimeOffset] datetimeoffset NOT NULL,
    [deletedDateTimeOffset] datetimeoffset NULL,
    [isDeleted] bit NULL,
    [_etag] nvarchar(255) NULL,

    ..., // TItem specific columns

    PRIMARY KEY ([id], [partitionKey])
);
```

#### SqlDataProvider - Event Schema

The table for the events must use the following schema to track changes.

```sql
CREATE TABLE [test-items-events] (
    [id] nvarchar(255) NOT NULL UNIQUE,
    [partitionKey] nvarchar(255) NOT NULL,
    [typeName] nvarchar(max) NOT NULL,
    [version] int NOT NULL,
    [createdDateTimeOffset] datetimeoffset NOT NULL,
    [updatedDateTimeOffset] datetimeoffset NOT NULL,
    [deletedDateTimeOffset] datetimeoffset NULL,
    [expireAtDateTimeOffset] datetimeoffset NULL,
    [isDeleted] bit NULL,
    [_etag] nvarchar(255) NULL,
    [saveAction] nvarchar(max) NOT NULL,
    [relatedId] nvarchar(255) NOT NULL,
    [relatedTypeName] nvarchar(max) NOT NULL,
    [changes] json NULL,
    [traceContext] nvarchar(55) NULL,
    [traceId] nvarchar(32) NULL,
    [spanId] nvarchar(16) NULL,
    PRIMARY KEY ([id], [partitionKey]),
    FOREIGN KEY ([relatedId], [partitionKey]) REFERENCES [test-items]([id], [partitionKey])
);
```

#### SqlDataProvider - Item Trigger

The following trigger must exist to check and update the item ETag for optimistic concurrency control.

```sql
CREATE TRIGGER [tr-test-items-etag]
ON [test-items]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM [inserted] AS [i]
        JOIN [deleted] AS [d] ON
            [i].[id] = [d].[id] AND
            [i].[partitionKey] = [d].[partitionKey]
        WHERE [i].[_etag] != [d].[_etag]
    ) THROW 2147418524, 'Precondition Failed.', 1;

    UPDATE [test-items]
    SET [_etag] = CONVERT(nvarchar(max), NEWID())
    FROM [inserted] AS [i]
    WHERE
        [test-items].[id] = [i].[id] AND
        [test-items].[partitionKey] = [i].[partitionKey]
END;
```

#### SqlDataProvider - Event Trigger

The following trigger must exist to update the event ETag.

```sql
CREATE TRIGGER [tr-test-items-events-etag]
ON [test-items-events]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [test-items-events]
    SET [_etag] = CONVERT(nvarchar(max), NEWID())
    FROM [inserted] AS [i]
    WHERE [test-items-events].[id] = [i].[id] AND [test-items-events].[partitionKey] = [i].[partitionKey]
END;
```

</details>

## Security Model

Trelnex.Core.Azure leverages Azure's robust security features to protect your data and applications. It implements a comprehensive security model for both Cosmos DB and SQL Server data stores, maintaining a consistent programming interface. For detailed information on Azure security best practices, refer to the official [Azure Security Documentation](https://learn.microsoft.com/en-us/azure/security/).

### CosmosDB Security Model

<details>

<summary>Expand</summary>

&nbsp;

Cosmos DB security is built around network controls, RBAC permissions, and authentication.

#### Azure Setup for CosmosDB

Refer to the [Azure Cosmos DB Security Checklist](https://learn.microsoft.com/en-us/azure/cosmos-db/security-checklist) for detailed setup instructions. Key steps include:

1.  Creating a Cosmos DB Account
2.  Creating a Database and Containers
3.  Configuring Network Security (Private Endpoints or IP restrictions)
4.  Enabling Managed Identity Access

</details>

### SQL Server Security Model

<details>

<summary>Expand</summary>

&nbsp;

SQL Server security in Azure combines multiple layers of protection.

#### Azure Setup for SQL Server

Refer to the [Security best practices for SQL Database](https://learn.microsoft.com/en-us/azure/sql-database/sql-database-security-best-practices) for detailed setup. Key steps include:

1.  Creating a SQL Server and Database
2.  Configuring Network Security (Private Endpoints or Firewall Rules)
3.  Setting up Identity and Access Management (Managed Identity)
4.  Enabling Advanced Security Features (Threat Protection, Encryption)

</details>

## Identity

Azure Identity integration provides managed authentication for Azure services.

<details>

<summary>Expand</summary>

&nbsp;

Trelnex.Core.Azure uses Azure's managed identity service for secure authentication. Applications should register the `AzureCredentialProvider` and use dependency injection to obtain `TokenCredential` and access tokens.

#### Key Features of AzureCredentialProvider

- **Credential Chaining** - Tries multiple credential sources in order of preference
- **Token Caching** - Caches access tokens to reduce authentication requests
- **Automatic Token Refresh** - Manages token lifecycle and refreshes before expiration
- **Token Status Reporting** - Provides health status of all managed tokens
- **Multiple Credential Sources** - Supports WorkloadIdentity and AzureCli credential sources

#### Azure Managed Identities

Trelnex.Core.Azure uses Azure's managed identity service.

##### Workload Identity

Workload Identity is recommended for production environments in AKS. See [Use a Kubernetes service account with workload identity](https://learn.microsoft.com/en-us/azure/aks/workload-identity-use-system-assigned).

##### Azure CLI for Development

For local development, `AzureCliCredential` allows developers to use their Azure CLI login context.

#### Credential Chain Fallback

Trelnex.Core.Azure uses a credential chain approach:

```json
{
  "Azure.Credentials": {
    "Sources": [ "WorkloadIdentity", "AzureCli" ]
  }
}
```

With this configuration, the application will:

1.  First try WorkloadIdentity (suitable for production in AKS)
2.  Fall back to AzureCli (suitable for development environments)

This pattern ensures that your application can run both in production with secure managed identities and in development environments with minimal configuration changes.

#### AzureCredentialProvider - Configuration

Configure Azure credentials in your `appsettings.json`:

```json
{
  "Azure.Credentials": {
    "Sources": [ "WorkloadIdentity", "AzureCli" ]
  }
}
```

#### AzureCredentialProvider - Dependency Injection

Add Azure Identity to your service collection:

```csharp
    services
        .AddAzureIdentity(
            configuration,
            bootstrapLogger);
```

#### IAccessTokenProvider - Dependency Injection

Register clients that require access tokens:

```csharp
    // Example of registering a client that uses access tokens
    services.AddScoped<IUsersClient, UsersClient>();
```

#### IAccessTokenProvider - Usage

Use the token provider in your HTTP clients:

```csharp
using System.Net.Http.Headers;

internal class UsersClient(
    HttpClient httpClient,
    ICredentialProvider<TokenCredential> credentialProvider)
    : BaseClient(httpClient), IUsersClient
{
    public async Task<UserResponse> GetUserAsync(string userId)
    {
        // Get access token provider from credential provider
        var tokenProvider = credentialProvider.GetAccessTokenProvider("https://api.trelnex.com/.default");
        var accessToken = tokenProvider.GetAccessToken();
        var authorizationHeader = accessToken.GetAuthorizationHeader();

        // Add the authorization header to the request
        using var request = new HttpRequestMessage(HttpMethod.Get, $"users/{userId}");
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);

        // Send the request
        using var response = await httpClient.SendAsync(request);

        // Process the response
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserResponse>();
    }
}
```

#### ManagedCredential

The `ManagedCredential` class internally manages access tokens with the following capabilities:

- **Thread-safe Token Cache** - Prevents duplicate token acquisitions for the same context
- **Automatic Token Refresh** - Uses a timer to refresh tokens before they expire
- **Error Handling** - Proper handling of credential unavailability with meaningful exceptions
- **Status Reporting** - Provides health status for all managed tokens

</details>
