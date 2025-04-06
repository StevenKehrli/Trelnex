# Trelnex.Core.Azure

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## CommandProviders

### CosmosCommandProvider - CosmosDB NoSQL

<details>

<summary>Expand</summary>

&nbsp;

`CosmosCommandProvider` is an `ICommandProvider` that uses CosmosDB NoSQL as a backing store.

#### CosmosCommandProvider - Dependency Injection

The `AddCosmosCommandProviders` method takes a `Action<ICommandProviderOptions>` `configureCommandProviders` delegate. This delegate will configure any necessary `ICommand Provider` for the application.

In this example, we configure a command provider for the `IUser` interface and its `User` DTO.

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

`appsettings.json` specifies the configuration of a `CosmosCommandProvider`.

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

The table for the items must follow the following schema.
  - Document id = `/id`
  - Document partition key = `/partitionKey`

</details>

### SqlCommandProvider - SQL Server

<details>

<summary>Expand</summary>

&nbsp;

`SqlCommandProvider` is an `ICommandProvider` that uses SQL Server as a backing store.

#### SqlCommandProvider - Dependency Injection

The `AddSqlCommandProviders` method takes a `Action<ICommandProviderOptions>` `configureCommandProviders` delegate. This delegate will configure any necessary `ICommand Provider` for the application.

In this example, we configure a command provider for the `IUser` interface and its `User` DTO.

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

`appsettings.json` specifies the configuration of a `SqlCommandProvider`.

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

The table for the events must use the following schema.

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

The following trigger must exist to check and update the item ETag.

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

## Identity

<details>

<summary>Expand</summary>

&nbsp;

### AzureCredentialProvider

`AzureCredentialProvider` is an implemtation of `ICredentialProvider<TokenCredential>`. It ensures that the necessary credentials are available and valid when making requests to Azure services.

Applications should not manage an Azure `TokenCredential` directly. Instead, the application should register the `AzureCredentialProvider` and use dependency injection of `ICredentialProvider<TokenCredential>` to get the `TokenCredential` and use dependency injection of `IAccessTokenProvider` to get the `AccessToken`.

#### AzureCredentialProvider - Dependency Injection

```csharp
    services
        .AddAzureIdentity(
            configuration,
            bootstrapLogger);
```

#### IAccessTokenProvider - Dependency Injection

```csharp
    // get the credential provider and access token provider
    services.AddClient<IUsersClient, UsersClient>(
        configuration: configuration);
```

#### IAccessTokenProvider - Usage

```csharp
internal class UsersClient(
    HttpClient httpClient,
    IAccessTokenProvider<UsersClient> tokenProvider)
    : BaseClient(httpClient), IUsersClient
{
    ...

        var authorizationHeader = tokenProvider.GetAccessToken().GetAuthorizationHeader();

    ...
}

```

</details>
