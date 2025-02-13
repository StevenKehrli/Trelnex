using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal static class PrincipalRoleSubjectName
{
    public static string Get(
        string? principalId = null,
        string? roleName = null)
    {
        return (principalId is null)
            ? $"PRINCIPAL#"
            : (roleName is null)
                ? $"PRINCIPAL#{principalId}#ROLE#"
                : $"PRINCIPAL#{principalId}#ROLE#{roleName}";
    }
}

internal class PrincipalRoleItem : BaseItem
{
    public PrincipalRoleItem(
        string principalId,
        string resourceName,
        string roleName)
        : base(resourceName)
    {
        PrincipalId = principalId;
        RoleName = roleName;
    }

    public PrincipalRoleItem(
        Dictionary<string, AttributeValue> attributeMap)
        : base(attributeMap)
    {
        PrincipalId = attributeMap["principalId"].S;
        RoleName = attributeMap["roleName"].S;;
    }

    /// <summary>
    /// The unique id of the princioal
    /// </summary>
    public string PrincipalId { get; init; }

    /// <summary>
    /// The name of the role.
    /// </summary>
    public string RoleName { get; init; }

    /// <summary>
    /// The name of the subject.
    /// </summary>
    public override string SubjectName => PrincipalRoleSubjectName.Get(PrincipalId, RoleName);

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
            { "principalId", new AttributeValue(PrincipalId) },
            { "roleName", new AttributeValue(RoleName) }
        };
    }
}

internal class PrincipalRoleScanItem : ScanItem
{
    public override string SubjectName => GetSubjectName();

    public void AddPrincipalId(
        string principalId)
    {
        AddAttribute("principalId", principalId);
    }

    public void AddRoleName(
        string roleName)
    {
        AddAttribute("roleName", roleName);
    }

    private string GetSubjectName()
    {
        var principalId = AttributeMap.GetValueOrDefault("principalId")?.S;
        var roleName = AttributeMap.GetValueOrDefault("roleName")?.S;

        return PrincipalRoleSubjectName.Get(principalId, roleName);
    }
}

internal class PrincipalRoleRepository(
    AmazonDynamoDBClient client,
    string tableName)
    : BaseRepository<PrincipalRoleItem>(client, tableName)
{
}
