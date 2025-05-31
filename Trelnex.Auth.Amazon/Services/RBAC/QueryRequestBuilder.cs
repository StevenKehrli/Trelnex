using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

/// <summary>
/// Provides a fluent interface for building DynamoDB query requests for the RBAC system.
/// </summary>
/// <remarks>
/// This builder helps construct queries with proper key conditions and expression attribute values
/// for querying the RBAC entities using entityName and subjectName keys.
/// </remarks>
public class QueryRequestBuilder
{
    #region Prrivate Fields

    /// <summary>
    /// The key condition expression for entityName equality.
    /// </summary>
    private const string _entityNameEquals = "entityName = :entityName";

    /// <summary>
    /// The key condition expression for subjectName begins_with operation.
    /// </summary>
    private const string _subjectNameBeginsWith = "begins_with(subjectName, :subjectNameBeginsWith)";

    /// <summary>
    /// The DynamoDB table name for the query.
    /// </summary>
    private string? _tableName = null;

    /// <summary>
    /// Collection of key condition expressions to be combined with AND.
    /// </summary>
    private readonly List<string> _keyConditions = [];

    /// <summary>
    /// Dictionary of expression attribute values for the query parameters.
    /// </summary>
    private readonly Dictionary<string, AttributeValue> _expressionAttributeValues = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the DynamoDB table name for the query.
    /// </summary>
    /// <param name="tableName">The name of the DynamoDB table to query.</param>
    /// <returns>The current QueryRequestBuilder instance for method chaining.</returns>
    public QueryRequestBuilder WithTableName(
        string tableName)
    {
        _tableName = tableName;

        return this;
    }

    /// <summary>
    /// Adds an equality condition for the entityName (partition key).
    /// </summary>
    /// <param name="entityName">The entity name value to match exactly.</param>
    /// <returns>The current QueryRequestBuilder instance for method chaining.</returns>
    public QueryRequestBuilder EntityNameEquals(
        string entityName)
    {
        _keyConditions.Add(_entityNameEquals);
        _expressionAttributeValues.Add(
            ":entityName",
            new AttributeValue(entityName));

        return this;
    }

    /// <summary>
    /// Adds a begins_with condition for the subjectName (sort key).
    /// </summary>
    /// <param name="subjectNameBeginsWith">The prefix that the subjectName must start with.</param>
    /// <returns>The current QueryRequestBuilder instance for method chaining.</returns>
    public QueryRequestBuilder SubjectNameBeginsWith(
        string subjectNameBeginsWith)
    {
        _keyConditions.Add(_subjectNameBeginsWith);
        _expressionAttributeValues.Add(
            ":subjectNameBeginsWith",
            new AttributeValue(subjectNameBeginsWith));

        return this;
    }

    /// <summary>
    /// Builds and returns the configured DynamoDB QueryRequest.
    /// </summary>
    /// <returns>A configured QueryRequest ready for execution.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties like TableName are not set.</exception>
    public QueryRequest Build()
    {
        string keyCondition = string.Join(" AND ", _keyConditions);

        return new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = keyCondition,
            ExpressionAttributeValues = _expressionAttributeValues,
            ConsistentRead = true
        };
    }

    #endregion
}
