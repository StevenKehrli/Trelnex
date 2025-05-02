using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// Identifies and caches property getter methods for a specified type.
/// </summary>
/// <typeparam name="TItem">The type to analyze for property getters.</typeparam>
/// <remarks>
/// This class analyzes a type's properties and caches their getter methods
/// for efficient runtime accessor identification.
/// </remarks>
internal class PropertyGetters<TItem>
{
    #region Private Fields

    /// <summary>
    /// The collection of property getter method names.
    /// </summary>
    private readonly HashSet<string> _propertyGetters = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new instance of the <see cref="PropertyGetters{TItem}"/> class.
    /// </summary>
    /// <returns>A configured <see cref="PropertyGetters{TItem}"/> instance.</returns>
    /// <remarks>
    /// This factory method analyzes all public instance properties of <typeparamref name="TItem"/>
    /// and caches their getter method names.
    /// </remarks>
    public static PropertyGetters<TItem> Create()
    {
        var propertyGetters = new PropertyGetters<TItem>();

        // Get all public instance properties defined on the type
        var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
        {
            // Get the property's getter method (if one exists)
            var getMethod = property.GetGetMethod();
            if (getMethod is null) continue;  // Skip properties without getter methods

            propertyGetters._propertyGetters.Add(getMethod.Name);
        }

        // Return the fully configured property getters instance
        return propertyGetters;
    }

    /// <summary>
    /// Determines whether the specified method is a property getter.
    /// </summary>
    /// <param name="targetMethod">The method to check.</param>
    /// <returns><see langword="true"/> if the method is a property getter; otherwise, <see langword="false"/>.</returns>
    public bool IsGetter(
        MethodInfo? targetMethod)
    {
        // Check if the method exists and is in the cached collection of getter methods
        return (targetMethod is not null) && _propertyGetters.Contains(targetMethod.Name);
    }

    #endregion
}
