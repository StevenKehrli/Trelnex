using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal static class ScopeSubjectName
{
    public static string Get(
        string? scopeName = null)
    {
        return (scopeName is null)
            ? $"SCOPE#"
            : $"SCOPE#{scopeName}";
    }
}

internal class ScopeItem : BaseItem
{
    public ScopeItem(
        string resourceName,
        string scopeName)
        : base(resourceName)
    {
        ScopeName = scopeName;
    }

    public ScopeItem(
        Dictionary<string, AttributeValue> attributeMap)
        : base(attributeMap)
    {
        ScopeName = attributeMap["scopeName"].S;;
    }

    /// <summary>
    /// The name of the scope.
    /// </summary>
    public string ScopeName { get; init; }

    /// <summary>
    /// The name of the subject.
    /// </summary>
    public override string SubjectName => ScopeSubjectName.Get(ScopeName);

    /// <summary>
    /// Converts the scope to its attribute map
    /// </summary>
    /// <param name="item">The scope to convert to an attribute map.</param>
    /// <returns>The attribute map representing the scope.</returns>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        var baseAttributeMap = base.ToAttributeMap();

        return new Dictionary<string, AttributeValue>(baseAttributeMap)
        {
            { "scopeName", new AttributeValue(ScopeName) }
        };
    }
}

internal class ScopeScanItem : ScanItem
{
    public override string SubjectName => ScopeSubjectName.Get();
}

internal class ScopeRepository(
    AmazonDynamoDBClient client,
    string tableName)
    : BaseRepository<ScopeItem>(client, tableName)
{
}
