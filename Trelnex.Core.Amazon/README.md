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
    "RegionName": "FROM_ENV",
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

## Identity

<details>

<summary>Expand</summary>

&nbsp;

### AmazonCredentialProvider

`AmazonCredentialProvider` is an implemtation of `ICredentialProvider<TokenCredential>`. It ensures that the necessary credentials are available and valid when making requests to AWS services.

Applications should not manage an Amazon `AWSCredentials` directly. Instead, the application should register the `AmazonCredentialProvider` and use dependency injection of `ICredentialProvider<AWSCredentials>` to get the `TokenCredential` and use dependency injection of `IAccessTokenProvider` to get the `AccessToken`.

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
