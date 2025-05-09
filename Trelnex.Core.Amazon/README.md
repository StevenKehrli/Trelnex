# Trelnex.Core.Amazon

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## CommandProviders

### DynamoCommandProvider - DynamoDB

<details>

<summary>Expand</summary>

&nbsp;

`DynamoCommandProvider` is an `ICommandProvider` that uses DynamoDB as a backing store.

#### DynamoCommandProvider - Dependency Injection

The `AddDynamoCommandProviders` method takes a `Action<ICommandProviderOptions>` `configureCommandProviders` delegate. This delegate will configure any necessary `ICommand Provider` for the application.

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
            .AddAmazonIdentity(
                configuration,
                bootstrapLogger)
            .AddDynamoCommandProviders(
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

#### DynamoCommandProvider - Configuration

`appsettings.json` specifies the configuration of a `DynamoCommandProvider`.

```json
  "DynamoCommandProviders": {
    "Region": "FROM_ENV",
    "Tables": [
      {
        "TypeName": "test-item",
        "TableName": "test-items"
      }
    ]
  }
```

#### DynamoCommandProvider - Table Schema

The table for the items must follow the following schema.
  - Partition key = `partitionKey (S)`
  - Sort key = `id (S)`

</details>

### PostgresCommandProvider - PostgreSQL

<details>

<summary>Expand</summary>

&nbsp;

`PostgresCommandProvider` is an `ICommandProvider` that uses PostgreSQL as a backing store.

#### PostgresCommandProvider - Dependency Injection

The `AddPostgresCommandProviders` method takes a `Action<ICommandProviderOptions>` `configureCommandProviders` delegate. This delegate will configure any necessary `ICommand Provider` for the application.

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
            .AddAmazonIdentity(
                configuration,
                bootstrapLogger)
            .AddPostgresCommandProviders(
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

#### PostgresCommandProvider - Configuration

`appsettings.json` specifies the configuration of a `PostgresCommandProvider`.

```json
  "PostgresCommandProviders": {
    "Region": "FROM_ENV",
    "Host": "FROM_ENV",
    "Port": 5432,
    "Database": "trelnex-core-data-tests",
    "DbUser": "FROM_ENV",
    "Tables": [
      {
        "TypeName": "test-item",
        "TableName": "test-items"
      }
    ]
  }
```

#### PostgresCommandProvider - Item Schema

The table for the items must follow the following schema.

```sql
CREATE TABLE "test-items" (
    "id" varchar NOT NULL,
    "partitionKey" varchar NOT NULL,
    "typeName" varchar NOT NULL,
    "createdDate" timestamptz NOT NULL,
    "updatedDate" timestamptz NOT NULL,
    "deletedDate" timestamptz NULL,
    "isDeleted" boolean NULL,
    "_etag" varchar NULL,

    ..., -- TItem specific columns

    PRIMARY KEY ("id", "partitionKey")
);
```

#### PostgresCommandProvider - Event Schema

The table for the events must use the following schema.

```sql
CREATE TABLE "test-items-events" (
    "id" varchar NOT NULL,
    "partitionKey" varchar NOT NULL,
    "typeName" varchar NOT NULL,
    "createdDate" timestamptz NOT NULL,
    "updatedDate" timestamptz NOT NULL,
    "deletedDate" timestamptz NULL,
    "isDeleted" boolean NULL,
    "_etag" varchar NULL,
    "saveAction" varchar NOT NULL,
    "relatedId" varchar NOT NULL,
    "relatedTypeName" varchar NOT NULL,
    "changes" varchar NULL,
    "context" varchar NULL,
    PRIMARY KEY ("id", "partitionKey"),
    FOREIGN KEY ("relatedId", "partitionKey") REFERENCES "test-items"("id", "partitionKey")
);
```

#### PostgresCommandProvider - Item Trigger

The following trigger must exist to check and update the item ETag.

```sql
CREATE OR REPLACE FUNCTION update_test_items_etag()
RETURNS TRIGGER AS $$
BEGIN
    IF (TG_OP = 'UPDATE') THEN
        IF (OLD._etag != NEW._etag) THEN
            RAISE EXCEPTION 'Precondition Failed.' USING ERRCODE = '23000';
        END IF;
    END IF;

    NEW._etag := gen_random_uuid()::text;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_test_items_etag
BEFORE INSERT OR UPDATE ON "test-items"
FOR EACH ROW EXECUTE FUNCTION update_test_items_etag();
```

#### PostgresCommandProvider - Event Trigger

The following trigger must exist to update the event ETag.

```sql
CREATE OR REPLACE FUNCTION update_test_items_events_etag()
RETURNS TRIGGER AS $$
BEGIN
    NEW._etag := gen_random_uuid()::text;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_test_items_events_etag
BEFORE INSERT OR UPDATE ON "test-items-events"
FOR EACH ROW EXECUTE FUNCTION update_test_items_events_etag();
```

</details>

## Identity

<details>

<summary>Expand</summary>

&nbsp;

### AmazonCredentialProvider

`AmazonCredentialProvider` is an implemtation of `ICredentialProvider<AWSCredentials>`. It ensures that the necessary credentials are available and valid when making requests to AWS services.

Applications should not manage an Amazon `AWSCredentials` directly. Instead, the application should register the `AmazonCredentialProvider` and use dependency injection of `ICredentialProvider<AWSCredentials>` to get the `AWSCredentials` and use dependency injection of `IAccessTokenProvider` to get the `AccessToken`.

#### AmazonCredentialProvider - Dependency Injection

```csharp
    services
        .AddAmazonIdentity(
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
