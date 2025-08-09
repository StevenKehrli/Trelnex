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

The service allows applications to securely authenticate users and services, issue signed JWT tokens, and enforce fine-grained authorization rules through a resource-scope-role permission model.

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
   - Example: `"api://amazon.auth.trelnex.com"`
2. **Scopes** - Authorization boundaries (e.g., environments, regions) for a resource
   - Example: `"rbac"`, `"production"`, `".default"`
3. **Roles** - Collections of permissions that can be granted for a resource
   - Examples: `"rbac.create"`, `"rbac.read"`, `"rbac.update"`, `"rbac.delete"`
4. **Principals** - Users or services that are granted access through role assignments
   - Example: `"arn:aws:iam::123456789012:user/john"`

### Permission Model Features

The system supports:
- **Dual assignment model**: Both role assignments and scope assignments for fine-grained control
- **Conditional role access**: Roles are only accessible if the principal has scope assignments
- **Cascade deletion**: Deleting resources, roles, or scopes automatically removes all associated assignments
- **Efficient querying**: Optimized DynamoDB schema for both principal-centric and resource-centric queries
- **Hierarchical permission management** and comprehensive auditability

## API Endpoints

The service exposes the following endpoints:

### Authentication Endpoints
- `GET /.well-known/openid-configuration` - OpenID Connect discovery document
- `GET /.well-known/jwks.json` - JSON Web Key Set for token validation
- `POST /token` - OAuth 2.0 token endpoint for issuing JWTs

### RBAC Management Endpoints

#### Resources
- `POST /resources` - Create a new resource
- `GET /resources` - Get a resource's details
- `DELETE /resources` - Delete a resource and all associated roles, scopes, and assignments

#### Scopes
- `POST /scopes` - Create a new scope for a resource
- `GET /scopes` - Get a scope's details
- `DELETE /scopes` - Delete a scope and all associated assignments

#### Scope Assignments
- `POST /assignments/scopes` - Assign a scope to a principal for a resource
- `GET /assignments/scopes` - Get all principals assigned to a specific scope
- `DELETE /assignments/scopes` - Remove a scope assignment from a principal

#### Roles
- `POST /roles` - Create a new role for a resource
- `GET /roles` - Get a role's details
- `DELETE /roles` - Delete a role and all associated assignments

#### Role Assignments
- `POST /assignments/roles` - Assign a role to a principal for a resource
- `GET /assignments/roles` - Get all principals assigned to a specific role
- `DELETE /assignments/roles` - Remove a role assignment from a principal

#### Principal Access
- `GET /assignments/principals` - Get a principal's access permissions (roles and scopes) for a resource
- `DELETE /assignments/principals` - Delete a principal and all its assignments

### Request/Response Format

All RBAC endpoints use JSON request bodies for parameters rather than URL path parameters. For example:

**Create Resource Request**:
```json
{
  "resourceName": "api://amazon.auth.trelnex.com"
}
```

**Create Role Assignment Request**:
```json
{
  "resourceName": "api://amazon.auth.trelnex.com",
  "roleName": "rbac.create",
  "principalId": "arn:aws:iam::123456789012:user/john"
}
```

**Principal Access Response**:
```json
{
  "resourceName": "api://amazon.auth.trelnex.com",
  "scopes": ["rbac"],
  "roles": ["rbac.create", "rbac.read"]
}
```

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
  "Auth": {
    "trelnex-api-rbac": {
      "Audience": "api://amazon.auth.trelnex.com",
      "Authority": "https://amazon.auth.trelnex.com",
      "MetadataAddress": "https://amazon.auth.trelnex.com/.well-known/openid-configuration",
      "Scope": "rbac"
    }
  },
  "Amazon.Credentials": {
    "Region": "us-west-2",
    "AccessTokenClientConfiguration": {
      "BaseAddress": "https://amazon.auth.trelnex.com/"
    }
  },
  "JWT": {
    "openid-configuration": {
      "issuer": "https://amazon.auth.trelnex.com",
      "token_endpoint": "https://amazon.auth.trelnex.com/token",
      "jwks_uri": "https://amazon.auth.trelnex.com/.well-known/jwks.json",
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

The RBAC system uses a single DynamoDB table with a composite key structure optimized for efficient querying:

- **Partition Key**: `entityName` (String) - Varies by entity type for optimal data distribution
- **Sort Key**: `subjectName` (String) - A composite identifier containing the entity type and name

### Item Types and Key Patterns

1. **Resources**:
   - `entityName`: `"RESOURCE#"`
   - `subjectName`: `"RESOURCE#api://amazon.auth.trelnex.com"`

2. **Scopes**:
   - `entityName`: `"RESOURCE#api://amazon.auth.trelnex.com"`
   - `subjectName`: `"SCOPE#rbac"`

3. **Scope Assignments (By Principal)**:
   - `entityName`: `"PRINCIPAL#arn:aws:iam::123456789012:user/john"`
   - `subjectName`: `"SCOPEASSIGNMENT##RESOURCE#api://amazon.auth.trelnex.com##SCOPE#rbac"`

4. **Scope Assignments (By Scope)**:
   - `entityName`: `"RESOURCE#api://amazon.auth.trelnex.com"`
   - `subjectName`: `"SCOPEASSIGNMENT##SCOPE#rbac##PRINCIPAL#arn:aws:iam::123456789012:user/john"`

5. **Roles**:
   - `entityName`: `"RESOURCE#api://amazon.auth.trelnex.com"`
   - `subjectName`: `"ROLE#rbac.create"`

6. **Role Assignments (By Principal)**:
   - `entityName`: `"PRINCIPAL#arn:aws:iam::123456789012:user/john"`
   - `subjectName`: `"ROLEASSIGNMENT##RESOURCE#api://amazon.auth.trelnex.com##ROLE#rbac.create"`

7. **Role Assignments (By Role)**:
   - `entityName`: `"RESOURCE#api://amazon.auth.trelnex.com"`
   - `subjectName`: `"ROLEASSIGNMENT##ROLE#rbac.create##PRINCIPAL#arn:aws:iam::123456789012:user/john"`

This dual storage pattern enables efficient queries both by principal (to get all assignments for a user) and by role/scope (to get all principals with a specific permission).

### Key Design Benefits

- **Optimized queries**: The dual storage approach allows efficient lookups in both directions
- **Standardized formatting**: All keys use consistent marker prefixes (`RESOURCE#`, `ROLE#`, `SCOPE#`, etc.)
- **Hierarchical structure**: Complex subject names enable prefix-based queries
- **Data locality**: Related items are stored near each other for efficient range queries

## References

- [OAuth 2.0 Client Credentials Flow](https://oauth.net/2/grant-types/client-credentials/)
- [Authenticating AWS Resources Using SigV4 and STS](https://fundwave.com/blogs/authenticating-aws-resources-using-sigv4-and-sts/)
- [IAM Credentials as Verifiable Tokens](https://pcg.io/insights/iam-credentials-as-verifiable-tokens/)
- [Leveraging AWS Signed Requests](https://ahermosilla.com/cloud/2020/11/17/leveraging-aws-signed-requests.html)
- [AWS Key Management Service Documentation](https://docs.aws.amazon.com/kms/latest/developerguide/overview.html)
- [Amazon DynamoDB Documentation](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html)
- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html)
