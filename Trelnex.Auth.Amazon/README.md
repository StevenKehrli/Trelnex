# Trelnex.Auth.Amazon

A comprehensive authentication and authorization service leveraging AWS services for securing cloud-native applications.

## License

See [LICENSE](LICENSE) for more information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.

## Overview

Trelnex.Auth.Amazon is an OAuth 2.0 authorization server implementation that integrates with AWS Identity and Access Management (IAM). It provides:

- **JWT token issuance and validation** using AWS Key Management Service (KMS) for cryptographic operations
- **Role-Based Access Control (RBAC)** with a flexible permission model stored in Amazon DynamoDB
- **AWS Security Token Service (STS)** integration for caller identity verification
- **OpenID Connect** standard endpoints for discovery and JWT validation

The service allows applications to securely authenticate users and services, issue signed JWT tokens, and enforce fine-grained authorization rules through a resource-role-scope permission model.

## Architecture

### Authentication Flow

The service implements the OAuth 2.0 client credentials flow with AWS STS caller identity:

1. **Client Creates CallerIdentitySignature**:
   - Client calls the AWS STS `GetCallerIdentity` API
   - Request is signed using AWS Signature Version 4 (SigV4)
   - The signed request becomes the client credential

2. **Client Requests Token**:
   - Client sends an OAuth 2.0 token request with the CallerIdentitySignature
   - Request uses the `client_credentials` grant type

3. **Server Validates Identity**:
   - Server extracts and validates the CallerIdentitySignature
   - Server calls AWS STS to verify the caller's identity

4. **Server Issues JWT**:
   - Server queries RBAC database for the principal's permissions
   - Server issues a signed JWT with appropriate claims
   - JWT is signed using AWS KMS with an elliptic curve key (ES256)

### RBAC Permission Model

The permission model is built around four core entities:

1. **Resources** - Protected assets or services requiring access control
2. **Roles** - Collections of permissions that can be granted for a resource
3. **Scopes** - Authorization boundaries (e.g., environments, regions) for a resource
4. **Principals** - Users or services that are granted access through role assignments

The system supports:
- Associating multiple roles with a principal for a specific resource
- Limiting role permissions to specific scopes
- Hierarchical permission management and auditability

## API Endpoints

The service exposes the following endpoints:

### Authentication Endpoints
- `GET /.well-known/openid-configuration` - OpenID Connect discovery document
- `GET /.well-known/jwks.json` - JSON Web Key Set for token validation
- `POST /token` - OAuth 2.0 token endpoint for issuing JWTs

### RBAC Management Endpoints

#### Resources
- `POST /api/resources` - Create a new resource
- `GET /api/resources/{resourceName}` - Get a resource's details
- `DELETE /api/resources/{resourceName}` - Delete a resource

#### Roles
- `POST /api/resources/{resourceName}/roles` - Create a new role for a resource
- `GET /api/resources/{resourceName}/roles/{roleName}` - Get a role's details
- `DELETE /api/resources/{resourceName}/roles/{roleName}` - Delete a role

#### Scopes
- `POST /api/resources/{resourceName}/scopes` - Create a new scope for a resource
- `GET /api/resources/{resourceName}/scopes/{scopeName}` - Get a scope's details
- `DELETE /api/resources/{resourceName}/scopes/{scopeName}` - Delete a scope

#### Principal Memberships
- `GET /api/principals/{principalId}/memberships/{resourceName}` - Get a principal's roles for a resource
- `POST /api/principals/{principalId}/memberships/{resourceName}/roles/{roleName}` - Grant a role to a principal
- `DELETE /api/principals/{principalId}/memberships/{resourceName}/roles/{roleName}` - Revoke a role from a principal

#### Role Assignments
- `GET /api/resources/{resourceName}/roles/{roleName}/assignments` - Get principals assigned to a role

#### Principals
- `DELETE /api/principals/{principalId}` - Delete a principal and all its role assignments

## Configuration

The service is configured through `appsettings.json` with the following sections:

```json
{
  "ServiceConfiguration": {
    "FullName": "Trelnex.Auth.Amazon",
    "DisplayName": "Auth",
    "Version": "1.0.0",
    "Description": "Amazon RBAC Authentication and Authorization Service"
  },
  "AmazonCredentials": {
    "Region": "us-west-2",
    "AccessTokenClientConfiguration": {
      "BaseAddress": "https://sts.amazonaws.com"
    }
  },
  "Auth": {
    "trelnex-api-rbac": {
      "Audience": "https://api.example.com",
      "Authority": "https://auth.example.com",
      "MetadataAddress": "https://auth.example.com/.well-known/openid-configuration",
      "Scope": "rbac"
    }
  },
  "JWT": {
    "openid-configuration": {
      "issuer": "https://auth.example.com",
      "token_endpoint": "https://auth.example.com/token",
      "jwks_uri": "https://auth.example.com/.well-known/jwks.json",
      "response_types_supported": [ "id_token" ],
      "subject_types_supported": [ "public" ],
      "id_token_signing_alg_values_supported": [ "ES256" ]
    },
    "DefaultKey": "arn:aws:kms:us-west-2:123456789012:key/abcdef-1234-5678-9abc-def123456789",
    "SecondaryKeys": [],
    "RegionalKeys": [],
    "ExpirationInMinutes": 60
  },
  "RBAC": {
    "Region": "us-west-2",
    "TableName": "Trelnex-RBAC"
  }
}
```

## DynamoDB Schema

The RBAC system uses a single DynamoDB table with a composite key structure:

- **Partition Key**: `resourceName` (String) - The name of the resource
- **Sort Key**: `subjectName` (String) - A composite identifier containing the entity type and name

### Item Types and Subject Name Patterns

1. **Resources**:
   - `subjectName`: `"RESOURCE#"`

2. **Roles**:
   - `subjectName`: `"ROLE#{roleName}"`
   - Example: `"ROLE#admin"`

3. **Scopes**:
   - `subjectName`: `"SCOPE#{scopeName}"`
   - Example: `"SCOPE#production"`

4. **Principal Roles**:
   - `subjectName`: `"PRINCIPAL#{principalId}#ROLE#{roleName}"`
   - Example: `"PRINCIPAL#arn:aws:iam::123456789012:user/john#ROLE#admin"`

## Bootstrap Data

To bootstrap the system with basic permissions for self-management, you'll need to set up the following entities:

### 1. Create the `api://amazon.auth.trelnex.com` Resource

```json
{
  "resourceName": "api://amazon.auth.trelnex.com",
  "subjectName": "RESOURCE#"
}
```

### 2. Create Standard RBAC Roles

```json
[
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "ROLE#rbac.create",
    "roleName": "rbac.create"
  },
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "ROLE#rbac.read",
    "roleName": "rbac.read"
  },
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "ROLE#rbac.update",
    "roleName": "rbac.update"
  },
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "ROLE#rbac.delete",
    "roleName": "rbac.delete"
  }
]
```

### 3. Create Standard Scopes

```json
[
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "SCOPE#rbac",
    "scopeName": "rbac"
  }
]
```

### 4. Grant Admin Access to Initial Admin Principal

Replace `{ADMIN_PRINCIPAL_ARN}` with your admin user or role ARN (e.g., `arn:aws:iam::123456789012:user/admin`):

```json
[
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "PRINCIPAL#{ADMIN_PRINCIPAL_ARN}#ROLE#rbac.create",
    "principalId": "{ADMIN_PRINCIPAL_ARN}",
    "roleName": "rbac.create"
  },
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "PRINCIPAL#{ADMIN_PRINCIPAL_ARN}#ROLE#rbac.read",
    "principalId": "{ADMIN_PRINCIPAL_ARN}",
    "roleName": "rbac.read"
  },
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "PRINCIPAL#{ADMIN_PRINCIPAL_ARN}#ROLE#rbac.update",
    "principalId": "{ADMIN_PRINCIPAL_ARN}",
    "roleName": "rbac.update"
  },
  {
    "resourceName": "api://amazon.auth.trelnex.com",
    "subjectName": "PRINCIPAL#{ADMIN_PRINCIPAL_ARN}#ROLE#rbac.delete",
    "principalId": "{ADMIN_PRINCIPAL_ARN}",
    "roleName": "rbac.delete"
  }
]
```

## References

- [OAuth 2.0 Client Credentials Flow](https://oauth.net/2/grant-types/client-credentials/)
- [Authenticating AWS Resources Using SigV4 and STS](https://fundwave.com/blogs/authenticating-aws-resources-using-sigv4-and-sts/)
- [IAM Credentials as Verifiable Tokens](https://pcg.io/insights/iam-credentials-as-verifiable-tokens/)
- [Leveraging AWS Signed Requests](https://ahermosilla.com/cloud/2020/11/17/leveraging-aws-signed-requests.html)
- [AWS Key Management Service Documentation](https://docs.aws.amazon.com/kms/latest/developerguide/overview.html)
- [Amazon DynamoDB Documentation](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html)
- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html)
