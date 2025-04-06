# Observability

The Observability directory provides components for monitoring and tracing application behavior within Trelnex applications. It offers a way to collect metrics, logs, and traces to help with debugging, performance optimization, and monitoring.

## Components

### TraceAttribute

`TraceAttribute` is a PostSharp aspect that can be applied to methods to automatically trace their execution. It creates an activity that can be tracked in your observability platform of choice.

#### Features:
- Marks methods to be traced using .NET's `System.Diagnostics.Activity` under the hood
- Creates trace spans with proper activity names and sources
- Captures parameters marked with `TraceIncludeAttribute` as span tags
- Properly handles exceptions by setting the activity status to error
- Ensures all activities are properly disposed of after method execution

#### Usage:
```csharp
[Trace]
public void MyMethod(
    [TraceInclude] string parameter1, 
    string parameter2)
{
    // The method execution will be traced
    // Only parameter1 will be included in the trace as a tag
}

// You can also specify a custom source name
[Trace(sourceName: "CustomActivitySource")]
public void AnotherMethod()
{
    // This method will use "CustomActivitySource" for tracing
}
```

### TraceIncludeAttribute

`TraceIncludeAttribute` is used to mark method parameters that should be included in the trace as tags.

#### Features:
- Simple attribute that can be applied to method parameters
- Works in conjunction with `TraceAttribute` to decide which parameters to include in traces

#### Usage:
```csharp
public void MyMethod(
    [TraceInclude] string visibleParam,  // This parameter will be included in the trace
    string hiddenParam)                  // This parameter will not be traced
{
    // Method implementation
}
```

## Integration with OpenTelemetry

These tracing components are designed to work with OpenTelemetry, which is configured in the Trelnex.Core.Api library. The `TraceAttribute` creates activity sources that can be collected by OpenTelemetry exporters and sent to your observability backend of choice (such as Jaeger, Zipkin, or Azure Application Insights).

The Core.Api project provides configuration for:
- Setting up OpenTelemetry tracing
- Connecting to an OpenTelemetry Protocol (OTLP) endpoint
- Adding instrumentation for ASP.NET Core and HttpClient
- Configuring custom activity sources

## Benefits

- **Automatic Method Tracing**: Add the `[Trace]` attribute to methods of interest
- **Parameter Capture**: Choose which parameters to include in traces with `[TraceInclude]`
- **Performance Insights**: Identify slow methods and performance bottlenecks
- **Distributed Tracing**: Track requests across service boundaries
- **Error Correlation**: See which methods failed and why in your traces

## Configuration

The activity sources created by `TraceAttribute` can be configured in your application's settings through the OpenTelemetry configuration in Trelnex.Core.Api:

```json
{
  "Observability": {
    "OpenTelemetry": {
      "Enabled": true,
      "ServiceName": "MyService",
      "ServiceVersion": "1.0.0",
      "Sources": ["MyService", "CustomActivitySource"]
    }
  }
}
```
