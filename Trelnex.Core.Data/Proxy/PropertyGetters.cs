using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// A class that maintains the collection of property getters.
/// </summary>
/// <typeparam name="TItem">The type to analyze for property getters.</typeparam>
internal class PropertyGetters<TItem>
{
    /// <summary>
    /// The collection of property getters.
    /// </summary>
    private readonly HashSet<string> _propertyGetters = [];

    /// <summary>
    /// Create a new instance of <see cref="PropertyGetters{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This will enumerate the properties of the specified type for property getters.
    /// </para>
    /// </remarks>
    /// <returns>The <see cref="PropertyGetters{T}"/>.</returns>
    public static PropertyGetters<TItem> Create()
    {
        var propertyGetters = new PropertyGetters<TItem>();

        // enumerate all properties for the getters
        var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
        {
            // get the get method for the property
            var getMethod = property.GetGetMethod();
            if (getMethod is null) continue;

            propertyGetters._propertyGetters.Add(getMethod.Name);
        }

        return propertyGetters;
    }

    /// <summary>
    /// Get a value indicating if the specified target method is a property getter.
    /// </summary>
    /// <param name="targetMethod">The method the caller invoked.</param>
    /// <returns>true if the specified target method is a property getter; otherwise, false.</returns>
    public bool IsGetter(
        MethodInfo? targetMethod)
    {
        return (targetMethod is not null) && _propertyGetters.Contains(targetMethod.Name);
    }
}
