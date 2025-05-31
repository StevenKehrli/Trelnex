using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// Caches property getter methods.
/// </summary>
/// <typeparam name="TItem">Type to analyze.</typeparam>
/// <remarks>
/// Analyzes properties and caches getter methods for efficient identification.
/// </remarks>
internal class PropertyGetters<TItem>
{
    #region Private Fields

    /// <summary>
    /// Set of property getter method names.
    /// </summary>
    private readonly HashSet<string> _propertyGetters = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a <see cref="PropertyGetters{TItem}"/> instance.
    /// </summary>
    /// <returns>Configured <see cref="PropertyGetters{TItem}"/> instance.</returns>
    /// <remarks>
    /// Analyzes public instance properties and caches their getter methods.
    /// </remarks>
    public static PropertyGetters<TItem> Create()
    {
        var propertyGetters = new PropertyGetters<TItem>();

        // Get all public instance properties defined on the type
        var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
        {
            // Get the property's getter method (if one exists)
            // Skip properties without getter methods
            var getMethod = property.GetGetMethod();
            if (getMethod is null) continue;

            propertyGetters._propertyGetters.Add(getMethod.Name);
        }

        // Return the fully configured property getters instance
        return propertyGetters;
    }

    /// <summary>
    /// Checks if a method is a property getter.
    /// </summary>
    /// <param name="targetMethod">Method to check.</param>
    /// <returns>True if the method is a property getter.</returns>
    public bool IsGetter(
        MethodInfo? targetMethod)
    {
        // Check if the method exists and is in the cached collection of getter methods
        return (targetMethod is not null) && _propertyGetters.Contains(targetMethod.Name);
    }

    #endregion
}
