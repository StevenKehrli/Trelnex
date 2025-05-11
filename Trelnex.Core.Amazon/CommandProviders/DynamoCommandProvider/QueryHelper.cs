using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using DynamoExpression = Amazon.DynamoDBv2.DocumentModel.Expression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// Translates LINQ expressions to DynamoDB query expressions.
/// </summary>
/// <typeparam name="T">The type of items being queried.</typeparam>
/// <remarks>
/// Converts C# LINQ expressions into DynamoDB's expression format.
/// Supports equality, comparison, string operations, null checks, and logical operators.
/// </remarks>
public class QueryHelper<T>
{
    #region Private Fields

    /// <summary>
    /// The DynamoDB WHERE expression.
    /// </summary>
    private readonly DynamoExpression? _dynamoWhereExpression;

    /// <summary>
    /// JSON serializer options for debugging and logging.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    /// <summary>
    /// Stack of LINQ method call expressions.
    /// </summary>
    private readonly Stack<MethodCallExpression> _methodCallExpressions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryHelper{T}"/> class.
    /// </summary>
    /// <param name="dynamoWhereExpression">The translated DynamoDB WHERE expression.</param>
    /// <param name="methodCallExpressions">Stack of LINQ method calls.</param>
    private QueryHelper(
        DynamoExpression? dynamoWhereExpression,
        Stack<MethodCallExpression> methodCallExpressions)
    {
        _dynamoWhereExpression = dynamoWhereExpression;
        _methodCallExpressions = methodCallExpressions;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the DynamoDB WHERE expression.
    /// </summary>
    public DynamoExpression? DynamoWhereExpression => _dynamoWhereExpression;

    #endregion

    #region Public Methods

    /// <summary>
    /// Applies any non-WHERE LINQ expressions to the given source.
    /// </summary>
    /// <param name="source">The collection of items.</param>
    /// <returns>The filtered and transformed collection.</returns>
    /// <exception cref="NotSupportedException">Thrown when a LINQ expression cannot be processed.</exception>
    /// <remarks>
    /// Applies OrderBy, Skip, and Take operations in-memory.
    /// </remarks>
    public IEnumerable<T> Filter(
        IEnumerable<T> source)
    {
        var result = source;

        // Process method call expressions until the stack is empty.
        while (_methodCallExpressions.Count > 0)
        {
            var queryable = result!.AsQueryable();

            // Get the next method call expression from the stack (LIFO).
            var methodCallExpression = _methodCallExpressions.Pop();

            // Get the method call parameter.  The parameter is the argument to the LINQ method (e.g., the lambda expression in `OrderBy(x => x.Name)`).
            object? parameter;

            var argument = methodCallExpression.Arguments[1];

            // Handle different types of arguments that can be passed to the LINQ methods.
            if (argument is ConstantExpression ce)
            {
                // If the argument is a constant expression, get its value.
                parameter = ce.Value;
            }
            else if (argument is UnaryExpression ue && ue.Operand is LambdaExpression ule)
            {
                // If the argument is a unary expression containing a lambda expression, compile the lambda expression.
                parameter = LinqExpression.Lambda(ule.Body, ule.Parameters);
            }
            else if (argument is LambdaExpression le)
            {
                // If the argument is a lambda expression, use it directly.
                parameter = le;
            }
            else
            {
                // If the argument is none of the above, it's an unsupported expression type.
                throw new NotSupportedException($"Filter() does not support '{methodCallExpression}'.");
            }

            // Invoke the method call expression.  This applies the LINQ method to the queryable with the extracted parameter.
            result = methodCallExpression.Method.Invoke(null, [ queryable, parameter ]) as IEnumerable<T>;
        }

        return result!;
    }

    /// <summary>
    /// Creates a QueryHelper instance from a LINQ expression.
    /// </summary>
    /// <param name="linqExpression">The LINQ expression to translate.</param>
    /// <returns>A QueryHelper instance.</returns>
    /// <exception cref="NotSupportedException">Thrown when the LINQ expression contains unsupported operations.</exception>
    /// <remarks>
    /// Translates LINQ expressions to DynamoDB expressions.
    /// </remarks>
    public static QueryHelper<T> FromLinqExpression(
        LinqExpression linqExpression)
    {
        DynamoExpression? dynamoWhereExpression = null;

        // Get the other expressions.
        var methodCallExpressions = new Stack<MethodCallExpression>();

        var currentExpression = linqExpression;
        while (currentExpression is MethodCallExpression mce)
        {
            // Should be a Queryable method.  We only support methods that extend IQueryable.
            if (mce.Method.DeclaringType != typeof(Queryable))
            {
                throw new NotSupportedException($"FromLinqExpression() does not support '{linqExpression}'.");
            }

            // If the expression is the Where method, convert to DynamoExpression and stop.
            if (mce.Method.Name == nameof(Queryable.Where))
            {
                // Parse the Where expression to extract the filter condition.
                var whereExpression = ParseWhereExpression(mce);
                // Convert the LINQ expression to a DynamoDB expression.
                dynamoWhereExpression = ExpressionConverter.Convert(whereExpression);
                break;
            }

            // For other methods (e.g., OrderBy, Skip, Take), create a constant value to call the method on.
            var constantValue = Enumerable.Empty<T>().AsQueryable();
            var constantExpression = LinqExpression.Constant(constantValue);

            // Create a method call expression that represents the LINQ method to be applied in-memory.
            var methodCallExpression = LinqExpression.Call(
                mce.Method,
                constantExpression,
                mce.Arguments[1]);

            methodCallExpressions.Push(methodCallExpression);

            // Move to the next expression in the chain.
            currentExpression = mce.Arguments[0];
        }

        return new QueryHelper<T>(
            dynamoWhereExpression,
            methodCallExpressions);
    }

    /// <summary>
    /// Converts the query helper's state to a JSON string for debugging.
    /// </summary>
    /// <returns>A JSON string.</returns>
    /// <remarks>
    /// Used for logging and debugging query translation.
    /// </remarks>
    public string ToJson()
    {
        // Convert the expression attribute values.
        var expressionAttributeValues = _dynamoWhereExpression?.ExpressionAttributeValues
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        // Convert the method call expressions.
        var methodCallExpressions = _methodCallExpressions
            .Select(mce => mce.ToString())
            .ToArray();

        var o = new
        {
            ExpressionStatement = _dynamoWhereExpression?.ExpressionStatement,
            ExpressionAttributeValues = expressionAttributeValues,
            MethodCallExpressions = _methodCallExpressions.Select(mce => mce.ToString()).ToArray()
        };

        return JsonSerializer.Serialize(o, _jsonSerializerOptions);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Extracts and processes the WHERE clause from a LINQ expression.
    /// </summary>
    /// <param name="methodCallExpression">The LINQ expression containing a WHERE clause.</param>
    /// <returns>The extracted WHERE predicate expression.</returns>
    /// <exception cref="NotSupportedException">Thrown when the expression is not a valid WHERE clause.</exception>
    /// <remarks>
    /// Handles simple and compound WHERE clauses.
    /// </remarks>
    private static LinqExpression ParseWhereExpression(
        MethodCallExpression methodCallExpression)
    {
        // Check if this is a Where method.
        if (methodCallExpression.Method.Name != nameof(Queryable.Where))
        {
            throw new NotSupportedException($"ParseWhereExpression() does not support '{methodCallExpression}'.");
        }

        (var source, var lambda) = ParseWhereMethod(methodCallExpression);

        // Check if the source of the Where method is another Queryable method.
        if (source is MethodCallExpression smce && smce.Method.DeclaringType == typeof(Queryable))
        {
            // Check if the source is another Where method.
            if (smce.Method.Name != nameof(Queryable.Where))
            {
                throw new NotSupportedException($"ParseWhereExpression() does not support '{methodCallExpression}'.");
            }

            // Parse the source Where method.
            var sourceWhere = ParseWhereExpression(smce);

            // If the source is a BinaryExpression, combine it with the current lambda using AndAlso.
            return sourceWhere switch
            {
                BinaryExpression sourceWhereBinaryExpression => LinqExpression.AndAlso(sourceWhereBinaryExpression, lambda.Body),
                LambdaExpression sourceWhereLambdaExpression => LinqExpression.AndAlso(sourceWhereLambdaExpression.Body, lambda.Body),

                _ => throw new NotSupportedException($"ParseWhereExpression() does not support '{methodCallExpression}'."),
            };
        }

        return lambda;
    }

    /// <summary>
    /// Extracts the source and predicate lambda from a WHERE method call expression.
    /// </summary>
    /// <param name="methodCallExpression">The WHERE method call expression.</param>
    /// <returns>A tuple containing the source expression and the lambda predicate expression.</returns>
    /// <exception cref="NotSupportedException">Thrown when the expression is not a valid WHERE method call.</exception>
    /// <remarks>
    /// Handles inline lambda expressions and pre-defined predicates.
    /// </remarks>
    private static (LinqExpression source, LambdaExpression lambda) ParseWhereMethod(
        MethodCallExpression methodCallExpression)
    {
        if (methodCallExpression.Arguments.Count != 2)
        {
            throw new NotSupportedException($"ParseWhereMethod() does not support '{methodCallExpression}'.");
        }

        var source = methodCallExpression.Arguments[0];

        // Case 1: Inline lambda
        //   var q = queryable.Where(r => r.Property == value)
        if (methodCallExpression.Arguments[1] is UnaryExpression ue &&
            ue.Operand is LambdaExpression ule)
        {
            return (source: source, lambda: ule);
        }

        // Case 2: Pre-defined predicate
        //   Expression<Func<T, bool>> predicate = r => r.Property == value;
        //   var q = queryable.Where(predicate);
        if (methodCallExpression.Arguments[1] is LambdaExpression dle)
        {
            return (source: source, lambda: dle);
        }

        throw new NotSupportedException($"ParseWhereMethod() does not support '{methodCallExpression}'.");
    }

    #endregion

    #region ExpressionConverter

    /// <summary>
    /// Converts LINQ expressions to DynamoDB expression format.
    /// </summary>
    private class ExpressionConverter
    {
        #region Private Fields

        /// <summary>
        /// Collection of attribute values used in the DynamoDB expression.
        /// </summary>
        private readonly Dictionary<string, DynamoDBEntry> _attributeValues = [];

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Converts a LINQ expression to a DynamoDB expression.
        /// </summary>
        /// <param name="linqExpression">The LINQ expression to convert.</param>
        /// <returns>A DynamoDB expression.</returns>
        /// <remarks>
        /// Creates a new ExpressionConverter instance and converts the LINQ expression.
        /// </remarks>
        public static DynamoExpression Convert(
            LinqExpression linqExpression)
        {
            var converter = new ExpressionConverter();

            return converter.ConvertExpression(linqExpression);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Add the given value to the <see cref="_attributeValues"/> dictionary and return its key.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The key of the given value.</returns>
        private string AddAttributeValue(
            object? value)
        {
            var attributeValueIndex = _attributeValues.Count + 1;
            var attributeValueKey = $":val{attributeValueIndex}";

            _attributeValues[attributeValueKey] = ToDynamoDBEntry(value);

            return attributeValueKey;
        }

        /// <summary>
        /// Process the given <see cref="LinqExpression"/> to build the DynamoDB expression statement.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression"/>.</param>
        /// <returns>The DynamoDB expression statement.</returns>
        /// <exception cref="NotSupportedException">Thrown when the given <see cref="LinqExpression"/> is not supported.</exception>
        private string BuildExpressionStatement(
            LinqExpression linqExpression)
        {
            return linqExpression switch
            {
                BinaryExpression binaryExpression => HandleBinaryExpression(binaryExpression),
                ConstantExpression constantExpression => HandleConstantExpression(constantExpression),
                LambdaExpression lambdaExpression => BuildExpressionStatement(lambdaExpression.Body),
                MemberExpression memberExpression => HandleMemberExpression(memberExpression),
                MethodCallExpression methodCallExpression => HandleMethodCallExpression(methodCallExpression),
                UnaryExpression unaryExpression => HandleUnaryExpression(unaryExpression),

                _ => throw new NotSupportedException($"BuildExpressionStatement() does not support '{linqExpression}'.")
            };
        }

        /// <summary>
        /// Performs the actual conversion of a LINQ expression to a DynamoDB expression.
        /// </summary>
        /// <param name="linqExpression">The LINQ expression to convert.</param>
        /// <returns>A DynamoDB expression.</returns>
        private DynamoExpression ConvertExpression(
            LinqExpression linqExpression)
        {
            var expressionStatement = BuildExpressionStatement(linqExpression);

            return new DynamoExpression()
            {
                ExpressionAttributeValues = _attributeValues,
                ExpressionStatement = expressionStatement
            };
        }

        /// <summary>
        /// Process the given <see cref="BinaryExpression"/>.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression"/>.</param>
        /// <returns>The DynamoDB expression statement.</returns>
        /// <exception cref="NotSupportedException">Thrown when the given <see cref="BinaryExpression"/> is not supported.</exception>
        private string HandleBinaryExpression(
            BinaryExpression binaryExpression)
        {
            // Handle logical AND and OR operators.
            if (binaryExpression.NodeType == ExpressionType.AndAlso || binaryExpression.NodeType == ExpressionType.OrElse)
            {
                var left = BuildExpressionStatement(binaryExpression.Left);
                var right = BuildExpressionStatement(binaryExpression.Right);
                var op = binaryExpression.NodeType == ExpressionType.AndAlso ? "AND" : "OR";

                return $"({left} {op} {right})";
            }

            if (binaryExpression.NodeType == ExpressionType.Equal) return HandleEqual(binaryExpression);
            if (binaryExpression.NodeType == ExpressionType.NotEqual) return HandleNotEqual(binaryExpression);

            // Get the property and value expressions.
            // Property be left oriented (property op value) or right oriented (value op property).
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            // Check if property was right oriented.
            var swapped = propertyExpression == binaryExpression.Right;

            return binaryExpression.NodeType switch
            {
                ExpressionType.GreaterThan => HandleComparison(propertyExpression, valueExpression, swapped ? "<" : ">"),
                ExpressionType.GreaterThanOrEqual => HandleComparison(propertyExpression, valueExpression, swapped ? "<=" : ">="),
                ExpressionType.LessThan => HandleComparison(propertyExpression, valueExpression, swapped ? ">" : "<"),
                ExpressionType.LessThanOrEqual => HandleComparison(propertyExpression, valueExpression, swapped ? ">=" : "<="),

                _ => throw new NotSupportedException($"HandleBinaryExpression() does not support '{binaryExpression}'.")
            };
        }

        /// <summary>
        /// Process the given <see cref="BinaryExpression"/> for its comparison.
        /// </summary>
        /// <param name="propertyExpression">The property expression.</param>
        /// <param name="valueExpression">The value expression.</param>
        /// <param name="op">The operator of the comparison.</param>
        /// <returns>The DynamoDB expression statement.</returns>
        private string HandleComparison(
            LinqExpression propertyExpression,
            LinqExpression valueExpression,
            string op)
        {
            var propertyName = GetPropertyName(propertyExpression);
            var valueKey = BuildExpressionStatement(valueExpression);

            return $"{propertyName} {op} {valueKey}";
        }

        /// <summary>
        /// Process the given <see cref="ConstantExpression"/>.
        /// </summary>
        /// <param name="constantExpression">The <see cref="ConstantExpression"/>.</param>
        /// <returns>The key of the given value.</returns>
        private string HandleConstantExpression(
            ConstantExpression constantExpression)
        {
            return AddAttributeValue(constantExpression.Value);
        }

        /// <summary>
        /// Process the given <see cref="BinaryExpression"/> for its equal comparison.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression"/>.</param>
        /// <returns>The DynamoDB expression statement.</returns>
        private string HandleEqual(
            BinaryExpression binaryExpression)
        {
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            // Handle null value.  In DynamoDB, we check for null by checking if the attribute exists.
            if (IsNullValue(valueExpression))
            {
                var propertyName = GetPropertyName(propertyExpression);

                return $"attribute_not_exists({propertyName})";
            }

            return HandleComparison(propertyExpression, valueExpression, "=");
        }

        /// <summary>
        /// Process the given <see cref="MemberExpression"/>.
        /// </summary>
        /// <param name="memberExpression">The <see cref="MemberExpression"/>.</param>
        /// <returns>The key of the given value.</returns>
        private string HandleMemberExpression(
            MemberExpression memberExpression)
        {
            // If the member expression is a parameter expression, return the member name.
            if (memberExpression.Expression is ParameterExpression)
            {
                return memberExpression.Member.Name;
            }

            // Invoke the lambda expression to get the value.
            var value = Invoke(memberExpression);

            return AddAttributeValue(value);
        }

        /// <summary>
        /// Process the given <see cref="MethodCallExpression"/>.
        /// </summary>
        /// <param name="methodCallExpression">The <see cref="MethodCallExpression"/>.</param>
        /// <returns>The key of the given value.</returns>
        private string HandleMethodCallExpression(
            MethodCallExpression methodCallExpression)
        {
            // Handle method calls on Enumerable (e.g., Contains).
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "Contains":
                        ValidateMethodCallArguments(methodCallExpression, 2);
                        var collectionExpression = methodCallExpression.Arguments[0];

                        // Ensure that the collection expression is a member expression and that it represents a property.
                        if (collectionExpression is not MemberExpression me || IsPropertyExpression(collectionExpression) is false)
                        {
                            throw new ArgumentException($"HandleMethodCallExpression() does not support '{methodCallExpression}'.");
                        }

                        var propertyName = GetPropertyName(me);
                        var containsValue = Invoke(methodCallExpression.Arguments[1]);
                        var containsKey = AddAttributeValue(containsValue);

                        return $"contains({propertyName}, {containsKey})";
                }
            }
            // If the method call is on a member expression and the member expression is a property expression.
            else if (methodCallExpression.Object is MemberExpression me && IsPropertyExpression(me))
            {
                var propertyName = me.Member.Name;

                switch (methodCallExpression.Method.Name)
                {
                    case "Contains":
                        ValidateMethodCallArguments(methodCallExpression, 1);
                        var containsValue = Invoke(methodCallExpression.Arguments[0]);
                        var containsKey = AddAttributeValue(containsValue);

                        return $"contains({propertyName}, {containsKey})";

                    case "StartsWith":
                        ValidateMethodCallArguments(methodCallExpression, 1);
                        var startsWithValue = Invoke(methodCallExpression.Arguments[0]);
                        var startsWithKey = AddAttributeValue(startsWithValue);

                        return $"begins_with({propertyName}, {startsWithKey})";
                }
            }

            try
            {
                // Attempt to invoke the method and add the result as an attribute value.
                var methodValue = Invoke(methodCallExpression);

                return AddAttributeValue(methodValue);
            }
            catch
            {
                // If the method call is not supported, throw an exception.
                throw new NotSupportedException($"HandleMethodCallExpression() does not support '{methodCallExpression}'.");
            }
        }

        /// <summary>
        /// Process the given <see cref="BinaryExpression"/> for its not equal comparison.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression"/>.</param>
        /// <returns>The DynamoDB expression statement.</returns>
        private string HandleNotEqual(
            BinaryExpression binaryExpression)
        {
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            // Handle null value.  In DynamoDB, we check for not null by checking if the attribute exists.
            if (IsNullValue(valueExpression))
            {
                var propertyName = GetPropertyName(propertyExpression);

                return $"attribute_exists({propertyName})";
            }

            return HandleComparison(propertyExpression, valueExpression, "<>");
        }

        /// <summary>
        /// Process the given <see cref="UnaryExpression"/>.
        /// </summary>
        /// <param name="unaryExpression">The <see cref="UnaryExpression.</param>
        /// <returns>The key of the given value.</returns>
        /// <exception cref="NotSupportedException">Thrown when the given <see cref="UnaryExpression"/> is not supported.</exception>
        private string HandleUnaryExpression(
            UnaryExpression unaryExpression)
        {
            if (unaryExpression.NodeType == ExpressionType.Convert)
            {
                // Get the constant value after conversion.
                var lambda = LinqExpression.Lambda(unaryExpression).Compile();
                var value = lambda.DynamicInvoke();

                return AddAttributeValue(value);
            }

            throw new NotSupportedException($"HandleUnaryExpression() does not support '{unaryExpression}'.");
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Process the <see cref="BinaryExpression"/> for the property and value.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression.</param>
        /// <returns>The property and value.</returns>
        /// <exception cref="NotSupportedException"></exception>
        private static (LinqExpression propertyExpression, LinqExpression valueExpression) GetPropertyAndValue(
            BinaryExpression binaryExpression)
        {
            // Check if left or right is the property.
            if (IsPropertyExpression(binaryExpression.Left))
            {
                return (binaryExpression.Left, binaryExpression.Right);
            }

            if (IsPropertyExpression(binaryExpression.Right))
            {
                return (binaryExpression.Right, binaryExpression.Left);
            }

            // Neither left nor right is the property, evalute the expression.
            if (binaryExpression.Left is MemberExpression)
            {
                return (binaryExpression.Left, binaryExpression.Right);
            }

            if (binaryExpression.Right is MemberExpression)
            {
                return (binaryExpression.Right, binaryExpression.Left);
            }

            throw new NotSupportedException($"GetPropertyAndValue() does not support '{binaryExpression}'.");
        }

        /// <summary>
        /// Get the property name from the given <see cref="LinqExpression"/>.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression.</param>
        /// <returns>The property name.</returns>
        /// <exception cref="NotSupportedException">thrown when the given <see cref="LinqExpression"/> is not supported.</exception>
        private static string GetPropertyName(
            LinqExpression linqExpression)
        {
            if (linqExpression is not MemberExpression me)
            {
                throw new NotSupportedException($"GetPropertyName() does not support '{linqExpression}'.");
            }

            if (me.Member is not PropertyInfo pi)
            {
                throw new NotSupportedException($"GetPropertyName() does not support '{linqExpression}'.");
            }

            // Check for the JsonPropertyNameAttribute.
            var jsonPropertyNameAttribute = pi.GetCustomAttribute<JsonPropertyNameAttribute>();

            return jsonPropertyNameAttribute?.Name ?? me.Member.Name;
        }

        /// <summary>
        /// Invole the given <see cref="LinqExpression"/> and return its result.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression.</param>
        /// <returns>The result.</returns>
        private static object? Invoke(
            LinqExpression linqExpression)
        {
            return LinqExpression.Lambda(linqExpression).Compile().DynamicInvoke();
        }

        /// <summary>
        /// Determines whether the given <see cref="LinqExpression"/> is a null value.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression"/>.</param>
        /// <returns><see langword="true"/> if the given <see cref="LinqExpression"/> is a null value; otherwise, <see langword="false"/>.</returns>
        private static bool IsNullValue(
            LinqExpression linqExpression)
        {
            return linqExpression is ConstantExpression ce && ce.Value == null;
        }

        /// <summary>
        /// Determines whether the given <see cref="LinqExpression"/> is a field or property on the named parameter.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression"/>.</param>
        /// <returns><see langword="true"/> if the given <see cref="LinqExpression"/> is a field or property on the named parameter; otherwise, <see langword="false"/>.</returns>
        private static bool IsPropertyExpression(
            LinqExpression linqExpression)
        {
            return linqExpression is MemberExpression { Expression: ParameterExpression };
        }

        /// <summary>
        /// Convert the given value to a <see cref="DynamoDBEntry"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="DynamoDBEntry"/> representation.</returns>
        private static DynamoDBEntry ToDynamoDBEntry(
            object? value)
        {
            if (value is null) return new DynamoDBNull();

            return DynamoDBEntryConversion.V2.ConvertToEntry(value.GetType(), value);
        }

        /// <summary>
        /// Validate the given <see cref="MethodCallExpression"/> for the expected argument count.
        /// </summary>
        /// <param name="methodCallExpression">The <see cref="MethodCallExpression"/>.</param>
        /// <param name="expectedArgumentCount">The expected argument count.</param>
        /// <exception cref="NotSupportedException">thrown when the given <see cref="MethodCallExpression"/> does not have the expected argument count.</exception>
        private static void ValidateMethodCallArguments(
            MethodCallExpression methodCallExpression,
            int expectedArgumentCount)
        {
            if (methodCallExpression.Arguments.Count != expectedArgumentCount)
            {
                throw new NotSupportedException($"ValidateMethodCallArguments() does not support '{methodCallExpression}'.");
            }
        }

        #endregion
    }

    #endregion
}
