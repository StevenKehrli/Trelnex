namespace Trelnex.Core.Data;

/// <summary>
/// Attribute to indicate that a property should be tracked for changes.
/// </summary>
/// <remarks>
/// Apply this attribute to properties that require change tracking by the proxy system.
/// Only properties marked with this attribute will be monitored for modifications.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class TrackAttribute : Attribute;
