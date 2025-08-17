namespace Trelnex.Core.Data;

/// <summary>
/// Marks a property to be excluded from change tracking operations.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DoNotTrackAttribute : Attribute;
