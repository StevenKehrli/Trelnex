namespace Trelnex.Core.Data;

/// <summary>
/// Manages a collection of property changes for tracking modifications to object properties.
/// </summary>
/// <remarks>
/// This class tracks property modifications and intelligently handles cases where values
/// revert to their original state or undergo multiple changes.
/// </remarks>
internal class PropertyChanges
{
    #region Private Fields

    /// <summary>
    /// The underlying collection of property changes.
    /// </summary>
    private Dictionary<string, PropertyChange> _propertyChanges = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds or updates a property change in the collection.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="oldValue">The original value of the property.</param>
    /// <param name="newValue">The new value of the property.</param>
    /// <remarks>
    /// This method handles several scenarios:
    /// <list type="bullet">
    /// <item>
    ///   <description>If the property value reverts to its original state, the change is removed.</description>
    /// </item>
    /// <item>
    ///   <description>If an existing change is further modified, the record is updated.</description>
    /// </item>
    /// <item>
    ///   <description>If a new change occurs, it's added to the collection.</description>
    /// </item>
    /// <item>
    ///   <description>If old and new values are equal, no change is recorded.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public void Add(
        string propertyName,
        dynamic? oldValue,
        dynamic? newValue)
    {
        if (_propertyChanges.TryGetValue(propertyName, out var propertyChange))
        {
            // Check if the property has reverted to its original value
            if (Equals(propertyChange.OldValue, newValue))
            {
                // Remove the change record since the property has reverted to its original state
                _propertyChanges.Remove(propertyName);
            }
            else
            {
                // Update the existing change record with the current values
                _propertyChanges[propertyName] = new PropertyChange
                {
                    PropertyName = propertyName,
                    OldValue = oldValue,
                    NewValue = newValue,
                };
            }
        }
        else
        {
            // Only track changes when values are different
            if (Equals(oldValue, newValue) is false)
            {
                // Create and store a new property change record
                _propertyChanges[propertyName] = new PropertyChange
                {
                    PropertyName = propertyName,
                    OldValue = oldValue,
                    NewValue = newValue,
                };
            }
        }
    }

    /// <summary>
    /// Gets an array of all tracked property changes.
    /// </summary>
    /// <returns>
    /// An ordered array of <see cref="PropertyChange"/> objects, or <see langword="null"/> if no changes exist.
    /// </returns>
    public PropertyChange[]? ToArray()
    {
        // Return null if no changes exist
        if (0 == _propertyChanges.Count) return null;

        // Return an ordered array of property changes
        return _propertyChanges.Values
            .OrderBy(pc => pc.PropertyName)
            .ToArray();
    }

    #endregion
}
