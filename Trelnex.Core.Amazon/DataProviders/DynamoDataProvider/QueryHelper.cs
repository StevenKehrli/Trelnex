using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using DynamoExpression = Amazon.DynamoDBv2.DocumentModel.Expression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Translates LINQ expressions to DynamoDB query expressions and applies remaining filters in-memory.
/// </summary>
/// <typeparam name="T">The type of items being queried.</typeparam>
public class QueryHelper<T>
{
    #region Private Fields

    // Translated DynamoDB WHERE expression for server-side filtering
    private readonly DynamoExpression? _dynamoWhereExpression;

    // Stack of LINQ operations to be applied in-memory after DynamoDB query
    private readonly Stack<MethodCallExpression> _methodCallExpressions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new QueryHelper with translated expressions.
    /// </summary>
    /// <param name="dynamoWhereExpression">Translated DynamoDB WHERE expression.</param>
    /// <param name="methodCallExpressions">Stack of LINQ operations for in-memory processing.</param>
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
    /// Gets the DynamoDB WHERE expression for server-side filtering.
    /// </summary>
    public DynamoExpression? DynamoWhereExpression => _dynamoWhereExpression;

    #endregion

    #region Public Methods

    /// <summary>
    /// Applies remaining LINQ operations to the collection in-memory.
    /// </summary>
    /// <param name="source">Collection of items retrieved from DynamoDB.</param>
    /// <returns>Filtered and transformed collection after applying LINQ operations.</returns>
    /// <exception cref="NotSupportedException">Thrown when a LINQ operation cannot be processed.</exception>
    public IEnumerable<T> Filter(
        IEnumerable<T> source)
    {
        var result = source;

        // Apply each LINQ operation from the stack
        while (_methodCallExpressions.Count > 0)
        {
            var queryable = result!.AsQueryable();

            // Get next LINQ operation to apply
            var methodCallExpression = _methodCallExpressions.Pop();

            // Extract the parameter for the LINQ operation
            object? parameter;

            var argument = methodCallExpression.Arguments[1];

            // Handle different argument types for LINQ methods
            if (argument is ConstantExpression ce)
            {
                // Direct constant value
                parameter = ce.Value;
            }
            else if (argument is UnaryExpression ue && ue.Operand is LambdaExpression ule)
            {
                // Lambda wrapped in unary expression
                parameter = LinqExpression.Lambda(ule.Body, ule.Parameters);
            }
            else if (argument is LambdaExpression le)
            {
                // Direct lambda expression
                parameter = le;
            }
            else
            {
                throw new NotSupportedException($"Filter() does not support '{methodCallExpression}'.");
            }

            // Apply the LINQ operation to the current result set
            result = methodCallExpression.Method.Invoke(null, [ queryable, parameter ]) as IEnumerable<T>;
        }

        return result!;
    }

    /// <summary>
    /// Creates a QueryHelper by parsing and translating a LINQ expression.
    /// </summary>
    /// <param name="linqExpression">LINQ expression to translate.</param>
    /// <returns>QueryHelper with translated DynamoDB expression and remaining operations.</returns>
    /// <exception cref="NotSupportedException">Thrown when the LINQ expression contains unsupported operations.</exception>
    public static QueryHelper<T> FromLinqExpression(
        LinqExpression linqExpression)
    {
        DynamoExpression? dynamoWhereExpression = null;

        // Stack to hold operations that can't be translated to DynamoDB
        var methodCallExpressions = new Stack<MethodCallExpression>();

        var currentExpression = linqExpression;
        while (currentExpression is MethodCallExpression mce)
        {
            // Only support Queryable extension methods
            if (mce.Method.DeclaringType != typeof(Queryable))
            {
                throw new NotSupportedException($"FromLinqExpression() does not support '{linqExpression}'.");
            }

            // Translate WHERE clause to DynamoDB expression
            if (mce.Method.Name == nameof(Queryable.Where))
            {
                var whereExpression = ParseWhereExpression(mce);
                dynamoWhereExpression = ExpressionConverter.Convert(whereExpression);
                break;
            }

            // Queue other operations for in-memory processing
            var constantValue = Enumerable.Empty<T>().AsQueryable();
            var constantExpression = LinqExpression.Constant(constantValue);

            var methodCallExpression = LinqExpression.Call(
                mce.Method,
                constantExpression,
                mce.Arguments[1]);

            methodCallExpressions.Push(methodCallExpression);

            // Continue parsing the expression chain
            currentExpression = mce.Arguments[0];
        }

        return new QueryHelper<T>(
            dynamoWhereExpression,
            methodCallExpressions);
    }

    /// <summary>
    /// Converts the QueryHelper state to JSON for debugging purposes.
    /// </summary>
    /// <param name="jsonSerializerOptions">JSON serialization options.</param>
    /// <returns>JSON representation of the query state.</returns>
    public string ToJson(
        JsonSerializerOptions jsonSerializerOptions)
    {
        // Convert DynamoDB expression attribute values to strings
        var expressionAttributeValues = _dynamoWhereExpression?.ExpressionAttributeValues
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        // Convert method call expressions to strings
        var methodCallExpressions = _methodCallExpressions
            .Select(mce => mce.ToString())
            .ToArray();

        var o = new
        {
            ExpressionStatement = _dynamoWhereExpression?.ExpressionStatement,
            ExpressionAttributeValues = expressionAttributeValues,
            MethodCallExpressions = _methodCallExpressions.Select(mce => mce.ToString()).ToArray()
        };

        return JsonSerializer.Serialize(o, jsonSerializerOptions);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Extracts and combines WHERE clause predicates from LINQ expressions.
    /// </summary>
    /// <param name="methodCallExpression">LINQ expression containing WHERE clause.</param>
    /// <returns>Combined predicate expression for all WHERE clauses.</returns>
    /// <exception cref="NotSupportedException">Thrown when the expression is not a valid WHERE clause.</exception>
    private static LinqExpression ParseWhereExpression(
        MethodCallExpression methodCallExpression)
    {
        // Verify this is a WHERE method call
        if (methodCallExpression.Method.Name != nameof(Queryable.Where))
        {
            throw new NotSupportedException($"ParseWhereExpression() does not support '{methodCallExpression}'.");
        }

        (var source, var lambda) = ParseWhereMethod(methodCallExpression);

        // Handle chained WHERE clauses
        if (source is MethodCallExpression smce && smce.Method.DeclaringType == typeof(Queryable))
        {
            // Only support chained WHERE clauses
            if (smce.Method.Name != nameof(Queryable.Where))
            {
                throw new NotSupportedException($"ParseWhereExpression() does not support '{methodCallExpression}'.");
            }

            // Recursively parse the source WHERE clause
            var sourceWhere = ParseWhereExpression(smce);

            // Combine predicates using AND logic
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
    /// Extracts the source expression and predicate lambda from a WHERE method call.
    /// </summary>
    /// <param name="methodCallExpression">WHERE method call expression.</param>
    /// <returns>Tuple containing source expression and predicate lambda.</returns>
    /// <exception cref="NotSupportedException">Thrown when the expression format is not supported.</exception>
    private static (LinqExpression source, LambdaExpression lambda) ParseWhereMethod(
        MethodCallExpression methodCallExpression)
    {
        if (methodCallExpression.Arguments.Count != 2)
        {
            throw new NotSupportedException($"ParseWhereMethod() does not support '{methodCallExpression}'.");
        }

        var source = methodCallExpression.Arguments[0];

        // Handle inline lambda expression
        if (methodCallExpression.Arguments[1] is UnaryExpression ue &&
            ue.Operand is LambdaExpression ule)
        {
            return (source: source, lambda: ule);
        }

        // Handle pre-defined predicate variable
        if (methodCallExpression.Arguments[1] is LambdaExpression dle)
        {
            return (source: source, lambda: dle);
        }

        throw new NotSupportedException($"ParseWhereMethod() does not support '{methodCallExpression}'.");
    }

    #endregion

    #region ExpressionConverter

    /// <summary>
    /// Converts LINQ expressions to DynamoDB expression format with attribute value mapping.
    /// </summary>
    private class ExpressionConverter
    {
        #region Private Fields

        // Dictionary storing attribute values referenced in the DynamoDB expression
        private readonly Dictionary<string, DynamoDBEntry> _attributeValues = [];

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Converts a LINQ expression to DynamoDB expression format.
        /// </summary>
        /// <param name="linqExpression">LINQ expression to convert.</param>
        /// <returns>DynamoDB expression with statement and attribute values.</returns>
        public static DynamoExpression Convert(
            LinqExpression linqExpression)
        {
            var converter = new ExpressionConverter();

            return converter.ConvertExpression(linqExpression);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a value to the attribute values dictionary and returns its generated key.
        /// </summary>
        /// <param name="value">Value to add to the dictionary.</param>
        /// <returns>Generated key for the value in the format ":val{index}".</returns>
        private string AddAttributeValue(
            object? value)
        {
            var attributeValueIndex = _attributeValues.Count + 1;
            var attributeValueKey = $":val{attributeValueIndex}";

            _attributeValues[attributeValueKey] = ToDynamoDBEntry(value);

            return attributeValueKey;
        }

        /// <summary>
        /// Recursively builds DynamoDB expression statement from LINQ expression tree.
        /// </summary>
        /// <param name="linqExpression">LINQ expression to process.</param>
        /// <returns>DynamoDB expression statement string.</returns>
        /// <exception cref="NotSupportedException">Thrown when expression type is not supported.</exception>
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
        /// Performs the conversion from LINQ expression to DynamoDB expression.
        /// </summary>
        /// <param name="linqExpression">LINQ expression to convert.</param>
        /// <returns>Complete DynamoDB expression with statement and attribute values.</returns>
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
        /// Handles binary expressions like comparisons and logical operators.
        /// </summary>
        /// <param name="binaryExpression">Binary expression to process.</param>
        /// <returns>DynamoDB expression statement for the binary operation.</returns>
        /// <exception cref="NotSupportedException">Thrown when binary operation is not supported.</exception>
        private string HandleBinaryExpression(
            BinaryExpression binaryExpression)
        {
            // Handle logical AND/OR operators
            if (binaryExpression.NodeType == ExpressionType.AndAlso || binaryExpression.NodeType == ExpressionType.OrElse)
            {
                var left = BuildExpressionStatement(binaryExpression.Left);
                var right = BuildExpressionStatement(binaryExpression.Right);
                var op = binaryExpression.NodeType == ExpressionType.AndAlso ? "AND" : "OR";

                return $"({left} {op} {right})";
            }

            if (binaryExpression.NodeType == ExpressionType.Equal) return HandleEqual(binaryExpression);
            if (binaryExpression.NodeType == ExpressionType.NotEqual) return HandleNotEqual(binaryExpression);

            // Extract property and value from comparison expression
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            // Check if operands were swapped (value op property instead of property op value)
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
        /// Builds DynamoDB comparison expression for property and value.
        /// </summary>
        /// <param name="propertyExpression">Expression representing the property.</param>
        /// <param name="valueExpression">Expression representing the value.</param>
        /// <param name="op">Comparison operator string.</param>
        /// <returns>DynamoDB comparison expression statement.</returns>
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
        /// Handles constant expressions by adding them to attribute values.
        /// </summary>
        /// <param name="constantExpression">Constant expression to process.</param>
        /// <returns>Attribute value key for the constant.</returns>
        private string HandleConstantExpression(
            ConstantExpression constantExpression)
        {
            return AddAttributeValue(constantExpression.Value);
        }

        /// <summary>
        /// Handles equality comparisons with special null handling.
        /// </summary>
        /// <param name="binaryExpression">Binary expression representing equality.</param>
        /// <returns>DynamoDB expression statement for equality check.</returns>
        private string HandleEqual(
            BinaryExpression binaryExpression)
        {
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            // Use attribute_not_exists for null equality checks
            if (IsNullValue(valueExpression))
            {
                var propertyName = GetPropertyName(propertyExpression);

                return $"attribute_not_exists({propertyName})";
            }

            return HandleComparison(propertyExpression, valueExpression, "=");
        }

        /// <summary>
        /// Handles member expressions representing properties or calculated values.
        /// </summary>
        /// <param name="memberExpression">Member expression to process.</param>
        /// <returns>Property name or attribute value key for calculated value.</returns>
        private string HandleMemberExpression(
            MemberExpression memberExpression)
        {
            // Return property name for parameter expressions
            if (memberExpression.Expression is ParameterExpression)
            {
                return memberExpression.Member.Name;
            }

            // Evaluate and add calculated values
            var value = Invoke(memberExpression);

            return AddAttributeValue(value);
        }

        /// <summary>
        /// Handles method call expressions like Contains, StartsWith, etc.
        /// </summary>
        /// <param name="methodCallExpression">Method call expression to process.</param>
        /// <returns>DynamoDB expression statement or attribute value key.</returns>
        private string HandleMethodCallExpression(
            MethodCallExpression methodCallExpression)
        {
            // Handle Enumerable methods like Contains
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "Contains":
                        ValidateMethodCallArguments(methodCallExpression, 2);
                        var collectionExpression = methodCallExpression.Arguments[0];

                        // Ensure collection is a property expression
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
            // Handle string methods on properties
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
                // Try to evaluate method call and add result as attribute value
                var methodValue = Invoke(methodCallExpression);

                return AddAttributeValue(methodValue);
            }
            catch
            {
                throw new NotSupportedException($"HandleMethodCallExpression() does not support '{methodCallExpression}'.");
            }
        }

        /// <summary>
        /// Handles not-equal comparisons with special null handling.
        /// </summary>
        /// <param name="binaryExpression">Binary expression representing inequality.</param>
        /// <returns>DynamoDB expression statement for inequality check.</returns>
        private string HandleNotEqual(
            BinaryExpression binaryExpression)
        {
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            // Use attribute_exists for not-null checks
            if (IsNullValue(valueExpression))
            {
                var propertyName = GetPropertyName(propertyExpression);

                return $"attribute_exists({propertyName})";
            }

            return HandleComparison(propertyExpression, valueExpression, "<>");
        }

        /// <summary>
        /// Handles unary expressions like type conversions.
        /// </summary>
        /// <param name="unaryExpression">Unary expression to process.</param>
        /// <returns>Attribute value key for the converted value.</returns>
        /// <exception cref="NotSupportedException">Thrown when unary operation is not supported.</exception>
        private string HandleUnaryExpression(
            UnaryExpression unaryExpression)
        {
            if (unaryExpression.NodeType == ExpressionType.Convert)
            {
                // Evaluate conversion and add result as attribute value
                var lambda = LinqExpression.Lambda(unaryExpression).Compile();
                var value = lambda.DynamicInvoke();

                return AddAttributeValue(value);
            }

            throw new NotSupportedException($"HandleUnaryExpression() does not support '{unaryExpression}'.");
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Extracts property and value expressions from binary comparison.
        /// </summary>
        /// <param name="binaryExpression">Binary expression to analyze.</param>
        /// <returns>Tuple containing property expression and value expression.</returns>
        /// <exception cref="NotSupportedException">Thrown when property/value cannot be determined.</exception>
        private static (LinqExpression propertyExpression, LinqExpression valueExpression) GetPropertyAndValue(
            BinaryExpression binaryExpression)
        {
            // Check which side represents the property
            if (IsPropertyExpression(binaryExpression.Left))
            {
                return (binaryExpression.Left, binaryExpression.Right);
            }

            if (IsPropertyExpression(binaryExpression.Right))
            {
                return (binaryExpression.Right, binaryExpression.Left);
            }

            // Fallback to member expression detection
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
        /// Extracts property name from expression, using JSON attribute if available.
        /// </summary>
        /// <param name="linqExpression">Expression representing a property.</param>
        /// <returns>Property name or JSON property name from attribute.</returns>
        /// <exception cref="NotSupportedException">Thrown when expression doesn't represent a property.</exception>
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

            // Use JSON property name if specified, otherwise use property name
            var jsonPropertyNameAttribute = pi.GetCustomAttribute<JsonPropertyNameAttribute>();

            return jsonPropertyNameAttribute?.Name ?? me.Member.Name;
        }

        /// <summary>
        /// Evaluates an expression and returns its value.
        /// </summary>
        /// <param name="linqExpression">Expression to evaluate.</param>
        /// <returns>Evaluated result of the expression.</returns>
        private static object? Invoke(
            LinqExpression linqExpression)
        {
            return LinqExpression.Lambda(linqExpression).Compile().DynamicInvoke();
        }

        /// <summary>
        /// Determines if an expression represents a null constant value.
        /// </summary>
        /// <param name="linqExpression">Expression to check.</param>
        /// <returns>True if expression is a null constant.</returns>
        private static bool IsNullValue(
            LinqExpression linqExpression)
        {
            return linqExpression is ConstantExpression ce && ce.Value == null;
        }

        /// <summary>
        /// Determines if an expression represents a property access on a parameter.
        /// </summary>
        /// <param name="linqExpression">Expression to check.</param>
        /// <returns>True if expression represents a property access.</returns>
        private static bool IsPropertyExpression(
            LinqExpression linqExpression)
        {
            return linqExpression is MemberExpression { Expression: ParameterExpression };
        }

        /// <summary>
        /// Converts a .NET value to DynamoDB entry format.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <returns>DynamoDB entry representation of the value.</returns>
        private static DynamoDBEntry ToDynamoDBEntry(
            object? value)
        {
            if (value is null) return new DynamoDBNull();

            return DynamoDBEntryConversion.V2.ConvertToEntry(value.GetType(), value);
        }

        /// <summary>
        /// Validates that a method call has the expected number of arguments.
        /// </summary>
        /// <param name="methodCallExpression">Method call to validate.</param>
        /// <param name="expectedArgumentCount">Expected argument count.</param>
        /// <exception cref="NotSupportedException">Thrown when argument count doesn't match.</exception>
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
