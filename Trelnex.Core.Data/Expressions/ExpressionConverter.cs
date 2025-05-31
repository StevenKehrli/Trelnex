using System.Linq.Expressions;
using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// Converts interface-based expressions to concrete implementation expressions.
/// </summary>
/// <remarks>
/// Rewrites expression trees by mapping interface members to implementation members.
/// </remarks>
/// <typeparam name="TInterface">Interface in original expression.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
public class ExpressionConverter<TInterface, TItem>
    where TItem : TInterface
{
    #region Static Fields

    /// <summary>
    /// Parameter for TItem in rewritten expressions.
    /// </summary>
    private static readonly ParameterExpression _parameterExpression = Expression.Parameter(typeof(TItem));

    /// <summary>
    /// Visitor that rewrites expression nodes.
    /// </summary>
    private static readonly ExpressionRewriter _expressionRewriter = new ExpressionRewriter();

    #endregion

    #region Internal Methods

    /// <summary>
    /// Converts a boolean predicate expression.
    /// </summary>
    /// <param name="predicate">Expression to convert.</param>
    /// <returns>Equivalent implementation-based expression.</returns>
    /// <exception cref="ArgumentException">When property has no match in TItem.</exception>
    /// <exception cref="ArgumentNullException">When predicate is null.</exception>
    internal Expression<Func<TItem, bool>> Convert(
        Expression<Func<TInterface, bool>> predicate)
    {
        // Ensure we don't attempt to convert a null expression
        ArgumentNullException.ThrowIfNull(predicate);

        // Create a new expression body by visiting the original expression
        // The expression visitor will recursively traverse the expression tree
        // and replace all interface member references with implementation members
        // For example: e => e.IsDeleted == false becomes e => ((BaseItem)e).IsDeleted == false
        var body = _expressionRewriter.Visit(predicate.Body);

        // Create a new lambda expression with:
        // 1. The rewritten body containing implementation type references
        // 2. Our parameter of type TItem instead of the original TInterface parameter
        // This produces a new lambda compatible with TItem instead of TInterface
        // while preserving the same logical conditions as the original expression
        return Expression.Lambda<Func<TItem, bool>>(body, _parameterExpression);
    }

    /// <summary>
    /// Converts a projection expression.
    /// </summary>
    /// <typeparam name="TKey">Result type of the projection.</typeparam>
    /// <param name="predicate">Expression to convert.</param>
    /// <returns>Equivalent implementation-based projection.</returns>
    /// <exception cref="ArgumentException">When property has no match in TItem.</exception>
    /// <exception cref="ArgumentNullException">When predicate is null.</exception>
    internal Expression<Func<TItem, TKey>> Convert<TKey>(
        Expression<Func<TInterface, TKey>> predicate)
    {
        // Ensure we don't attempt to convert a null expression
        ArgumentNullException.ThrowIfNull(predicate);

        // Visit the body of the original expression to rewrite it
        // This handles projections like e => e.Name or e => new { e.Id, e.Name }
        // The visitor traverses the tree and rewrites all interface member accesses
        // to use the concrete implementation type's equivalent members
        var body = _expressionRewriter.Visit(predicate.Body);

        // Create a new lambda expression with the rewritten body
        // This maintains the same semantic meaning as the original expression
        // but works with TItem instead of TInterface
        // The generic TKey type allows this to handle any return type, including:
        // - Simple property projections (string, int, etc.)
        // - Complex object projections (anonymous types, DTOs)
        // - Calculated expressions (e => e.Price * e.Quantity)
        return Expression.Lambda<Func<TItem, TKey>>(body, _parameterExpression);
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Rewrites expression nodes to use the target implementation type.
    /// </summary>
    private class ExpressionRewriter : ExpressionVisitor
    {
        #region Static Fields

        /// <summary>
        /// Interface names in TInterface hierarchy.
        /// </summary>
        private static readonly HashSet<string> _interfaces = GetInterfaces();

        /// <summary>
        /// Maps property names to TItem properties.
        /// </summary>
        private static readonly Dictionary<string, PropertyInfo> _itemPropertiesByName = GetItemPropertiesByName();

        #endregion

        #region Protected Methods

        /// <inheritdoc/>
        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return _parameterExpression;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">When property has no match in TItem.</exception>
        protected override Expression VisitMember(
            MemberExpression node)
        {
            // We only need to rewrite property accesses from our interfaces
            // Skip method calls, fields, or any non-property member access
            if (node.Member is not PropertyInfo propertyInfo) return node;

            // Skip if the declaring type is null (shouldn't happen in practice)
            if (propertyInfo.DeclaringType is null) return node;

            // Only rewrite properties from interfaces in our hierarchy
            // If the property comes from a different type entirely, we leave it as-is
            // This allows expressions to include references to unrelated types without errors
            if (_interfaces.Contains(propertyInfo.DeclaringType.Name) is false) return node;

            // Extract the property name we need to locate in the target type
            // For example, if accessing IBaseItem.Id, we need to find TItem.Id
            var nodeMemberName = node.Member.Name;

            // Find the corresponding property in the concrete TItem type
            // The mapping from interface property to implementation is by name matching
            // This assumes TItem properly implements all interface properties with matching names
            if (_itemPropertiesByName.TryGetValue(nodeMemberName, out var property) is false)
            {
                // If the property doesn't exist in TItem, it's a contract violation
                // TItem must implement all properties of TInterface and its base interfaces
                throw new ArgumentException($"The '{typeof(TItem)}' does not contain a definition for '{nodeMemberName}'.");
            }

            // Recursively visit the instance expression (this handles nested properties)
            // For example: item.Address.City - first visits item.Address, then accesses .City
            var nodeExpression = Visit(node.Expression);

            // Construct a new property access expression using the TItem property
            // This effectively maps IBaseItem.Id to BaseItem.Id in the expression tree
            return Expression.Property(nodeExpression, property);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets names of all interfaces in TInterface hierarchy.
        /// </summary>
        /// <returns>Set of interface names.</returns>
        private static HashSet<string> GetInterfaces()
        {
            // We build a set of interface names rather than types for easier lookup
            // Using a HashSet provides O(1) lookups during expression rewriting
            var interfaces = new HashSet<string>();

            // Use a breadth-first approach with a queue to discover all interfaces
            // This ensures we capture the complete interface hierarchy, including:
            // 1. The primary TInterface itself
            // 2. All interfaces that TInterface inherits from
            // 3. All interfaces inherited by those interfaces, recursively
            var queue = new Queue<Type>();
            queue.Enqueue(typeof(TInterface));

            // Process the queue until we've visited all interfaces in the hierarchy
            // This is a classic breadth-first traversal of the interface inheritance graph
            while (queue.TryDequeue(out var currentType))
            {
                // Add the current interface name to our set
                // We use Name rather than FullName to support nested types
                // Note: This means we can't distinguish between same-named interfaces
                // in different namespaces, but that's a rare edge case
                interfaces.Add(currentType.Name);

                // Get all interfaces that this interface inherits from
                // For example, if TInterface is IQueryResult<T>, this includes IBaseItem
                var nextTypes = currentType.GetInterfaces();

                // Add each base interface to the queue for processing
                // This ensures we discover the complete inheritance hierarchy
                Array.ForEach(nextTypes, nextType => queue.Enqueue(nextType));
            }

            return interfaces;
        }

        /// <summary>
        /// Maps property names to PropertyInfo objects for TItem.
        /// </summary>
        /// <returns>Dictionary of property names to PropertyInfo objects.</returns>
        private static Dictionary<string, PropertyInfo> GetItemPropertiesByName()
        {
            // Create a fast lookup dictionary that maps property names to their PropertyInfo objects
            // This provides O(1) lookups when mapping interface properties to implementation properties
            var itemPropertiesByName = new Dictionary<string, PropertyInfo>();

            // Get all public instance properties of TItem
            // We only use public properties since interface members must be public,
            // and we only need instance properties (not static ones)
            var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            // Build the name-to-property mapping dictionary
            // If TItem has a property with the same name as a property in TInterface,
            // we'll use it as the corresponding implementation property
            foreach (var property in properties)
            {
                // In case of duplicate property names (which shouldn't happen in well-designed types),
                // the last one wins - this matches the behavior of most reflection-based mapping systems
                itemPropertiesByName[property.Name] = property;
            }

            return itemPropertiesByName;
        }

        #endregion
    }

    #endregion
}
