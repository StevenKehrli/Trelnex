# Trelnex.Core.Api

## Overview

`Trelnex.Core.Api` is a .NET library designed to provide essential web application operations in a simple manner.

## Features

- **Authentication and Authorization**: Secures the endpoints with authentication and authorization (policy based role checks).
- **Configuration**: Loads configuration from the appsettings json files
- **Exceptions**: Injects the middleware to handle any `HttpStatusCodeException`
- **HealthChecks**: Injects health checks for reporting the health of the web application and its components
- **Metrics**: Integrates [Prometheus](https://prometheus.io/) into the web application
- **Serilog**: Integrates [Serilog](https://serilog.net/) into the web application
- **Swagger**: Generates detailed OpennAPI documentation for developers

## License

See the [LICENSE](LICENSE) file for information.

## Third-Party Libraries

See [NOTICE.md](NOTICE.md) for more information.
