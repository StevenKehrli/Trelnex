using System.Reflection;
using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Provides core infrastructure for dynamic proxy management with change tracking, validation, and access control.
/// </summary>
/// <typeparam name="TInterface">The interface type that the proxy implements and exposes to consumers.</typeparam>
/// <typeparam name="TItem">The concrete implementation type that fulfills the interface contract.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="ProxyManager{TInterface, TItem}"/> abstract class is a foundational component of the
/// proxy infrastructure, serving as the base class for various proxy-based command and result classes
/// throughout the system. It provides essential functionality for:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Creating and managing dynamic proxies that implement the interface type
///     </description>
///   </item>
///   <item>
///     <description>
///       Intercepting and controlling method invocations on proxied objects
///     </description>
///   </item>
///   <item>
///     <description>
///       Enforcing read-only access based on operation type
///     </description>
///   </item>
///   <item>
///     <description>
///       Tracking property changes for audit events and change history
///     </description>
///   </item>
///   <item>
///     <description>
///       Ensuring thread safety through synchronization primitives
///     </description>
///   </item>
///   <item>
///     <description>
///       Providing validation capabilities through delegate-based validation
///     </description>
///   </item>
/// </list>
/// <para>
/// This class implements the Proxy design pattern, where a surrogate or placeholder object (the proxy)
/// controls access to another object (the real subject). The proxy sits between clients and the real subject,
/// intercepting all method calls to perform additional processing before delegating to the real subject.
/// </para>
/// <para>
/// Key design characteristics include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Separation of interface from implementation, allowing interface-based programming
///     </description>
///   </item>
///   <item>
///     <description>
///       Transparent interception of property access and modification
///     </description>
///   </item>
///   <item>
///     <description>
///       Thread safety through semaphore-based synchronization
///     </description>
///   </item>
///   <item>
///     <description>
///       Proper resource management through IDisposable implementation
///     </description>
///   </item>
/// </list>
/// <para>
/// Concrete subclasses like <see cref="ReadResult{TInterface, TItem}"/>, <see cref="QueryResult{TInterface, TItem}"/>,
/// and <see cref="SaveCommand{TInterface, TItem}"/> extend this base class to provide specialized
/// functionality for different operation types while inheriting the core proxy management capabilities.
/// </para>
/// </remarks>
/// <seealso cref="IDisposable"/>
/// <seealso cref="ReadResult{TInterface, TItem}"/>
/// <seealso cref="QueryResult{TInterface, TItem}"/>
/// <seealso cref="SaveCommand{TInterface, TItem}"/>
internal abstract class ProxyManager<TInterface, TItem> : IDisposable
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Static Fields

    /// <summary>
    /// A cached collection of property getter methods for the <typeparamref name="TItem"/> type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This static field provides a performance optimization by caching reflection information
    /// about property getter methods once per type, rather than discovering them repeatedly
    /// for each instance.
    /// </para>
    /// <para>
    /// It is used by the <see cref="OnInvoke"/> method to determine if a method being invoked
    /// is a property getter, which is essential for implementing read-only protection. Property
    /// getters are always allowed, even in read-only mode, while setters and other methods
    /// are restricted when <see cref="_isReadOnly"/> is <see langword="true"/>.
    /// </para>
    /// <para>
    /// The field is initialized using the <see cref="PropertyGetters{TItem}.Create"/> factory method,
    /// which uses reflection to discover and cache all property getter methods for the type.
    /// </para>
    /// </remarks>
    /// <seealso cref="OnInvoke"/>
    /// <seealso cref="_isReadOnly"/>
    private static readonly PropertyGetters<TItem> _propertyGetters = PropertyGetters<TItem>.Create();

    /// <summary>
    /// A handler that intercepts property operations to enable change tracking for <typeparamref name="TItem"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This static field provides the core mechanism for tracking property changes in proxied objects.
    /// It intercepts property setter method calls, captures the old and new values, and determines
    /// whether the property is marked for change tracking with <see cref="TrackChangeAttribute"/>.
    /// </para>
    /// <para>
    /// The field is initialized using the <see cref="TrackProperties{TItem}.Create"/> factory method,
    /// which uses reflection to discover properties marked with <see cref="TrackChangeAttribute"/>
    /// and caches this information for efficient runtime access.
    /// </para>
    /// <para>
    /// When a tracked property changes, the <see cref="OnInvoke"/> method uses the information
    /// provided by this handler to record the change in the <see cref="_propertyChanges"/> collection.
    /// These tracked changes are later available for audit events and other tracking purposes.
    /// </para>
    /// </remarks>
    /// <seealso cref="OnInvoke"/>
    /// <seealso cref="_propertyChanges"/>
    /// <seealso cref="TrackChangeAttribute"/>
    private static readonly TrackProperties<TItem> _trackProperties = TrackProperties<TItem>.Create();

    #endregion

    #region Protected Instance Fields

    /// <summary>
    /// Controls whether modifications to the item are permitted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field determines the access mode for the proxied item. When set to <see langword="true"/>,
    /// the proxy will prevent all modifications to the item's properties by throwing an
    /// <see cref="InvalidOperationException"/> when a property setter or modifying method is called.
    /// </para>
    /// <para>
    /// Property getters are always allowed regardless of this setting, enabling read-only access
    /// to property values even when modifications are prohibited.
    /// </para>
    /// <para>
    /// This field is typically set:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see langword="true"/> for read-only operations (Read, Delete, Query results)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see langword="false"/> for modifiable operations (Create, Update)
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The access control check is performed by the <see cref="OnInvoke"/> method using
    /// <see cref="_propertyGetters"/> to determine if the method being invoked is a property getter.
    /// </para>
    /// </remarks>
    /// <seealso cref="OnInvoke"/>
    /// <seealso cref="_propertyGetters"/>
    protected bool _isReadOnly;

    /// <summary>
    /// The actual item instance being proxied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field holds the concrete implementation instance that fulfills the operations
    /// defined by <typeparamref name="TInterface"/>. It is the real subject in the Proxy pattern
    /// that the proxy controls access to.
    /// </para>
    /// <para>
    /// The implementation instance contains the actual property values and business logic,
    /// while the proxy provided through <see cref="_proxy"/> intercepts all method calls
    /// to provide additional functionality like change tracking and access control.
    /// </para>
    /// <para>
    /// This field is initialized during creation of proxy manager subclasses, either through
    /// constructor parameters or factory methods, and is marked with the <see langword="null!"/>
    /// annotation to indicate that it will be assigned before use despite being defined
    /// without an initializer.
    /// </para>
    /// </remarks>
    /// <seealso cref="_proxy"/>
    /// <seealso cref="OnInvoke"/>
    protected TItem _item = null!;

    /// <summary>
    /// The proxy implementation that wraps the underlying item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field holds the dynamically generated proxy instance that implements <typeparamref name="TInterface"/>
    /// and intercepts all method calls before delegating to the real implementation in <see cref="_item"/>.
    /// </para>
    /// <para>
    /// The proxy is created using the <see cref="ItemProxy{TInterface, TItem}.Create"/> factory method,
    /// which generates a dynamic implementation of the interface that delegates all method calls to
    /// the <see cref="OnInvoke"/> method of this proxy manager.
    /// </para>
    /// <para>
    /// This field is exposed through the <see cref="Item"/> property, allowing consumers to work with
    /// the proxied item through its interface without being aware of the proxy infrastructure.
    /// </para>
    /// <para>
    /// The proxy is marked with the <see langword="null!"/> annotation to indicate that it will be assigned
    /// before use despite being defined without an initializer.
    /// </para>
    /// </remarks>
    /// <seealso cref="Item"/>
    /// <seealso cref="OnInvoke"/>
    protected TInterface _proxy = null!;

    /// <summary>
    /// A synchronization primitive that ensures thread-safe access to the proxied item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This semaphore provides concurrency control for operations on the proxied item,
    /// ensuring that only one thread can modify the item at a time. It prevents race conditions
    /// and data corruption that could occur if multiple threads attempted to modify the item
    /// simultaneously.
    /// </para>
    /// <para>
    /// The semaphore is initialized with:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Initial count of 1, allowing a single thread to enter the critical section
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Maximum count of 1, preventing multiple threads from entering even if released multiple times
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The semaphore is used in multiple places:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="OnInvoke"/> for synchronizing all method calls to the proxied item
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Various methods in subclasses for synchronizing higher-level operations
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// This resource is properly disposed when the proxy manager is disposed.
    /// </para>
    /// </remarks>
    /// <seealso cref="OnInvoke"/>
    /// <seealso cref="Dispose(bool)"/>
    protected readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// The delegate that performs validation on the proxied item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This delegate encapsulates the validation logic for the item type, allowing the proxy
    /// manager to validate the item without knowing the specific validation rules that apply.
    /// </para>
    /// <para>
    /// The delegate is invoked by the <see cref="ValidateAsync"/> method to perform validation
    /// against business rules and data constraints. It typically includes:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Base validation rules for <see cref="IBaseItem"/> properties
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Domain-specific validation rules for the concrete item type
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// This field is initialized during creation of proxy manager subclasses, typically through
    /// constructor parameters or factory methods, and is marked with the <see langword="null!"/>
    /// annotation to indicate that it will be assigned before use despite being defined
    /// without an initializer.
    /// </para>
    /// </remarks>
    /// <seealso cref="ValidateAsync"/>
    /// <seealso cref="ValidateAsyncDelegate{TInterface, TItem}"/>
    protected ValidateAsyncDelegate<TInterface, TItem> _validateAsyncDelegate = null!;

    #endregion

    #region Private Instance Fields

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field implements a common pattern for tracking the disposal state of an object
    /// to prevent multiple disposal attempts and to enforce the dispose pattern correctly.
    /// </para>
    /// <para>
    /// When the object is disposed (either through an explicit call to <see cref="Dispose"/>
    /// or through the finalizer), this field is set to <see langword="true"/> to indicate that
    /// the object is no longer valid for use.
    /// </para>
    /// <para>
    /// The <see cref="Dispose(bool)"/> method checks this field to prevent redundant disposal
    /// operations, which could lead to exceptions if resources are released multiple times.
    /// </para>
    /// </remarks>
    /// <seealso cref="Dispose"/>
    /// <seealso cref="Dispose(bool)"/>
    private bool _disposed = false;

    /// <summary>
    /// The collection that tracks all property changes made to the proxied item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This collection maintains a chronological history of all property modifications made to the
    /// proxied item. Each change record includes:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>The name of the property that was changed</description>
    ///   </item>
    ///   <item>
    ///     <description>The original value before the change</description>
    ///   </item>
    ///   <item>
    ///     <description>The new value after the change</description>
    ///   </item>
    /// </list>
    /// <para>
    /// Changes are tracked only for properties marked with the <see cref="TrackChangeAttribute"/>,
    /// and only when the property value actually changes (if the new value is equal to the old value,
    /// no change is recorded).
    /// </para>
    /// <para>
    /// This change history is used to:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Generate audit events that record who changed what and when
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Support optimistic concurrency control by detecting conflicting changes
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Enable debugging and troubleshooting of data modifications
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The collection is exposed through the <see cref="GetPropertyChanges"/> method,
    /// which returns the changes as an ordered array.
    /// </para>
    /// </remarks>
    /// <seealso cref="GetPropertyChanges"/>
    /// <seealso cref="TrackChangeAttribute"/>
    /// <seealso cref="OnInvoke"/>
    private readonly PropertyChanges _propertyChanges = new();

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the proxied item instance that implements <typeparamref name="TInterface"/>.
    /// </summary>
    /// <value>
    /// A proxy-wrapped instance of type <typeparamref name="TInterface"/> that provides controlled
    /// access to the underlying <typeparamref name="TItem"/> instance.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides the primary access point for consumers to interact with the
    /// proxied item. It returns the dynamic proxy instance stored in <see cref="_proxy"/>,
    /// which implements <typeparamref name="TInterface"/> and intercepts all method calls.
    /// </para>
    /// <para>
    /// Through this property, consumers can:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Read property values from the underlying item (always permitted)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Modify property values if the proxy is not read-only (i.e., if <see cref="_isReadOnly"/> is <see langword="false"/>)
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The property provides a clean abstraction that hides the proxy infrastructure details
    /// from consumers, allowing them to work with the item through its interface without
    /// being aware of the proxy implementation.
    /// </para>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// // Read a property value (always allowed)
    /// var id = proxyManager.Item.Id;
    /// 
    /// // Modify a property value (allowed only if not read-only)
    /// proxyManager.Item.Name = "New Name";
    /// </code>
    /// </remarks>
    /// <seealso cref="_proxy"/>
    /// <seealso cref="_isReadOnly"/>
    /// <seealso cref="OnInvoke"/>
    public TInterface Item => _proxy;

    #endregion

    #region Public Methods

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="ProxyManager{TInterface, TItem}"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the <see cref="IDisposable.Dispose"/> method as part of the standard
    /// .NET dispose pattern. It provides proper cleanup of resources when the object is no longer needed.
    /// </para>
    /// <para>
    /// The implementation:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       Calls <see cref="Dispose(bool)"/> with <see langword="true"/> to release both managed and
    ///       unmanaged resources, indicating that this is an explicit dispose call
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Calls <see cref="GC.SuppressFinalize"/> to prevent the finalizer from running,
    ///       as the resources have already been properly released
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// This method does not throw exceptions, in accordance with the general .NET guideline
    /// that Dispose methods should not throw exceptions, as they are often called in finally blocks.
    /// </para>
    /// <para>
    /// After this method is called, the object should not be used, as its resources have been released.
    /// </para>
    /// </remarks>
    /// <seealso cref="Dispose(bool)"/>
    /// <seealso cref="IDisposable"/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Validates the current state of the proxied item against configured validation rules.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous validation operation. The task result contains a
    /// <see cref="ValidationResult"/> indicating whether the item is valid and listing any
    /// validation errors that were found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs validation of the item without modifying it, executing both
    /// system-defined and domain-specific validation rules to determine if the item
    /// is in a valid state.
    /// </para>
    /// <para>
    /// The implementation delegates to the configured <see cref="_validateAsyncDelegate"/> to
    /// perform the actual validation, which typically includes:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Base validation rules for <see cref="IBaseItem"/> properties (Id, PartitionKey, TypeName, etc.)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Domain-specific validation rules defined for the concrete item type
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The validation is performed asynchronously to accommodate validation rules that might
    /// require external resource access or time-consuming operations.
    /// </para>
    /// <para>
    /// This method is available to all proxy manager subclasses, providing consistent validation
    /// capabilities across different operation types.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <seealso cref="_validateAsyncDelegate"/>
    /// <seealso cref="FluentValidation.Results.ValidationResult"/>
    public async Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken)
    {
        return await _validateAsyncDelegate(_item, cancellationToken);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Releases resources used by the <see cref="ProxyManager{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources (called from <see cref="Dispose()"/>);
    /// <see langword="false"/> to release only unmanaged resources (called from the finalizer).
    /// </param>
    /// <remarks>
    /// <para>
    /// This method implements the disposal pattern for the proxy manager, providing proper
    /// cleanup of resources when the object is no longer needed. It is designed to be overridden
    /// by derived classes that need to dispose additional resources.
    /// </para>
    /// <para>
    /// When <paramref name="disposing"/> is <see langword="true"/> (called from <see cref="Dispose()"/>),
    /// this method releases managed resources:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The <see cref="_semaphore"/> is disposed to release the synchronization primitive
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// When <paramref name="disposing"/> is <see langword="false"/> (called from the finalizer),
    /// only unmanaged resources (if any) would be released, but this base implementation
    /// does not manage any unmanaged resources.
    /// </para>
    /// <para>
    /// The method uses the <see cref="_disposed"/> flag to prevent multiple disposal of the
    /// same resources, which is an important safety measure in the disposal pattern.
    /// </para>
    /// <para>
    /// Derived classes that override this method should:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       First call the base implementation: <c>base.Dispose(disposing)</c>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Release their own resources, checking the <paramref name="disposing"/> parameter
    ///       to determine which resources to release
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Dispose()"/>
    /// <seealso cref="_semaphore"/>
    /// <seealso cref="_disposed"/>
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
    /// Handles all method invocations on the proxy object, providing core proxy functionality.
    /// </summary>
    /// <param name="targetMethod">The intercepted method being called on the proxy.</param>
    /// <param name="args">The arguments passed to the method.</param>
    /// <returns>The result of the method invocation on the underlying item.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to modify a read-only item with a non-getter method.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the core method of the proxy pattern implementation. It is called by the dynamic proxy
    /// (<see cref="_proxy"/>) whenever a method is invoked on the proxy object. The method:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       Acquires a lock on the item using <see cref="_semaphore"/> to ensure thread safety
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Enforces read-only restrictions if <see cref="_isReadOnly"/> is <see langword="true"/>,
    ///       allowing only property getters (identified using <see cref="_propertyGetters"/>)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Delegates the actual method invocation to <see cref="_trackProperties"/>, which:
    ///       <list type="bullet">
    ///         <item>
    ///           <description>Invokes the method on the underlying <see cref="_item"/></description>
    ///         </item>
    ///         <item>
    ///           <description>
    ///             Captures property changes for properties marked with <see cref="TrackChangeAttribute"/>
    ///           </description>
    ///         </item>
    ///       </list>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Records property changes in <see cref="_propertyChanges"/> if the method modified a tracked property
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Releases the lock on the item to allow other threads to access it
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Returns the result of the method invocation
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// This method is called by the generated proxy for all interface method calls, including
    /// property getters and setters (which are compiled to method calls by the C# compiler).
    /// </para>
    /// </remarks>
    /// <seealso cref="_isReadOnly"/>
    /// <seealso cref="_propertyGetters"/>
    /// <seealso cref="_trackProperties"/>
    /// <seealso cref="_propertyChanges"/>
    /// <seealso cref="_semaphore"/>
    protected object? OnInvoke(
        MethodInfo? targetMethod,
        object?[]? args)
    {
        // Acquire a lock to ensure thread safety during method invocation
        // This prevents concurrent modifications that could lead to race conditions
        // The semaphore is configured with an initial and maximum count of 1,
        // ensuring only one thread can access this critical section at a time
        _semaphore.Wait();

        try
        {
            // For read-only items, we only allow property getter methods to be called
            // This enforces immutability for items like query results or delete commands
            // Property getters are identified using the cached _propertyGetters collection
            // which contains reflection data about all getter methods in the TItem type
            if (_isReadOnly && _propertyGetters.IsGetter(targetMethod) is false)
            {
                throw new InvalidOperationException($"The '{typeof(TInterface)}' is read-only.");
            }

            // Delegate the actual method invocation to the _trackProperties helper
            // This specialized class handles property access interception and:
            // 1. Invokes the actual method on the underlying concrete item
            // 2. Detects if the invocation is setting a property value
            // 3. Captures old and new values for tracked properties
            // 4. Returns an InvokeResult with all relevant metadata about the invocation
            var invokeResult = _trackProperties.Invoke(targetMethod, _item, args);

            // If this invocation modified a property marked with [TrackChange]
            // and the property value actually changed (old != new),
            // we record this change in the _propertyChanges collection
            // This change history is later used for audit events and concurrency control
            if (invokeResult.IsTracked)
            {
                _propertyChanges.Add(
                    propertyName: invokeResult.PropertyName,
                    oldValue: invokeResult.OldValue, 
                    newValue: invokeResult.NewValue);
            }

            // Return the result of the method invocation to the caller
            // This maintains the transparent proxy behavior, where the
            // proxy appears to be the real object to the caller
            return invokeResult.Result;
        }
        finally
        {
            // Always release the semaphore to prevent deadlocks
            // This executes even if an exception occurs during method invocation
            // Ensures proper resource cleanup and allows other threads to proceed
            _semaphore.Release();
        }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Retrieves the collection of tracked property changes made to the proxied item.
    /// </summary>
    /// <returns>
    /// An ordered array of <see cref="PropertyChange"/> objects representing all changes made
    /// to tracked properties of the item since it was created or last saved, or <see langword="null"/>
    /// if no changes have been made.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides access to the history of property changes tracked by the proxy manager.
    /// It returns a copy of the change history as an array, preserving the chronological order
    /// in which the changes were made.
    /// </para>
    /// <para>
    /// The method returns <see langword="null"/> when no properties have been changed, which allows
    /// callers to quickly determine if any changes exist without checking array length.
    /// </para>
    /// <para>
    /// This method is typically used when creating audit events for operations, where the
    /// property changes are included to record what was modified as part of the operation.
    /// </para>
    /// <para>
    /// Only properties marked with <see cref="TrackChangeAttribute"/> are included in the
    /// returned changes, as these are the only properties for which changes are tracked.
    /// </para>
    /// </remarks>
    /// <seealso cref="_propertyChanges"/>
    /// <seealso cref="PropertyChange"/>
    /// <seealso cref="TrackChangeAttribute"/>
    internal PropertyChange[]? GetPropertyChanges()
    {
        return _propertyChanges.ToArray();
    }

    #endregion

    #region Finalizer

    /// <summary>
    /// Finalizer (destructor) for the <see cref="ProxyManager{TInterface, TItem}"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This finalizer is part of the standard .NET dispose pattern. It provides a last-resort
    /// cleanup mechanism for releasing resources when an object is garbage collected without
    /// having had its <see cref="Dispose"/> method explicitly called.
    /// </para>
    /// <para>
    /// The finalizer:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Calls <see cref="Dispose(bool)"/> with <see langword="false"/> to indicate that
    ///       only unmanaged resources should be released, as managed resources may no longer
    ///       be accessible during finalization
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Is automatically called by the garbage collector when the object becomes unreachable,
    ///       but only if <see cref="GC.SuppressFinalize"/> has not been called for the object
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The presence of this finalizer means that objects of this class may live longer in memory
    /// (until a garbage collection cycle that includes finalization), which is why the <see cref="Dispose"/>
    /// method calls <see cref="GC.SuppressFinalize"/> to prevent the finalizer from running when
    /// explicit disposal has already occurred.
    /// </para>
    /// <para>
    /// This class follows the Dispose Pattern exactly as recommended by Microsoft:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>A public non-virtual <see cref="IDisposable.Dispose"/> implementation</description>
    ///   </item>
    ///   <item>
    ///     <description>A protected virtual <see cref="Dispose(bool)"/> method</description>
    ///   </item>
    ///   <item>
    ///     <description>A finalizer that calls <see cref="Dispose(bool)"/> with <see langword="false"/></description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Dispose()"/>
    /// <seealso cref="Dispose(bool)"/>
    ~ProxyManager()
    {
        Dispose(false);
    }

    #endregion
}
