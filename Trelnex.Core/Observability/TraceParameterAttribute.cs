namespace Trelnex.Core.Observability;

/// <summary>
/// Attribute to mark a parameter to be included in the trace.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class TraceParameterAttribute : Attribute
{
}
