using System.Text.Json.Nodes;
using Tomlyn.Model;

namespace TomlToObject;

public static class TomlTableExtensions
{
    public static JsonObject? ToJsonObject(this TomlTable? table)
    {
        if (table is null) return null;

        var result = new JsonObject();
        foreach (var pair in table)
        {
            var convertedValue = TomlObjectConverter.ConvertObjectToJsonNode(pair.Value);
            result.Add(pair.Key, convertedValue);
        }
        
        return result;
    }
}