using System.Reflection;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Identifies and manages properties that should be tracked for changes.
/// </summary>
/// <typeparam name="TItem">The type to analyze for trackable properties.</typeparam>
/// <remarks>
/// This class analyzes a type's properties for those decorated with <see cref="TrackChangeAttribute"/>
/// and provides mechanisms to monitor and record changes to those properties.
/// </remarks>
internal class TrackProperties<TItem>
{
    #region Private Fields

    /// <summary>
    /// Gets the dictionary mapping property setter methods to their tracking information.
    /// </summary>
    /// <remarks>
    /// This collection enables quick lookup of property information when setter methods are invoked.
    /// </remarks>
    private readonly Dictionary<string, TrackProperty> _trackPropertiesBySetMethod = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new instance of the <see cref="TrackProperties{TItem}"/> class.
    /// </summary>
    /// <returns>A configured <see cref="TrackProperties{TItem}"/> instance.</returns>
    /// <remarks>
    /// This factory method analyzes all public instance properties of <typeparamref name="TItem"/>
    /// and identifies those that should be tracked for changes based on attributes.
    /// </remarks>
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
    /// Invokes a method on the target item and captures any resulting property changes.
    /// </summary>
    /// <param name="targetMethod">The method to invoke.</param>
    /// <param name="item">The target instance on which to invoke the method.</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>
    /// An <see cref="InvokeResult"/> containing the method result and property change information.
    /// </returns>
    /// <remarks>
    /// When the invoked method is a property setter that's configured for tracking,
    /// this method captures both the previous and new property values.
    /// </remarks>
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

        // get the property based on the target method name
        _trackPropertiesBySetMethod.TryGetValue(targetMethodName, out var trackProperty);

        // get the old property value
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

    #endregion

    #region Private Types

    /// <summary>
    /// Stores information about a property that should be tracked for changes.
    /// </summary>
    /// <param name="PropertyName">The JSON property name used for serialization.</param>
    /// <param name="PropertyInfo">Reflection information about the property.</param>
    private record TrackProperty(
        string PropertyName,
        PropertyInfo PropertyInfo);

    #endregion
}
