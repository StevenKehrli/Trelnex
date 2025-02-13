using System.Linq.Expressions;
using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// A class to convert an expression using a TInterface to an expression using a TItem.
/// </summary>
/// <remarks>
/// <para>
/// https://stackoverflow.com/questions/14932779/how-to-change-a-type-in-an-expression-tree/14933106#14933106
/// </para>
/// </remarks>
/// <typeparam name="TInterface">The specified interface type.</typeparam>
/// <typeparam name="TItem">The specified item type that implements the specified interface type.</typeparam>
public class ExpressionConverter<TInterface, TItem>
    where TItem : TInterface
{
    /// <summary>
    /// The ParameterExpression node used to identify a parameter or a variable.
    /// </summary>
    private static readonly ParameterExpression _parameterExpression = Expression.Parameter(typeof(TItem));

    /// <summary>
    /// The <see cref="ExpressionRewriter"/> to rewrite the expression tree.
    /// </summary>
    private static readonly ExpressionRewriter _expressionRewriter = new ExpressionRewriter();

    internal Expression<Func<TItem, bool>> Convert(
        Expression<Func<TInterface, bool>> predicate)
    {
        // create a new expression body
        var body = _expressionRewriter.Visit(predicate.Body);

        // create the new expression
        return Expression.Lambda<Func<TItem, bool>>(body, _parameterExpression);
    }

    internal Expression<Func<TItem, TKey>> Convert<TKey>(
        Expression<Func<TInterface, TKey>> predicate)
    {
        // create a new expression body
        var body = _expressionRewriter.Visit(predicate.Body);

        // create the new expression
        return Expression.Lambda<Func<TItem, TKey>>(body, _parameterExpression);
    }

    private class ExpressionRewriter : ExpressionVisitor
    {
        /// <summary>
        /// The collection of interfaces for the TInterface type.
        /// </summary>
        private static readonly HashSet<string> _interfaces = GetInterfaces();

        /// <summary>
        /// The map from the expression member name to the TItem property.
        /// </summary>
        private static readonly Dictionary<string, PropertyInfo> _itemPropertiesByName = GetItemPropertiesByName();

        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return _parameterExpression;
        }

        protected override Expression VisitMember(
            MemberExpression node)
        {
            // check if a property expression
            if (node.Member is not PropertyInfo propertyInfo) return node;
            if (propertyInfo.DeclaringType is null) return node;
            if (_interfaces.Contains(propertyInfo.DeclaringType.Name) is false) return node;

            // get the member name
            var nodeMemberName = node.Member.Name;

            // find property of TItem
            if (_itemPropertiesByName.TryGetValue(nodeMemberName, out var property) is false)
            {
                throw new ArgumentException($"The '{typeof(TItem)}' does not contain a definition for '{nodeMemberName}'.");
            }

            // create the visit and create the property expression
            var nodeExpression = Visit(node.Expression);

            return Expression.Property(nodeExpression, property);
        }

        /// <summary>
        /// Create the collection interfaces from the interface stype.
        /// </summary>
        /// <returns>The <see cref="HashSet"/> of interfaces.</returns>
        private static HashSet<string> GetInterfaces()
        {
            var interfaces = new HashSet<string>();

            var queue = new Queue<Type>();
            queue.Enqueue(typeof(TInterface));

            while (queue.TryDequeue(out var currentType))
            {
                interfaces.Add(currentType.Name);

                var nextTypes = currentType.GetInterfaces();

                Array.ForEach(nextTypes, nextType => queue.Enqueue(nextType));
            }

            return interfaces;
        }

        /// <summary>
        /// Create the dictionary of item properties by their name.
        /// </summary>
        /// <returns>The <see cref="Dictionary"/> of item properties by their name.</returns>
        private static Dictionary<string, PropertyInfo> GetItemPropertiesByName()
        {
            var itemPropertiesByName = new Dictionary<string, PropertyInfo>();

            // enumerate all properties for the getters
            var properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                itemPropertiesByName[property.Name] = property;
            }

            return itemPropertiesByName;
        }
    }
}
