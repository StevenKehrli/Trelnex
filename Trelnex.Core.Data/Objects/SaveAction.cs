using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SaveAction
{
    UNKNOWN = 0,
    CREATED = 1,
    UPDATED = 2,
    DELETED = 3,
}
