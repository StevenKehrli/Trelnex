namespace Trelnex.Core.Data;

/// <summary>
/// Result of a proxy method invocation.
/// </summary>
/// <remarks>
/// Captures method result and property change data.
/// </remarks>
internal record InvokeResult
{
    /// <summary>
    /// Return value of the invoked method.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Indicates if the property is tracked for changes.
    /// </summary>
    public bool IsTracked { get; set; }

    /// <summary>
    /// Name of the property.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Previous value of the property.
    /// </summary>
    public dynamic? OldValue { get; set; }

    /// <summary>
    /// Current value of the property.
    /// </summary>
    public dynamic? NewValue { get; set; }
}
