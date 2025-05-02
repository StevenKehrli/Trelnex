using System.Linq.Expressions;
using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// Converts expressions based on an interface type to equivalent expressions using a concrete implementation type.
/// </summary>
/// <remarks>
/// <para>
/// This utility allows reusing LINQ expressions defined against interfaces with concrete implementations
/// of those interfaces. It works by rewriting the expression tree, mapping interface members to their
/// corresponding implementation members.
/// </para>
/// <para>
/// Based on the solution from Stack Overflow:
/// https://stackoverflow.com/questions/14932779/how-to-change-a-type-in-an-expression-tree/14933106#14933106
/// </para>
/// </remarks>
/// <typeparam name="TInterface">The interface type used in the original expression.</typeparam>
/// <typeparam name="TItem">The concrete type that implements <typeparamref name="TInterface"/> to target in the converted expression.</typeparam>
public class ExpressionConverter<TInterface, TItem>
    where TItem : TInterface
{
    #region Static Fields

    /// <summary>
    /// The parameter expression representing an instance of <typeparamref name="TItem"/> in the rewritten expressions.
    /// </summary>
    private static readonly ParameterExpression _parameterExpression = Expression.Parameter(typeof(TItem));

    /// <summary>
    /// The expression visitor that performs the actual rewriting of expression nodes.
    /// </summary>
    private static readonly ExpressionRewriter _expressionRewriter = new ExpressionRewriter();

    #endregion

    #region Internal Methods

    /// <summary>
    /// Converts a boolean predicate expression from interface type to implementation type.
    /// </summary>
    /// <param name="predicate">The expression using <typeparamref name="TInterface"/> to convert.</param>
    /// <returns>An equivalent expression using <typeparamref name="TItem"/> instead of <typeparamref name="TInterface"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when a property in the expression doesn't have a matching property in <typeparamref name="TItem"/>.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
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
    /// Converts a projection expression from interface type to implementation type.
    /// </summary>
    /// <typeparam name="TKey">The type of the result produced by the projection.</typeparam>
    /// <param name="predicate">The projection expression using <typeparamref name="TInterface"/> to convert.</param>
    /// <returns>An equivalent projection expression using <typeparamref name="TItem"/> instead of <typeparamref name="TInterface"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when a property in the expression doesn't have a matching property in <typeparamref name="TItem"/>.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
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
    /// Expression visitor that rewrites expression nodes to use the target implementation type.
    /// </summary>
    private class ExpressionRewriter : ExpressionVisitor
    {
        #region Static Fields

        /// <summary>
        /// Set of interface names that are part of the type hierarchy of <typeparamref name="TInterface"/>.
        /// </summary>
        /// <remarks>
        /// This includes <typeparamref name="TInterface"/> and all interfaces it inherits from.
        /// </remarks>
        private static readonly HashSet<string> _interfaces = GetInterfaces();

        /// <summary>
        /// Mapping from property names to <typeparamref name="TItem"/> property information.
        /// </summary>
        private static readonly Dictionary<string, PropertyInfo> _itemPropertiesByName = GetItemPropertiesByName();

        #endregion

        #region Protected Methods

        /// <summary>
        /// Visits a parameter expression and replaces it with the parameter for the target type.
        /// </summary>
        /// <param name="node">The parameter expression to visit.</param>
        /// <returns>The <see cref="_parameterExpression"/> representing the target type parameter.</returns>
        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return _parameterExpression;
        }

        /// <summary>
        /// Visits a member expression and rewrites it to use the corresponding member of the target type.
        /// </summary>
        /// <param name="node">The member expression to visit.</param>
        /// <returns>A rewritten member expression using the target type's member, or the original expression if no rewriting is needed.</returns>
        /// <exception cref="ArgumentException">Thrown when a property in <typeparamref name="TInterface"/> doesn't have a matching property in <typeparamref name="TItem"/>.</exception>
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
        /// Creates a set containing the names of all interfaces in the <typeparamref name="TInterface"/> hierarchy.
        /// </summary>
        /// <returns>A <see cref="HashSet{T}"/> containing interface names.</returns>
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
        /// Creates a dictionary mapping property names to PropertyInfo objects for <typeparamref name="TItem"/>.
        /// </summary>
        /// <returns>A dictionary mapping property names to their corresponding <see cref="PropertyInfo"/> objects.</returns>
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
