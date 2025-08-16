namespace Trelnex.Core.Data;

/// <summary>
/// Attribute to indicate that a property should be excluded from change tracking.
/// </summary>
/// <remarks>
/// Apply this attribute to properties that should not be monitored for modifications by the proxy system.
/// Properties marked with this attribute will be ignored during change tracking.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DoNotTrackAttribute : Attribute;
