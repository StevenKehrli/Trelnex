# Trelnex.Core.Azure

`Trelnex.Core.Azure` is a .NET library that provides Azure-specific implementations for key components of the Trelnex framework. It includes integration with Azure services for data storage, authentication, and identity management, enabling applications to securely interact with Azure resources.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Key Features

- **Data Access Integration** - SQL Server and Cosmos DB command providers for the Trelnex.Core.Data system
- **Command Provider Factories** - Simplified registration of Azure-based data stores
- **Azure Identity Integration** - Managed credential handling for Azure services with automatic token refresh

## Overview

Trelnex.Core.Azure bridges the gap between the Trelnex framework and Azure services, providing implementations that follow the patterns and interfaces defined in the core libraries while leveraging Azure-specific functionality.

## CommandProviders

The library includes command providers for Azure data services that implement the `ICommandProvider` interface from Trelnex.Core.Data.

### CosmosCommandProvider - CosmosDB NoSQL

<details>

<summary>Expand</summary>

&nbsp;

`CosmosCommandProvider` is an `ICommandProvider` that uses Azure Cosmos DB NoSQL API as a backing store, providing scalable, globally distributed data access.

#### CosmosCommandProvider - Dependency Injection

The `AddCosmosCommandProviders` method takes a `Action<ICommandProviderOptions>` `configureCommandProviders` delegate. This delegate configures the necessary `ICommandProvider` instances for the application.

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
            .AddCosmosCommandProviders(
                configuration,
                bootstrapLogger,
                options => options.AddUsersCommandProviders());
    }
```

```csharp
    public static ICommandProviderOptions AddUsersCommandProviders(
        this ICommandProviderOptions options)
    {
        return options
            .Add<IUser, User>(
                typeName: "user",
                validator: User.Validator,
                commandOperations: CommandOperations.All);
    }
```

#### CosmosCommandProvider - Configuration

`appsettings.json` specifies the configuration of a `CosmosCommandProvider`. Values like connection strings can be sourced from environment variables for security.

```json
  "CosmosCommandProviders": {
    "TenantId": "FROM_ENV",
    "EndpointUri": "FROM_ENV",
    "DatabaseId": "trelnex-core-data-tests",
    "Containers": [
      {
        "TypeName": "test-item",
        "ContainerId": "test-items"
      }
    ]
  }
```

#### CosmosCommandProvider - Container Schema

The document schema in Cosmos DB follows these conventions:
  - Document id = `/id`
  - Document partition key = `/partitionKey`
  - Standard properties from `BaseItem` are mapped to appropriate fields
  - Custom properties are serialized according to JSON property name attributes

</details>

### SqlCommandProvider - SQL Server

<details>

<summary>Expand</summary>

&nbsp;

`SqlCommandProvider` is an `ICommandProvider` that uses Azure SQL Database or SQL Server as a backing store, providing relational database capabilities while maintaining the same command-based interface.

#### SqlCommandProvider - Dependency Injection

The `AddSqlCommandProviders` method takes a `Action<ICommandProviderOptions>` `configureCommandProviders` delegate. This delegate configures the necessary `ICommandProvider` instances for the application.

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
            .AddSqlCommandProviders(
                configuration,
                bootstrapLogger,
                options => options.AddUsersCommandProviders());
    }
```

```csharp
    public static ICommandProviderOptions AddUsersCommandProviders(
        this ICommandProviderOptions options)
    {
        return options
            .Add<IUser, User>(
                typeName: "user",
                validator: User.Validator,
                commandOperations: CommandOperations.All);
    }
```

#### SqlCommandProvider - Configuration

`appsettings.json` specifies the configuration of a `SqlCommandProvider`. Connection strings can be securely loaded from environment variables.

```json
  "SqlCommandProviders": {
    "DataSource": "FROM_ENV",
    "InitialCatalog": "trelnex-core-data-tests",
    "Tables": [
      {
        "TypeName": "test-item",
        "TableName": "test-items"
      }
    ]
  }
```

#### SqlCommandProvider - Item Schema

The table for the items must follow the following schema.

```sql
CREATE TABLE [test-items] (
	[id] nvarchar(255) NOT NULL UNIQUE,
	[partitionKey] nvarchar(255) NOT NULL,
	[typeName] nvarchar(max) NOT NULL,
	[createdDate] datetimeoffset NOT NULL,
	[updatedDate] datetimeoffset NOT NULL,
	[deletedDate] datetimeoffset NULL,
	[isDeleted] bit NULL,
	[_etag] nvarchar(255) NULL,

	..., // TItem specific columns

	PRIMARY KEY ([id], [partitionKey])
);
```

#### SqlCommandProvider - Event Schema

The table for the events must use the following schema to track changes.

```sql
CREATE TABLE [test-items-events] (
	[id] nvarchar(255) NOT NULL UNIQUE,
	[partitionKey] nvarchar(255) NOT NULL,
	[typeName] nvarchar(max) NOT NULL,
	[createdDate] datetimeoffset NOT NULL,
	[updatedDate] datetimeoffset NOT NULL,
	[deletedDate] datetimeoffset NULL,
	[isDeleted] bit NULL,
	[_etag] nvarchar(255) NULL,
	[saveAction] nvarchar(max) NOT NULL,
	[relatedId] nvarchar(255) NOT NULL,
	[relatedTypeName] nvarchar(max) NOT NULL,
	[changes] json NULL,
	[context] json NULL,
	PRIMARY KEY ([id], [partitionKey]),
	FOREIGN KEY ([relatedId], [partitionKey]) REFERENCES [test-items]([id], [partitionKey])
);
```

#### SqlCommandProvider - Item Trigger

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

#### SqlCommandProvider - Event Trigger

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
  "AzureCredentials": {
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
  "AzureCredentials": {
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
    // get the credential provider and access token provider
    services.AddClient<IUsersClient, UsersClient>(
        configuration: configuration);
```

#### IAccessTokenProvider - Usage

Use the token provider in your HTTP clients:

```csharp
internal class UsersClient(
    HttpClient httpClient,
    IAccessTokenProvider<UsersClient> tokenProvider)
    : BaseClient(httpClient), IUsersClient
{
    public async Task<UserResponse> GetUserAsync(string userId)
    {
        // Get the authorization header from the token provider
        var authorizationHeader = tokenProvider.GetAccessToken().GetAuthorizationHeader();

        // Add the authorization header to the request
        using var request = new HttpRequestMessage(HttpMethod.Get, $"users/{userId}");
        request.Headers.Authorization = authorizationHeader;

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
