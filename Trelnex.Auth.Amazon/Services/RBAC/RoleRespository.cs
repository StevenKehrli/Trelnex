using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal class RoleSubjectName
{
    public static string Get(
        string? roleName = null)
    {
        return (roleName is null)
            ? $"ROLE#"
            : $"ROLE#{roleName}";
    }
}

internal class RoleItem : BaseItem
{
    public RoleItem(
        string resourceName,
        string roleName)
        : base(resourceName)
    {
        RoleName = roleName;
    }

    public RoleItem(
        Dictionary<string, AttributeValue> attributeMap)
        : base(attributeMap)
    {
        RoleName = attributeMap["roleName"].S;;
    }

    /// <summary>
    /// The name of the role.
    /// </summary>
    public string RoleName { get; init; }

    /// <summary>
    /// The name of the subject.
    /// </summary>
    public override string SubjectName => RoleSubjectName.Get(RoleName);

    /// <summary>
    /// Converts the specified assigned role to its attribute map
    /// </summary>
    /// <param name="item">The assigned role to convert to an attribute map.</param>
    /// <returns>The attribute map representing the specified assigned role.</returns>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        var baseAttributeMap = base.ToAttributeMap();

        return new Dictionary<string, AttributeValue>(baseAttributeMap)
        {
            { "roleName", new AttributeValue(RoleName) }
        };
    }
}

internal class RoleScanItem : ScanItem
{
    public override string SubjectName => RoleSubjectName.Get();
}

internal class RoleRepository(
    AmazonDynamoDBClient client,
    string tableName)
    : BaseRepository<RoleItem>(client, tableName)
{
}
