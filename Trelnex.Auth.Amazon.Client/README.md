# Trelnex.Auth.Amazon.Client

A command-line client for obtaining OAuth 2.0 access tokens from a Trelnex.Auth.Amazon authorization server using AWS credentials.

## Overview

Trelnex.Auth.Amazon.Client is a console application that obtains OAuth 2.0 / JWT Bearer tokens from the Trelnex.Auth.Amazon service. These tokens can then be used in API requests to services that use JWT authentication from the Trelnex.Auth.Amazon service. It uses the OAuth 2.0 client credentials flow and AWS Security Token Service (STS) for caller identity verification.

Key features:
- Uses AWS Signature Version 4 (SigV4) for secure request signing
- Obtains JWT tokens with customizable scopes
- Outputs tokens in human-readable JSON format
- Simple command-line interface with comprehensive help

## Usage

### Basic Syntax

```bash
dotnet run -- -r <region> -s <scope> -u <uri>
```

### Parameters

| Parameter | Short | Long       | Description                                                                                                            |
| :-------- | :---- | :--------- | :--------------------------------------------------------------------------------------------------------------------- |
| Region    | -r    | --region   | The AWS region name of the SecurityTokenService used to sign and validate the request (e.g., `us-west-2`)              |
| Scope     | -s    | --scope    | The requested scope for the client_credentials OAuth 2.0 grant type (e.g., `api://amazon.auth.trelnex.com/.default`)   |
| URI       | -u    | --uri      | The URI of the Trelnex.Auth.Amazon OAuth 2.0 Authorization Server (e.g., `https://amazon.auth.trelnex.com`)            |

## Output Format

The client outputs a JSON object containing the access token and metadata:

```json
{
  "token": "eyJhbGciOiJFUzI1NiIsImtpZCI6IjEyMzQ1Njc4OTAifQ...",
  "tokenType": "Bearer",
  "expiresOn": "2023-06-01T12:34:56Z",
  "refreshOn": "2023-06-01T12:29:56Z"
}
```

### Output Fields

### Output Fields

| Field       | Description                                                                                         |
| :---------- | :-------------------------------------------------------------------------------------------------- |
| token       | The JWT access token that can be used to authenticate API requests                                  |
| tokenType   | The token type, typically "Bearer"                                                                  |
| expiresOn   | The ISO 8601 timestamp when the token will expire                                                   |
| refreshOn   | The ISO 8601 timestamp when the token should be refreshed (typically 5 minutes before expiration)   |

## Using the Token

To use the obtained token with API requests, include it in the `Authorization` header:

```
Authorization: Bearer eyJhbGciOiJFUzI1NiIsImtpZCI6IjEyMzQ1Njc4OTAifQ...
```

## Related Documentation

- [Trelnex.Auth.Amazon](../Trelnex.Auth.Amazon/README.md) - Documentation for the authorization server
