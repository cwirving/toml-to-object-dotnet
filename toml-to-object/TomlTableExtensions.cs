using System.Text.Json.Nodes;
using Tomlyn.Model;

namespace TomlToObject;

/// <summary>
///     Extensions for the Tomlyn <see cref="TomlTable" /> type.
/// </summary>
public static class TomlTableExtensions
{
    /// <summary>
    ///     Convert this (potentially null) TOML table object into the corresponding <c>System.Text.Json</c>
    ///     <see cref="JsonObject" />, using the <see cref="TomlObjectConverter" /> class for the conversion details.
    /// </summary>
    /// <remarks>
    ///     The conversion preserves the types the TOML parser created. So, numbers will either be <see cref="Int64" /> or
    ///     <see cref="Double" /> depending on whether they are integer or floating point, dates and times will be
    ///     <see cref="DateTimeOffset" />. If in doubt, refer to the behavior of the Tomlyn library.
    /// </remarks>
    /// <param name="table">The TOML table to convert to a JSON object.</param>
    /// <returns>The <see cref="JsonObject" /> that corresponds to this table.</returns>
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