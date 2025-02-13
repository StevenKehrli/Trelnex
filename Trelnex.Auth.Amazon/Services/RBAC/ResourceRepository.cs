using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal static class ResourceSubjectName
{
    public static string Get()
    {
        return "RESOURCE#";
    }
}

internal class ResourceItem : BaseItem
{
    public ResourceItem(
        string resourceName)
        : base(resourceName)
    {
    }

    public ResourceItem(
        Dictionary<string, AttributeValue> attributeMap)
        : base(attributeMap)
    {
    }

    /// <summary>
    /// The name of the subject.
    /// </summary>
    public override string SubjectName => ResourceSubjectName.Get();
}

internal class ResourceScanItem : ScanItem
{
    public override string SubjectName => ResourceSubjectName.Get();
}

internal class ResourceRepository(
    AmazonDynamoDBClient client,
    string tableName)
    : BaseRepository<ResourceItem>(client, tableName)
{
}
