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
        ArgumentNullException.ThrowIfNull(predicate);

        // Create a new expression body by visiting the original expression
        var body = _expressionRewriter.Visit(predicate.Body);

        // Create the new lambda expression with the rewritten body and our parameter
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
        ArgumentNullException.ThrowIfNull(predicate);

        // Create a new expression body by visiting the original expression
        var body = _expressionRewriter.Visit(predicate.Body);

        // Create the new lambda expression with the rewritten body and our parameter
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
            // Only rewrite property expressions from our interfaces
            if (node.Member is not PropertyInfo propertyInfo) return node;
            if (propertyInfo.DeclaringType is null) return node;
            if (_interfaces.Contains(propertyInfo.DeclaringType.Name) is false) return node;

            // Get the member name from the expression
            var nodeMemberName = node.Member.Name;

            // Find the corresponding property in TItem
            if (_itemPropertiesByName.TryGetValue(nodeMemberName, out var property) is false)
            {
                throw new ArgumentException($"The '{typeof(TItem)}' does not contain a definition for '{nodeMemberName}'.");
            }

            // Visit the instance expression and create a new property expression
            var nodeExpression = Visit(node.Expression);

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
            var interfaces = new HashSet<string>();

            // Use a queue to collect all interfaces in the hierarchy
            var queue = new Queue<Type>();
            queue.Enqueue(typeof(TInterface));

            // Process the queue, adding each interface's name and queueing its base interfaces
            while (queue.TryDequeue(out var currentType))
            {
                interfaces.Add(currentType.Name);

                var nextTypes = currentType.GetInterfaces();
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
            var itemPropertiesByName = new Dictionary<string, PropertyInfo>();

            // Get all public instance properties of TItem
            var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                itemPropertiesByName[property.Name] = property;
            }

            return itemPropertiesByName;
        }

        #endregion
    }

    #endregion
}
