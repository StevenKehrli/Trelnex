namespace Trelnex.Core.Data;

/// <summary>
/// Tracks property modifications.
/// </summary>
/// <remarks>
/// Manages property changes, handling reverted values and multiple modifications.
/// </remarks>
internal class PropertyChanges
{
    #region Private Fields

    /// <summary>
    /// Collection of tracked property changes.
    /// </summary>
    private Dictionary<string, PropertyChange> _propertyChanges = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds or updates a property change.
    /// </summary>
    /// <param name="propertyName">Name of changed property.</param>
    /// <param name="oldValue">Original property value.</param>
    /// <param name="newValue">New property value.</param>
    /// <remarks>
    /// Handles reverted values, updates, and new changes.
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
    /// Gets all tracked property changes.
    /// </summary>
    /// <returns>Ordered array of PropertyChange objects, or null if no changes.</returns>
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
