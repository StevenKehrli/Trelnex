namespace Trelnex.Core.Observability;

/// <summary>
/// Attribute to include a parameter's value in trace spans.
/// </summary>
/// <remarks>
/// The parameter's value will be captured as a tag in the OpenTelemetry activity.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public class TraceParameterAttribute : Attribute;