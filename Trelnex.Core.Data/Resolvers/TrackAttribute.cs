namespace Trelnex.Core.Data;

/// <summary>
/// Marks a property to be included in change tracking operations.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class TrackAttribute : Attribute;
