using System.Reflection;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Identifies and manages properties that should be tracked for changes.
/// </summary>
/// <typeparam name="TItem">The type to analyze for trackable properties.</typeparam>
/// <remarks>
/// This class analyzes a type's properties for those decorated with <see cref="TrackChangeAttribute"/>
/// and provides mechanisms to monitor and record changes to those properties. It serves as a foundation
/// for change tracking functionality throughout the application.
/// </remarks>
internal class TrackProperties<TItem>
{
    #region Private Fields

    /// <summary>
    /// Gets the dictionary mapping property setter methods to their tracking information.
    /// </summary>
    /// <remarks>
    /// This collection enables quick lookup of property information when setter methods are invoked.
    /// The key is the name of the property's set method, and the value is the tracking metadata.
    /// </remarks>
    private readonly Dictionary<string, TrackProperty> _trackPropertiesBySetMethod = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new instance of the <see cref="TrackProperties{TItem}"/> class with configured tracking properties.
    /// </summary>
    /// <returns>A configured <see cref="TrackProperties{TItem}"/> instance ready for tracking property changes.</returns>
    /// <remarks>
    /// This factory method analyzes all public instance properties of <typeparamref name="TItem"/>
    /// and identifies those that should be tracked for changes based on the <see cref="TrackChangeAttribute"/>.
    /// Only properties that have both getter and setter methods and are decorated with <see cref="TrackChangeAttribute"/>
    /// and <see cref="JsonPropertyNameAttribute"/> will be tracked.
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
    /// Invokes a method on the target item and captures any resulting property changes.
    /// </summary>
    /// <param name="targetMethod">The method to invoke on the target instance.</param>
    /// <param name="item">The target instance on which to invoke the method.</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>
    /// An <see cref="InvokeResult"/> containing the method result and property change information.
    /// </returns>
    /// <remarks>
    /// When the invoked method is a property setter that's configured for tracking,
    /// this method captures both the previous and new property values for change tracking.
    /// If the method is not a tracked property setter, it will still be invoked but
    /// without tracking property changes.
    /// </remarks>
    /// <exception cref="TargetInvocationException">
    /// Thrown when the called method throws an exception.
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
    /// Stores information about a property that should be tracked for changes.
    /// </summary>
    /// <param name="PropertyName">The JSON property name used for serialization.</param>
    /// <param name="PropertyInfo">Reflection information about the property.</param>
    /// <remarks>
    /// This record combines the JSON property name (used for external representation)
    /// with the .NET reflection metadata needed to access the property values.
    /// </remarks>
    private record TrackProperty(
        string PropertyName,
        PropertyInfo PropertyInfo);

    #endregion
}
