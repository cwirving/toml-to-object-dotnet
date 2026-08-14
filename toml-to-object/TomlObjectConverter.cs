using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;

namespace TomlToObject;

public static class TomlObjectConverter
{
    public static JsonNode? ConvertObjectToJsonNode(object? objectInToml) => objectInToml switch
    {
        null => null,
        TomlTable table => table.ToJsonObject(),
        TomlArray array => new JsonArray(array.Select(ConvertObjectToJsonNode).ToArray()),
        TomlTableArray tableArray => new JsonArray(tableArray.Select(table => table.ToJsonObject()).ToArray()),
        bool b => JsonValue.Create(b),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        uint ui => JsonValue.Create(ui),
        ulong ul => JsonValue.Create(ul),
        float f => JsonValue.Create(f),
        double d => JsonValue.Create(d),
        DateTime dt => JsonValue.Create(dt),
        TomlDateTime tdt => JsonValue.Create(tdt.DateTime), //!!! This is not enough!
        string str => JsonValue.Create(str),
        _ => JsonValue.Create(objectInToml.ToString())
    };
}
