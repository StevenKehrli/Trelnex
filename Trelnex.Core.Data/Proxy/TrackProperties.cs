using System.Reflection;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Manages properties for change tracking.
/// </summary>
/// <typeparam name="TItem">Type to analyze.</typeparam>
/// <remarks>
/// Identifies properties with TrackChangeAttribute and monitors their changes.
/// </remarks>
internal class TrackProperties<TItem>
{
    #region Private Fields

    /// <summary>
    /// Maps property setter methods to tracking information.
    /// </summary>
    /// <remarks>
    /// Lookup dictionary with setter method names as keys and tracking metadata as values.
    /// </remarks>
    private readonly Dictionary<string, TrackProperty> _trackPropertiesBySetMethod = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a TrackProperties instance.
    /// </summary>
    /// <returns>Configured TrackProperties instance.</returns>
    /// <remarks>
    /// Identifies trackable properties based on:
    /// - Getter and setter methods
    /// - TrackChangeAttribute
    /// - JsonPropertyNameAttribute
    /// </remarks>
    public static TrackProperties<TItem> Create()
    {
        var trackProperties = new TrackProperties<TItem>();

        // Enumerate all properties for the TrackChangeAttribute
        var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var propertyInfo in properties)
        {
            // Get the set method for the property
            var setMethodName = propertyInfo.GetSetMethod()?.Name;
            if (setMethodName is null) continue;

            // Get the get method for the property
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod is null) continue;

            // Check if we should track this property for changes
            var trackChangeAttribute = propertyInfo.GetCustomAttribute<TrackChangeAttribute>();
            if (trackChangeAttribute is null) continue;

            // Skip if the property is marked for encryption
            var encryptAttribute = propertyInfo.GetCustomAttribute<EncryptAttribute>();
            if (encryptAttribute is not null) continue;

            // Get the JsonPropertyNameAttribute for this property
            var jsonPropertyNameAttribute = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonPropertyNameAttribute is null) continue;

            // Track this property
            var trackedProperty = new TrackProperty(
                PropertyName: jsonPropertyNameAttribute!.Name,
                PropertyInfo: propertyInfo);

            trackProperties._trackPropertiesBySetMethod[setMethodName] = trackedProperty;
        }

        return trackProperties;
    }

    /// <summary>
    /// Invokes a method and captures property changes.
    /// </summary>
    /// <param name="targetMethod">Method to invoke.</param>
    /// <param name="item">Target instance.</param>
    /// <param name="args">Method arguments.</param>
    /// <returns>InvokeResult with method result and property change data.</returns>
    /// <remarks>
    /// Captures old and new values for tracked property setters.
    /// Non-tracked methods execute normally without change tracking.
    /// </remarks>
    /// <exception cref="TargetInvocationException">
    /// When the called method throws an exception.
    /// </exception>
    public InvokeResult Invoke(
        MethodInfo? targetMethod,
        TItem item,
        object?[]? args)
    {
        var invokeResult = new InvokeResult();

        // Get the target method name
        var targetMethodName = targetMethod?.Name;
        if (targetMethodName is null)
        {
            // Invoke the target method on the item (even though it might be null)
            invokeResult.Result = targetMethod?.Invoke(item, args);

            return invokeResult;
        }

        // Get the property based on the target method name
        _trackPropertiesBySetMethod.TryGetValue(targetMethodName, out var trackProperty);

        // Get the old property value if this is a tracked property
        var oldValue = trackProperty?.PropertyInfo.GetValue(item, null);

        // Invoke the target method on the item
        invokeResult.Result = targetMethod?.Invoke(item, args);

        // Get the new property value after the method invocation
        var newValue = trackProperty?.PropertyInfo.GetValue(item, null);

        invokeResult.IsTracked = trackProperty is not null;
        invokeResult.PropertyName = trackProperty?.PropertyName;
        invokeResult.OldValue = oldValue;
        invokeResult.NewValue = newValue;

        return invokeResult;
    }

    #endregion

    #region Private Types

    /// <summary>
    /// Metadata for a tracked property.
    /// </summary>
    /// <param name="PropertyName">JSON property name for serialization.</param>
    /// <param name="PropertyInfo">Reflection metadata for the property.</param>
    /// <remarks>
    /// Links JSON property names with .NET reflection data for property access.
    /// </remarks>
    private record TrackProperty(
        string PropertyName,
        PropertyInfo PropertyInfo);

    #endregion
}
