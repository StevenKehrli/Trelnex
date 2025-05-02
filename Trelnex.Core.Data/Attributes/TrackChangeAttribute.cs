namespace Trelnex.Core.Data;

/// <summary>
/// Marks a property for automatic change tracking within the system's proxy-based property interception framework.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is a cornerstone of the change tracking system, allowing the framework to automatically 
/// monitor and record modifications to designated properties. When applied to a property, it signals 
/// to the proxy infrastructure that changes to this property should be detected and recorded.
/// </para>
/// <para>
/// Properties marked with this attribute must also:
/// </para>
/// <list type="bullet">
///   <item><description>Have both a public getter and setter</description></item>
///   <item><description>Be decorated with <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/> to define the external property name</description></item>
/// </list>
/// <para>
/// When property changes are detected, they're tracked in a <see cref="PropertyChanges"/> collection and can be 
/// transformed into <see cref="ItemEvent{TItem}"/> records for audit logging and historical tracking purposes. 
/// The change tracking system intelligently handles:
/// </para>
/// <list type="bullet">
///   <item><description>Recording both old and new values of changed properties</description></item>
///   <item><description>Detecting when properties revert to their original values</description></item>
///   <item><description>Consolidating multiple changes to the same property</description></item>
/// </list>
/// <para>
/// The <see cref="TrackProperties{TItem}"/> class is responsible for analyzing types for properties decorated
/// with this attribute and establishing the tracking infrastructure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Customer : BaseItem
/// {
///     [TrackChange]
///     [JsonPropertyName("name")]
///     public string Name { get; set; } = string.Empty;
///     
///     [TrackChange]
///     [JsonPropertyName("email")]
///     public string Email { get; set; } = string.Empty;
///     
///     // This property will not be tracked for changes
///     [JsonPropertyName("internalNotes")]
///     public string InternalNotes { get; set; } = string.Empty;
/// }
/// </code>
/// </example>
/// <seealso cref="PropertyChanges"/>
/// <seealso cref="PropertyChange"/>
/// <seealso cref="TrackProperties{TItem}"/>
/// <seealso cref="ItemEvent{TItem}"/>
[AttributeUsage(AttributeTargets.Property)]
public class TrackChangeAttribute : Attribute;
