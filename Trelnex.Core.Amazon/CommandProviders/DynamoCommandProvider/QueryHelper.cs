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

public class QueryHelper<T>
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private readonly DynamoExpression? _dynamoWhereExpression;
    private readonly Stack<MethodCallExpression> _methodCallExpressions;

    private QueryHelper(
        DynamoExpression? dynamoWhereExpression,
        Stack<MethodCallExpression> methodCallExpressions)
    {
        _dynamoWhereExpression = dynamoWhereExpression;
        _methodCallExpressions = methodCallExpressions;
    }

    public DynamoExpression? DynamoWhereExpression => _dynamoWhereExpression;

    /// <summary>
    /// Parse the <see cref="LinqExpression"/> into its Dynamo Where expression and other LINQ expressions.
    /// </summary>
    /// <param name="linqExpression">The <see cref="LinqExpression"/> to parse.</param>
    /// <returns>The <see cref="QueryHelper"/> with the Dynamo Where expression and other LINQ expressions.</returns>
    /// <exception cref="NotSupportedException">thrown when the given <see cref="LinqExpression"/> is not supported.</exception>
    public static QueryHelper<T> FromLinqExpression(
        LinqExpression linqExpression)
    {
        DynamoExpression? dynamoWhereExpression = null;

        // get the other expressions
        var methodCallExpressions = new Stack<MethodCallExpression>();

        var currentExpression = linqExpression;
        while (currentExpression is MethodCallExpression mce)
        {
            // should be a Queryable method
            if (mce.Method.DeclaringType != typeof(Queryable))
            {
                throw new NotSupportedException($"FromLinqExpression() does not support '{linqExpression}'.");
            }

            // if the expression is the Where method, convert to DynamoExpression and stop
            if (mce.Method.Name == nameof(Queryable.Where))
            {
                var whereExpression = ParseWhereExpression(mce);
                dynamoWhereExpression = ExpressionConverter.Convert(whereExpression);
                break;
            }

            var constantValue = Enumerable.Empty<T>().AsQueryable();
            var constantExpression = LinqExpression.Constant(constantValue);

            var methodCallExpression = LinqExpression.Call(
                mce.Method,
                constantExpression,
                mce.Arguments[1]);

            methodCallExpressions.Push(methodCallExpression);

            currentExpression = mce.Arguments[0];
        }

        return new QueryHelper<T>(
            dynamoWhereExpression,
            methodCallExpressions);
    }

    /// <summary>
    /// Filter the given source with the parsed <see cref="LinqExpression"/>.
    /// </summary>
    /// <param name="source">The source to filter with the parsed <see cref="LinqExpression"/>.</param>
    /// <returns>The filtered source with the parsed <see cref="LinqExpression"/>.</returns>
    /// <exception cref="NotSupportedException">thrown when the given <see cref="LinqExpression"/> is not supported.</exception>
    public IEnumerable<T> Filter(
        IEnumerable<T> source)
    {
        var result = source;

        while (_methodCallExpressions.Count > 0)
        {
            var queryable = result!.AsQueryable();

            // get the next method call expression
            var methodCallExpression = _methodCallExpressions.Pop();

            // get the method call parameter
            object? parameter;

            var argument = methodCallExpression.Arguments[1];

            if (argument is ConstantExpression ce)
            {
                parameter = ce.Value;
            }
            else if (argument is UnaryExpression ue && ue.Operand is LambdaExpression ule)
            {
                parameter = LinqExpression.Lambda(ule.Body, ule.Parameters);
            }
            else if (argument is LambdaExpression dle)
            {
                parameter = dle;
            }
            else
            {
                throw new NotSupportedException($"Filter() does not support '{methodCallExpression}'.");
            }

            // invoke the method call expression
            result = methodCallExpression.Method.Invoke(null, [ queryable, parameter ]) as IEnumerable<T>;
        }

        return result!;
    }

    public string ToJson()
    {
        // convert the expression attribute values
        var expressionAttributeValues = _dynamoWhereExpression?.ExpressionAttributeValues
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        // convert the method call expressions
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

    /// <summary>
    /// Parse the Where expression from the given <see cref="LinqExpression"/>.
    /// </summary>
    /// <param name="linqExpression">The <see cref="LinqExpression"/> to parse its Where expression.</param>
    /// <returns>The Where expression from the given <see cref="LinqExpression"/>.</returns>
    /// <exception cref="NotSupportedException">thrown when the given <see cref="LinqExpression"/> is not a valid Where expression.</exception>
    private static LinqExpression ParseWhereExpression(
        MethodCallExpression methodCallExpression)
    {
        // check if this is a Where method
        if (methodCallExpression.Method.Name != nameof(Queryable.Where))
        {
            throw new NotSupportedException($"ParseWhereExpression() does not support '{methodCallExpression}'.");
        }

        (var source, var lambda) = ParseWhereMethod(methodCallExpression);

        // check if the source of the Where method is another Queryable method
        if (source is MethodCallExpression smce && smce.Method.DeclaringType == typeof(Queryable))
        {
            // check if the source is another Where method
            if (smce.Method.Name != nameof(Queryable.Where))
            {
                throw new NotSupportedException($"ParseWhereExpression() does not support '{methodCallExpression}'.");
            }

            // parse the source Where method
            var sourceWhere = ParseWhereExpression(smce);

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
    /// Parse the Where method from the given <see cref="MethodCallExpression"/> into its source and lambda expressions.
    /// </summary>
    /// <param name="methodCallExpression">The <see cref="MethodCallExpression"/> to parse its Where method into its source and lambda expressions.</param>
    /// <returns>The source and lambda expressions from the given <see cref="MethodCallExpression"/> Where method.</returns>
    /// <exception cref="NotSupportedException">thrown when the given <see cref="MethodCallExpression"/> is not a valid Where method.</exception>
    private static (LinqExpression source, LambdaExpression lambda) ParseWhereMethod(
        MethodCallExpression methodCallExpression)
    {
        if (methodCallExpression.Arguments.Count != 2)
        {
            throw new NotSupportedException($"ParseWhereMethod() does not support '{methodCallExpression}'.");
        }

        var source = methodCallExpression.Arguments[0];

        // case 1:
        //   var q = queryable.Where(r => r.Property == value)
        if (methodCallExpression.Arguments[1] is UnaryExpression ue &&
            ue.Operand is LambdaExpression ule)
        {
            return (source: source, lambda: ule);
        }

        // case 2:
        //   Expression<Func<T, bool>> predicate = r => r.Property == value;
        //   var q = queryable.Where(predicate);
        if (methodCallExpression.Arguments[1] is LambdaExpression dle)
        {
            return (source: source, lambda: dle);
        }

        throw new NotSupportedException($"ParseWhereMethod() does not support '{methodCallExpression}'.");
    }

    private class ExpressionConverter
    {
        private readonly Dictionary<string, DynamoDBEntry> _attributeValues = [];

        /// <summary>
        /// Convert the given <see cref="LinqExpression"/> to a <see cref="DynamoExpression"/>.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression"/> to convert to a <see cref="DynamoExpression"/>.</param>
        /// <returns>The <see cref="DynamoExpression"/> representation of the given <see cref="LinqExpression"/>.</returns>
        public static DynamoExpression Convert(
            LinqExpression linqExpression)
        {
            var converter = new ExpressionConverter();

            return converter.ConvertExpression(linqExpression);
        }

        /// <summary>
        /// Convert the given <see cref="LinqExpression"/> to a <see cref="DynamoExpression"/>.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression"/> to convert to a <see cref="DynamoExpression"/>.</param>
        /// <returns>The <see cref="DynamoExpression"/> representation of the given <see cref="LinqExpression"/>.</returns>
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
        /// Add the given value to the <see cref="_attributeValues"/> dictionary and return its key.
        /// </summary>
        /// <param name="value">The value to add to the <see cref="_attributeValues"/> dictionary.</param>
        /// <returns>The key of the given value in the <see cref="_attributeValues"/> dictionary.</returns>
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
        /// <param name="linqExpression">The <see cref="LinqExpression"/> to process to build the DynamoDB expression statement.</param>
        /// <returns>The DynamoDB expression statement from the given <see cref="LinqExpression"/>.</returns>
        /// <exception cref="NotSupportedException">thrown when the given <see cref="LinqExpression"/> is not supported.</exception>
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
        /// Process the given <see cref="BinaryExpression"/> for its operation on the property and value.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression"/> to process for its operation on the property and value.</param>
        /// <returns>The DynamoDB expression statement for the operation on the property and value.</returns>
        /// <exception cref="NotSupportedException">thrown when the given <see cref="BinaryExpression"/> is not supported.</exception>
        private string HandleBinaryExpression(
            BinaryExpression binaryExpression)
        {
            if (binaryExpression.NodeType == ExpressionType.AndAlso || binaryExpression.NodeType == ExpressionType.OrElse)
            {
                var left = BuildExpressionStatement(binaryExpression.Left);
                var right = BuildExpressionStatement(binaryExpression.Right);
                var op = binaryExpression.NodeType == ExpressionType.AndAlso ? "AND" : "OR";

                return $"({left} {op} {right})";
            }

            if (binaryExpression.NodeType == ExpressionType.Equal) return HandleEqual(binaryExpression);
            if (binaryExpression.NodeType == ExpressionType.NotEqual) return HandleNotEqual(binaryExpression);

            // get the property and value expressions
            // property be left oriented (property op value) or right oriented (value op property)
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            // check if property was right oriented
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
        /// Process the given <see cref="BinaryExpression"/> for its comparison of the property and value.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression"/> to process for its comparison of the property and value.</param>
        /// <param name="op">The operator of the comparison.</param>
        /// <returns>The DynamoDB expression statement for the equal comparison of the property and value.</returns>
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
        /// Process the given <see cref="ConstantExpression"/> for its value.
        /// </summary>
        /// <param name="constantExpression">The <see cref="ConstantExpression"/> to process for its value.</param>
        /// <returns>The key of the given value in the <see cref="_attributeValues"/> dictionary.</returns>
        private string HandleConstantExpression(
            ConstantExpression constantExpression)
        {
            return AddAttributeValue(constantExpression.Value);
        }

        /// <summary>
        /// Process the given <see cref="BinaryExpression"/> for its equal comparison of the property and value.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression"/> to process for its equal comparison of the property and value.</param>
        /// <returns>The DynamoDB expression statement for the equal comparison of the property and value.</returns>
        private string HandleEqual(
            BinaryExpression binaryExpression)
        {
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            if (IsNullValue(valueExpression))
            {
                var propertyName = GetPropertyName(propertyExpression);

                return $"attribute_not_exists({propertyName})";
            }

            return HandleComparison(propertyExpression, valueExpression, "=");
        }

        /// <summary>
        /// Process the given <see cref="MemberExpression"/> to get the value of the member.
        /// </summary>
        /// <param name="memberExpression">The <see cref="MemberExpression"/> to process to get the value of the member.</param>
        /// <returns>The key of the given value in the <see cref="_attributeValues"/> dictionary.</returns>
        private string HandleMemberExpression(
            MemberExpression memberExpression)
        {
            if (memberExpression.Expression is ParameterExpression)
            {
                return memberExpression.Member.Name;
            }

            // invoke the lambda expression to get the value
            var value = Invoke(memberExpression);

            return AddAttributeValue(value);
        }

        /// <summary>
        /// Process the given <see cref="MethodCallExpression"/> to get the value of the method.
        /// </summary>
        /// <param name="methodCallExpression">The <see cref="MethodCallExpression"/> to process to get the value of the method.</param>
        /// <returns>The key of the given value in the <see cref="_attributeValues"/> dictionary.</returns>
        private string HandleMethodCallExpression(
            MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "Contains":
                        ValidateMethodCallArguments(methodCallExpression, 2);
                        var collectionExpression = methodCallExpression.Arguments[0];

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
                var methodValue = Invoke(methodCallExpression);

                return AddAttributeValue(methodValue);
            }
            catch
            {
                throw new NotSupportedException($"HandleMethodCallExpression() does not support '{methodCallExpression}'.");
            }
        }

        /// <summary>
        /// Process the given <see cref="BinaryExpression"/> for its not equal comparison of the property and value.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression"/> to process for its not equal comparison of the property and value.</param>
        /// <returns>The DynamoDB expression statement for the not equal comparison of the property and value.</returns>
        private string HandleNotEqual(
            BinaryExpression binaryExpression)
        {
            var (propertyExpression, valueExpression) = GetPropertyAndValue(binaryExpression);

            if (IsNullValue(valueExpression))
            {
                var propertyName = GetPropertyName(propertyExpression);

                return $"attribute_exists({propertyName})";
            }

            return HandleComparison(propertyExpression, valueExpression, "<>");
        }

        private string HandleUnaryExpression(
            UnaryExpression unaryExpression)
        {
            if (unaryExpression.NodeType == ExpressionType.Convert)
            {
                // get the constant value after conversion
                var lambda = LinqExpression.Lambda(unaryExpression).Compile();
                var value = lambda.DynamicInvoke();

                return AddAttributeValue(value);
            }

            throw new NotSupportedException($"HandleUnaryExpression() does not support '{unaryExpression}'.");
        }

        /// <summary>
        /// Process the <see cref="BinaryExpression"/> for the property and value.
        /// </summary>
        /// <param name="binaryExpression">The <see cref="BinaryExpression"/> to process for its property and value.</param>
        /// <returns>The property and value from the given <see cref="BinaryExpression"/>.</returns>
        /// <exception cref="NotSupportedException"></exception>
        private static (LinqExpression propertyExpression, LinqExpression valueExpression) GetPropertyAndValue(
            BinaryExpression binaryExpression)
        {
            // check if left or right is the property
            if (IsPropertyExpression(binaryExpression.Left))
            {
                return (binaryExpression.Left, binaryExpression.Right);
            }

            if (IsPropertyExpression(binaryExpression.Right))
            {
                return (binaryExpression.Right, binaryExpression.Left);
            }

            // neither left nor right is the property, evalute the expression
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
        /// <param name="linqExpression">The <see cref="LinqExpression"/> to get the property name from.</param>
        /// <returns>The property name from the given <see cref="LinqExpression"/>.</returns>
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

            // check for the JsonPropertyNameAttribute
            var jsonPropertyNameAttribute = pi.GetCustomAttribute<JsonPropertyNameAttribute>();

            return jsonPropertyNameAttribute?.Name ?? me.Member.Name;
        }

        /// <summary>
        /// Invole the given <see cref="LinqExpression"/> and return its result.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression"/> to invoke and return its result.</param>
        /// <returns>The result of the given <see cref="LinqExpression"/>.</returns>
        private static object? Invoke(
            LinqExpression linqExpression)
        {
            return LinqExpression.Lambda(linqExpression).Compile().DynamicInvoke();
        }

        /// <summary>
        /// Determines whether the given <see cref="LinqExpression"/> is a null value.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression"/> to check for a null value.</param>
        /// <returns><see langword="true"/> if the given <see cref="LinqExpression"/> is a null value; otherwise, <see langword="false"/>.</returns>
        private static bool IsNullValue(
            LinqExpression linqExpression)
        {
            return linqExpression is ConstantExpression ce && ce.Value == null;
        }

        /// <summary>
        /// Determines whether the given <see cref="LinqExpression"/> is a field or property on the named parameter.
        /// </summary>
        /// <param name="linqExpression">The <see cref="LinqExpression"/> to check for a field or property on the named parameter.</param>
        /// <returns><see langword="true"/> if the given <see cref="LinqExpression"/> is a field or property on the named parameter; otherwise, <see langword="false"/>.</returns>
        private static bool IsPropertyExpression(
            LinqExpression linqExpression)
        {
            return linqExpression is MemberExpression { Expression: ParameterExpression };
        }

        /// <summary>
        /// Convert the given value to a <see cref="DynamoDBEntry"/>.
        /// </summary>
        /// <param name="value">The value to convert to a <see cref="DynamoDBEntry"/>.</param>
        /// <returns>The <see cref="DynamoDBEntry"/> representation of the given value.</returns>
        private static DynamoDBEntry ToDynamoDBEntry(
            object? value)
        {
            if (value is null) return new DynamoDBNull();

            return DynamoDBEntryConversion.V2.ConvertToEntry(value.GetType(), value);
        }

        /// <summary>
        /// Validate the given <see cref="MethodCallExpression"/> for the expected argument count.
        /// </summary>
        /// <param name="methodCallExpression">The <see cref="MethodCallExpression"/> to validate for the expected argument count.</param>
        /// <param name="expectedArgumentCount">The expected argument count of the given <see cref="MethodCallExpression"/>.</param>
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
    }
}
