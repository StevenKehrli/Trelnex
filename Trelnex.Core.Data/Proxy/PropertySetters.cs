using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// Caches property setter methods.
/// </summary>
/// <typeparam name="TItem">Type to analyze.</typeparam>
/// <remarks>
/// Analyzes properties and caches setter methods for efficient identification.
/// </remarks>
internal class PropertySetters<TItem>
{
    #region Private Fields

    /// <summary>
    /// Set of property setter method names.
    /// </summary>
    private readonly HashSet<string> _propertySetters = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a <see cref="PropertySetters{TItem}"/> instance.
    /// </summary>
    /// <returns>Configured <see cref="PropertySetters{TItem}"/> instance.</returns>
    /// <remarks>
    /// Analyzes public instance properties and caches their setter methods.
    /// </remarks>
    public static PropertySetters<TItem> Create()
    {
        var propertySetters = new PropertySetters<TItem>();

        // Get all public instance properties defined on the type
        var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
        {
            // Get the property's setter method (if one exists)
            // Skip properties without setter methods
            if (property.SetMethod is null) continue;

            propertySetters._propertySetters.Add(property.SetMethod.Name);
        }

        // Return the fully configured property setters instance
        return propertySetters;
    }

    /// <summary>
    /// Checks if a method is a property setter.
    /// </summary>
    /// <param name="targetMethod">Method to check.</param>
    /// <returns>True if the method is a property setter.</returns>
    public bool IsSetter(
        MethodInfo? targetMethod)
    {
        // Check if the method exists and is in the cached collection of setter methods
        return (targetMethod is not null) && _propertySetters.Contains(targetMethod.Name);
    }

    #endregion
}
