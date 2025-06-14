using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Manages dynamic proxies with change tracking and validation.
/// </summary>
/// <typeparam name="TInterface">Interface type exposed to consumers.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Base class for proxy-based command and result classes that provides:
/// - Dynamic proxy management
/// - Method interception for property tracking
/// - Read-only enforcement
/// - Thread-safe property change tracking
/// - Validation capabilities
/// </remarks>
internal abstract class ProxyManager<TInterface, TItem> : IDisposable
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Static Fields

    private static readonly JsonNode _jsonNodeEmpty = new JsonObject();

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        TypeInfoResolver = new TrackChangeResolver()
    };

    /// <summary>
    /// Cached property getter methods.
    /// </summary>
    /// <remarks>
    /// Used to determine if a method invocation is a property getter.
    /// </remarks>
    private static readonly PropertyGetters<TItem> _propertyGetters = PropertyGetters<TItem>.Create();

    #endregion

    #region Protected Instance Fields

    /// <summary>
    /// Controls whether modifications to the item are permitted.
    /// </summary>
    /// <remarks>
    /// When true, only property getters are allowed.
    /// </remarks>
    protected bool _isReadOnly;

    /// <summary>
    /// The actual item instance being proxied.
    /// </summary>
    protected TItem _item
    {
        get => _itemValue;

        set
        {
            _itemValue = value;
            _initialJsonNode = JsonSerializer.SerializeToNode(value, _jsonSerializerOptions);
        }
    }

    /// <summary>
    /// The proxy that wraps the underlying item.
    /// </summary>
    protected TInterface _proxy = null!;

    /// <summary>
    /// Thread synchronization primitive.
    /// </summary>
    /// <remarks>
    /// Ensures only one thread can modify the item at a time.
    /// </remarks>
    protected readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Validation delegate for the proxied item.
    /// </summary>
    protected ValidateAsyncDelegate<TInterface, TItem> _validateAsyncDelegate = null!;

    #endregion

    #region Private Instance Fields

    /// <summary>
    /// Disposal state flag.
    /// </summary>
    /// <remarks>
    /// Prevents multiple disposal attempts.
    /// </remarks>
    private bool _disposed = false;

    /// <summary>
    /// Parsed representation of the item's initial state for change tracking.
    /// </summary>
    private JsonNode? _initialJsonNode;

    /// <summary>
    /// Backing field for the actual item instance being proxied.
    /// </summary>
    private TItem _itemValue = null!;

    #endregion

    #region Public Properties

    /// <summary>
    /// Primary access point for consumers to interact with the proxied item.
    /// </summary>
    public TInterface Item => _proxy;

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Runs both system and domain-specific validation rules without modifying the item.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    public async Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken)
    {
        return await _validateAsyncDelegate(_item, cancellationToken);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Releases resources based on disposal state.
    /// </summary>
    /// <param name="disposing">
    /// True when called from Dispose(), false when called from finalizer.
    /// </param>
    /// <remarks>
    /// Standard disposal pattern implementation.
    /// </remarks>
    protected virtual void Dispose(
        bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _semaphore.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Core method that handles all proxy invocations.
    /// </summary>
    /// <param name="targetMethod">Intercepted method.</param>
    /// <param name="args">Method arguments.</param>
    /// <returns>Result of the method invocation.</returns>
    /// <exception cref="InvalidOperationException">
    /// When modifying a read-only item.
    /// </exception>
    /// <remarks>
    /// Called by the dynamic proxy for all method invocations.
    /// </remarks>
    protected object? OnInvoke(
        MethodInfo? targetMethod,
        object?[]? args)
    {
        // For read-only items, we only allow property getter methods to be called
        if (_isReadOnly && _propertyGetters.IsGetter(targetMethod) is false)
        {
            throw new InvalidOperationException($"The '{typeof(TInterface)}' is read-only.");
        }

        // Execute the method on the underlying item
        return targetMethod?.Invoke(_item, args);
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Gets tracked property changes by comparing initial and current JSON states using RFC 6902 diff.
    /// </summary>
    /// <returns>PropertyChange array for leaf-level differences, or null if no changes</returns>
    /// <remarks>
    /// Detects all modifications including nested objects and arrays using JSON diff comparison.
    /// Returns individual entries for each modified leaf property with JSON Pointer paths.
    /// Automatically consolidates array reordering operations.
    /// </remarks>
    internal PropertyChange[]? GetPropertyChanges()
    {
        // Serialize current item state to JsonNode for comparison
        var currentJsonNode = JsonSerializer.SerializeToNode(_item, _jsonSerializerOptions);

        // Compare initial and current states using RFC 6902 JSON Patch diff
        return PropertyChanges.Compare(
            _initialJsonNode ?? _jsonNodeEmpty,
            currentJsonNode ?? _jsonNodeEmpty);
    }

    #endregion

    #region Finalizer

    /// <summary>
    /// Finalizer for resource cleanup.
    /// </summary>
    /// <remarks>
    /// Calls Dispose(false) to release unmanaged resources.
    /// </remarks>
    ~ProxyManager()
    {
        Dispose(false);
    }

    #endregion
}
