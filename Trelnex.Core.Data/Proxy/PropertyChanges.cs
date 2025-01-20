namespace Trelnex.Core.Data;

internal class PropertyChanges
{
    /// <summary>
    /// The underlying collection of property changes.
    /// </summary>
    private Dictionary<string, PropertyChange> _propertyChanges = [];

    public void Add(
        string propertyName,
        dynamic? oldValue,
        dynamic? newValue)
    {
        // there are several possibilities here

        // 1: there is an existing property change for the property name
        // 1a: the new value is now the same as the existing property change old value
        //     there is no need to track the property change since it has not changed
        //     remove the property change from the underlying collection
        // 1b: the new value is different from the existing property change old value
        //     update the old value in the existing property change
        //
        // 2: there is not an existing property change for the property name
        // 2a: the new value is different from the old value
        //     create a new property change for the property name
        // 2b: the new value is the same as the old value
        //     there is not need to track the property change since it has not changed
        //     there is no work to do here

        if (_propertyChanges.TryGetValue(propertyName, out var propertyChange))
        {
            // 1: there is an existing property change

            if (Equals(propertyChange.OldValue, newValue))
            {
                // 1a: the new value is now the same as the existing property change old value
                // 1a: there is no need to track the property change since it has not changed
                // 1a: remove the property change from the underlying collection
                _propertyChanges.Remove(propertyName);
            }
            else
            {
                // 1b: the new value is different from the existing property change old value
                // 1b: update the old value in the existing property change
                propertyChange.NewValue = newValue;
            }
        }
        else
        {
            // 2: there is not an existing property change for the property name

            if (Equals(oldValue, newValue) is false)
            {
                // 2a: the new value is different from the old value
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
    /// Get the array of <see cref="PropertyChange"/>.
    /// </summary>
    /// <returns>The array of <see cref="PropertyChange"/>.</returns>
    public PropertyChange[]? ToArray()
    {
        // get the array of property changes
        if (0 == _propertyChanges.Count) return null;

        return _propertyChanges.Values
            .OrderBy(pc => pc.PropertyName)
            .ToArray();
    }
}
