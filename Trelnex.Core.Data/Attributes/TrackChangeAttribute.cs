namespace Trelnex.Core.Data;

/// <summary>
/// Attribute that marks a property for change tracking.
/// </summary>
/// <remarks>
/// Apply this attribute to properties that should be monitored for modifications.
/// When changes occur to properties with this attribute, they can be tracked by
/// the change tracking system.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class TrackChangeAttribute : Attribute;
