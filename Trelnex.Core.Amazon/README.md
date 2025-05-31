# Trelnex.Core.Amazon

`Trelnex.Core.Amazon` is a .NET library that provides AWS-specific implementations for key components of the Trelnex framework. It includes integration with AWS services for data storage, authentication, and identity management, enabling applications to securely interact with AWS resources.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Key Features

- **Data Access Integration** - DynamoDB and PostgreSQL command providers for the Trelnex.Core.Data system
- **Command Provider Factories** - Simplified registration of AWS-based data stores
- **AWS Identity Integration** - Managed credential handling for AWS services with automatic token refresh
- **Query Translation** - LINQ to DynamoDB expression conversion for strongly-typed queries

## Overview

Trelnex.Core.Amazon bridges the gap between the Trelnex framework and AWS services, providing implementations that follow the patterns and interfaces defined in the core libraries while leveraging AWS-specific functionality.

## CommandProviders

The library includes command providers for AWS data services that implement the `ICommandProvider` interface from Trelnex.Core.Data.

### DynamoCommandProvider - DynamoDB

<details>

<summary>Expand</summary>

&nbsp;

`DynamoCommandProvider` is an `ICommandProvider` that uses Amazon DynamoDB as a backing store, providing scalable, highly available NoSQL database capabilities.

#### DynamoCommandProvider - Dependency Injection

The `AddDynamoCommandProviders` method takes a `Action<ICommandProviderOptions>` `configureCommandProviders` delegate. This delegate configures the necessary `ICommandProvider` instances for the application.

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

`appsettings.json` specifies the configuration of a `DynamoCommandProvider`. Values like region can be sourced from environment variables for security.

```json
  "Amazon.DynamoCommandProviders": {
    "Region": "FROM_ENV",
    "Tables": {
      "test-item": {
        "TableName": "test-items"
      },
      "encrypted-test-item": {
        "TableName": "test-items",
        "EncryptionSecret": "2ff9347d-0566-499a-b2d3-3aeaf3fe7ae5"
      }
    }
  }
```

The `EncryptionSecret` property is optional and enables client-side encryption for the specified type name. When provided, properties marked with the `[Encrypt]` attribute will be automatically encrypted before storage and decrypted when retrieved, ensuring sensitive data remains protected at rest.

#### DynamoCommandProvider - Table Schema

The DynamoDB table must follow these requirements:
- Partition key = `partitionKey (S)` - String type partition key
- Sort key = `id (S)` - String type sort key
- Standard properties from `BaseItem` are mapped to appropriate attributes
- Custom properties are serialized according to JSON property name attributes

#### DynamoCommandProvider - Query Model

The `QueryHelper<T>` class provides LINQ to DynamoDB expression translation:

```csharp
// Build a strongly-typed LINQ query
var query = items.AsQueryable()
    .Where(x => x.Status == "Active" && x.Count > 10)
    .OrderByDescending(x => x.CreatedDate);

// Translate to DynamoDB expressions
var queryHelper = QueryHelper<Item>.FromLinqExpression(query.Expression);

// Apply the query with DynamoDB expressions for filtering and in-memory for sorting
var results = queryHelper.Filter(items);
```

The query translation supports:
- Equality and comparison operators
- Logical operators (AND, OR)
- String operations (Contains, StartsWith)
- NULL checks
- Complex nested expressions

</details>

### PostgresCommandProvider - PostgreSQL with IAM Authentication

<details>

<summary>Expand</summary>

&nbsp;

`PostgresCommandProvider` is an `ICommandProvider` that uses Amazon RDS for PostgreSQL as a backing store, providing relational database capabilities with AWS IAM authentication.

#### PostgresCommandProvider - Dependency Injection

The `AddPostgresCommandProviders` method takes a `Action<ICommandProviderOptions>` `configureCommandProviders` delegate. This delegate configures the necessary `ICommandProvider` instances for the application.

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

`appsettings.json` specifies the configuration of a `PostgresCommandProvider`. Connection information can be securely loaded from environment variables.

```json
  "Amazon.PostgresCommandProviders": {
    "Host": "FROM_ENV",
    "Database": "trelnex-core-data-tests",
    "DbUser": "FROM_ENV",
    "Tables": {
      "test-item": {
        "TableName": "test-items"
      },
      "encrypted-test-item": {
        "TableName": "test-items",
        "EncryptionSecret": "b5d34a7e-42e1-4cba-8bec-2ab15cb27885"
      }
    }
  }
```

The `EncryptionSecret` property is optional and enables client-side encryption for the specified type name. When provided, properties marked with the `[Encrypt]` attribute will be automatically encrypted before storage and decrypted when retrieved, ensuring sensitive data remains protected at rest.

#### PostgresCommandProvider - Item Schema

The table for the items must follow the following schema:

```sql
CREATE TABLE "test-items" (
    "id" varchar NOT NULL,
    "partitionKey" varchar NOT NULL,
    "typeName" varchar NOT NULL,
    "createdDateTimeOffset" timestamptz NOT NULL,
    "updatedDateTimeOffset" timestamptz NOT NULL,
    "deletedDateTimeOffset" timestamptz NULL,
    "isDeleted" boolean NULL,
    "_etag" varchar NULL,

    ..., -- TItem specific columns

    PRIMARY KEY ("id", "partitionKey")
);
```

#### PostgresCommandProvider - Event Schema

The table for the events must use the following schema to track changes:

```sql
CREATE TABLE "test-items-events" (
    "id" varchar NOT NULL,
    "partitionKey" varchar NOT NULL,
    "typeName" varchar NOT NULL,
    "createdDateTimeOffset" timestamptz NOT NULL,
    "updatedDateTimeOffset" timestamptz NOT NULL,
    "deletedDateTimeOffset" timestamptz NULL,
    "isDeleted" boolean NULL,
    "_etag" varchar NULL,
    "saveAction" varchar NOT NULL,
    "relatedId" varchar NOT NULL,
    "relatedTypeName" varchar NOT NULL,
    "changes" varchar NULL,
    "traceContext" varchar(55) NULL,
    "traceId" varchar(32) NULL,
    "spanId" varchar(16) NULL,
    PRIMARY KEY ("id", "partitionKey"),
    FOREIGN KEY ("relatedId", "partitionKey") REFERENCES "test-items"("id", "partitionKey")
);
```

#### PostgresCommandProvider - Item Trigger

The following trigger must exist to check and update the item ETag for optimistic concurrency control:

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

The following trigger must exist to update the event ETag:

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

#### PostgresCommandProvider - IAM Authentication

The provider uses AWS IAM authentication to connect to RDS PostgreSQL instances. Instead of storing static passwords, it generates dynamic authentication tokens using AWS credentials:

1. Authentication tokens are generated using `RDSAuthTokenGenerator` with valid AWS credentials
2. Tokens are automatically refreshed before each connection to ensure they don't expire
3. SSL is required for secure communications with the database

</details>

## Security Model

Trelnex.Core.Amazon leverages AWS's robust security features to protect your data and applications. It implements a comprehensive security model for both DynamoDB and PostgreSQL data stores, maintaining a consistent programming interface. For detailed information on AWS security best practices, refer to the official [AWS Security Documentation](https://docs.aws.amazon.com/security/).

### DynamoDB Security Model

<details>

<summary>Expand</summary>

&nbsp;

DynamoDB security is built around IAM permissions, VPC endpoints, and encryption.

#### AWS Setup for DynamoDB

Refer to the [Amazon DynamoDB Security Best Practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices-security.html) for detailed setup instructions. Key steps include:

1. Creating a DynamoDB Table with appropriate encryption settings
2. Configuring IAM Roles with least privilege permissions
3. Setting up VPC Endpoints for private network access
4. Enabling encryption at rest with AWS KMS

</details>

### PostgreSQL Security Model

<details>

<summary>Expand</summary>

&nbsp;

PostgreSQL security in AWS combines IAM authentication with database-level security.

#### AWS Setup for PostgreSQL RDS

Refer to the [Amazon RDS Security Best Practices](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_BestPractices.Security.html) for detailed setup. Key steps include:

1. Creating an RDS PostgreSQL instance with appropriate encryption settings
2. Configuring IAM Database Authentication
3. Setting up VPC Security Groups and network controls
4. Configuring SSL for encrypted connections

</details>

## Identity

AWS Identity integration provides managed authentication for AWS services.

<details>

<summary>Expand</summary>

&nbsp;

Trelnex.Core.Amazon uses AWS's credential management for secure authentication. Applications should register the `AmazonCredentialProvider` and use dependency injection to obtain `AWSCredentials` and access tokens.

### Key Features of AmazonCredentialProvider

- **Credential Management** - Handles AWS credentials and provides them securely to services
- **Token Caching** - Caches access tokens to reduce authentication requests
- **Automatic Token Refresh** - Manages token lifecycle and refreshes before expiration
- **Token Status Reporting** - Provides health status of all managed tokens
- **CallerIdentity Integration** - Supports AWS SigV4 signatures for authentication

### AWS Credential Management

Trelnex.Core.Amazon manages AWS credentials through the following components:

1. **ManagedCredential** - Thread-safe credential wrapper with token caching and refresh
2. **AccessTokenClient** - Client for requesting and validating tokens
3. **CallerIdentitySignature** - Handler for AWS SigV4 signatures

### AmazonCredentialProvider - Dependency Injection

Add Amazon Identity to your service collection:

```csharp
    services
        .AddAmazonIdentity(
            configuration,
            bootstrapLogger);
```

### IAccessTokenProvider - Dependency Injection

Register clients that require access tokens:

```csharp
    // Get the credential provider and access token provider
    services.AddClient<IUsersClient, UsersClient>(
        configuration: configuration);
```

### IAccessTokenProvider - Usage

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

### AWS Credentials Manager

The `AWSCredentialsManager` class manages AWS credentials with the following capabilities:

- **Profile Selection** - Loads credentials from named profiles
- **Environment Variable Support** - Can load credentials from environment variables
- **EC2 Instance Profile Support** - Can load credentials from EC2 instance metadata
- **ECS Task Role Support** - Can load credentials from ECS task roles

</details>

## Observability

<details>

<summary>Expand</summary>

&nbsp;

Trelnex.Core.Amazon provides AWS-specific observability features for tracing and monitoring.

### AWS X-Ray Integration

The library integrates with AWS X-Ray for distributed tracing:

```csharp
// Add X-Ray tracing to your application
services.AddAmazonObservability(configuration);
```

This enables tracing of AWS service calls, including:
- DynamoDB operations
- RDS PostgreSQL queries
- AWS credential and token operations
- HTTP requests to AWS services

Traced operations include:
- Start/end timestamps
- Operation metadata
- Error information
- Dependencies and downstream calls

</details>