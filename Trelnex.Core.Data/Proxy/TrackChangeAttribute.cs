namespace Trelnex.Core.Data;

/// <summary>
/// Marks a property for change tracking.
/// </summary>
/// <remarks>
/// Enables the proxy system to monitor and record property modifications.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class TrackChangeAttribute : Attribute;
