using System.Reflection;
using System.Text.Json.Serialization;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

namespace Trelnex.Core.Data;

internal class JsonPropertyNameAttributeReader : IMetadataReader
{
    public MappingAttribute[] GetAttributes(Type type) => [];

    public MappingAttribute[] GetAttributes(
        Type type,
        MemberInfo memberInfo)
    {
        var jsonPropertyNameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();

        if (jsonPropertyNameAttribute is null) return [];

		var columnAttribute = new ColumnAttribute()
		{
			Name = jsonPropertyNameAttribute.Name,
		};

		return [ columnAttribute ];
    }

    public MemberInfo[] GetDynamicColumns(Type type) => [];

	public string GetObjectID() => $".{nameof(JsonPropertyNameAttributeReader)}.";
}
