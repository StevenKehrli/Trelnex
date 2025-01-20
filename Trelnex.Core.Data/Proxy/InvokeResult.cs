namespace Trelnex.Core.Data;

internal record InvokeResult
{
    /// <summary>
    /// An object containing the return value of the invoked method.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// A value indicating if the property is tracked for changes. true if tracked; otherwise, false.
    /// </summary>
    public bool IsTracked { get; set; }

    /// <summary>
    /// The property name.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// The old value for the property.
    /// </summary>
    public dynamic? OldValue { get; set; }

    /// <summary>
    /// The new value for the property.
    /// </summary>
    public dynamic? NewValue { get; set; }
}
