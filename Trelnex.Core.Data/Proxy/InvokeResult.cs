namespace Trelnex.Core.Data;

/// <summary>
/// Represents the result of a method invocation in the proxy system.
/// </summary>
/// <remarks>
/// This record stores information about method execution results and property change tracking.
/// It captures both the return value and property state changes when applicable.
/// </remarks>
internal record InvokeResult
{
    /// <summary>
    /// Gets or sets the return value of the invoked method.
    /// </summary>
    /// <value>
    /// An object containing the method's return value.
    /// </value>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the property is tracked for changes.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the property is tracked; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsTracked { get; set; }

    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    /// <value>
    /// The property name.
    /// </value>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the previous value of the property before modification.
    /// </summary>
    /// <value>
    /// The original property value.
    /// </value>
    public dynamic? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the current value of the property after modification.
    /// </summary>
    /// <value>
    /// The updated property value.
    /// </value>
    public dynamic? NewValue { get; set; }
}
