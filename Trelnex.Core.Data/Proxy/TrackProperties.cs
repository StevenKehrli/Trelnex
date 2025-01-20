using System.Reflection;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// A class that maintains the collection of properties to track for changes.
/// </summary>
/// <typeparam name="TItem">The type to analyze for properties to track for changes, based on the <see cref="TrackChangeAttribute"/>.</typeparam>
internal class TrackProperties<TItem>
{
    /// <summary>
    /// The collection of properties to track for changes (by set method).
    /// </summary>
    private readonly Dictionary<string, TrackProperty> _trackPropertiesBySetMethod = [];

    /// <summary>
    /// Create a new instance of <see cref="TrackProperties{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This will enumerate the properties of the specified type for properties decorated with the TrackChangeAttribute.
    /// </para>
    /// </remarks>
    /// <returns>The <see cref="TrackProperties{T}"/>.</returns>
    public static TrackProperties<TItem> Create()
    {
        var trackProperties = new TrackProperties<TItem>();

        // enumerate all properties for the TrackChangeAttribute
        var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var propertyInfo in properties)
        {
            // get the set method for the property
            var setMethodName = propertyInfo.GetSetMethod()?.Name;
            if (setMethodName is null) continue;

            // get the get method for the property
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod is null) continue;

            // check if we should track this property for changes
            var trackChangeAttribute = propertyInfo.GetCustomAttribute<TrackChangeAttribute>();
            if (trackChangeAttribute is null) continue;

            // get the JsonPropertyNameAttribute for this property
            var jsonPropertyNameAttribute = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonPropertyNameAttribute is null) continue;

            // track this property
            var trackedProperty = new TrackProperty(
                PropertyName: jsonPropertyNameAttribute!.Name,
                PropertyInfo: propertyInfo);

            trackProperties._trackPropertiesBySetMethod[setMethodName] = trackedProperty;
        }

        return trackProperties;
    }

    /// <summary>
    /// Invoke the target method and capture its change, if on a property tracked for changes.
    /// </summary>
    /// <param name="targetMethod">The method the caller invoked.</param>
    /// <param name="item">The item on which to invoke the property get method.</param>
    /// <param name="args">The arguments the caller passed to the method.</param>
    /// <returns>A <see cref="InvokeResult"/> that captures the invoke result and its change.</returns>
    public InvokeResult Invoke(
        MethodInfo? targetMethod,
        TItem item,
        object?[]? args)
    {
        var invokeResult = new InvokeResult();

        // get the target method name
        var targetMethodName = targetMethod?.Name;
        if (targetMethodName is null)
        {
            // invoke the target method on the item
            invokeResult.Result = targetMethod?.Invoke(item, args);

            return invokeResult;
        }

        // get the get method based on the target method name
        _trackPropertiesBySetMethod.TryGetValue(targetMethodName, out var trackProperty);

        // invoke the get method to get the old property value
        var oldValue = trackProperty?.PropertyInfo.GetValue(item, null);

        // invoke the target method on the item
        invokeResult.Result = targetMethod?.Invoke(item, args);

        // invoke the get method to get the new property value
        var newValue = trackProperty?.PropertyInfo.GetValue(item, null);

        invokeResult.IsTracked = trackProperty is not null;
        invokeResult.PropertyName = trackProperty?.PropertyName;
        invokeResult.OldValue = oldValue;
        invokeResult.NewValue = newValue;

        return invokeResult;
    }

    private record TrackProperty(
        string PropertyName,
        PropertyInfo PropertyInfo);
}
