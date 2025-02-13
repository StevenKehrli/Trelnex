# Trelnex.Auth.Amazon

## OAuth 2.0 Authorization Server Using AWS IAM

Trelnex.Auth.Amazon is an OAuth 2.0 authorization server implementation using AWS IAM. It leverages AWS SecurityTokenService Caller Identity to authenticate users and roles, providing secure access to AWS resources.

### Credentials Flow

#### 1. Client Creates a CallerIdentitySignature

The client uses the AWS Security Token Service (STS) to create a CallerIdentitySignature. This involves the following steps:

1. **Get Caller Identity**: The client calls the `GetCallerIdentity` API to obtain a unique identifier for the AWS account and the IAM user or role making the request.
2. **Sign the Request**: The client signs the `GetCallerIdentity` request using AWS Signature Version 4 (SigV4). This signature is included in the `CallerIdentitySignature`.

#### 2. Client Sends CallerIdentitySignature as client_credentials Grant Type

The client sends an OAuth 2.0 token request to the authorization server using the `client_credentials` grant type. The request includes the `CallerIdentitySignature` encoded in the `client_secret`.

#### 3. Server Validates CallerIdentitySignature

The authorization server performs the following steps:

1. **Extract Signature**: The server extracts the `CallerIdentitySignature` from the request.
2. **Validate Signature**: The server validates the `CallerIdentitySignature` by calling the `GetCallerIdentity` API using the AWS SDK. This ensures that the signature is valid and was created by an authorized client.
3. **Verify Identity**: The server verifies the identity of the client based on the response from the `GetCallerIdentity` API.

#### 4. Server Issues JWT

If the `CallerIdentitySignature` is valid, the server issues a JSON Web Token (JWT) to the client. The JWT contains claims about the client's identity and any other relevant information.

### DynamoDB - Table Schema

The authorization server uses a DynamoDB table to store the RBAC settings. The table for the items must follow the following schema.
  - Partition key = `resourceName (S)`
  - Sort key = `subjectName (S)`

### References

  - [https://fundwave.com/blogs/authenticating-aws-resources-using-sigv4-and-sts/](https://fundwave.com/blogs/authenticating-aws-resources-using-sigv4-and-sts/)
  - [https://pcg.io/insights/iam-credentials-as-verifiable-tokens/](https://pcg.io/insights/iam-credentials-as-verifiable-tokens/)
  - [https://ahermosilla.com/cloud/2020/11/17/leveraging-aws-signed-requests.html](https://ahermosilla.com/cloud/2020/11/17/leveraging-aws-signed-requests.html)

## License

See the [LICENSE](LICENSE) file for information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.
