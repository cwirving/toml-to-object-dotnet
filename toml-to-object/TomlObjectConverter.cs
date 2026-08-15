using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;

namespace TomlToObject;

/// <summary>
///     Static class with methods that are useful to convert parsed TOML from the Tomlyn library to corresponding
///     <c>System.Text.Json</c> concepts.
/// </summary>
public static class TomlObjectConverter
{
    /// <summary>
    ///     Given an object parsed by Tomlyn, create the corresponding <c>System.Text.Json</c> <see cref="JsonNode" />.
    ///     Most primitive types are simply wrapped in a <see cref="JsonNode" />, <c>Tomlyn</c> types are converted and unknown
    ///     types are conveted to strings.
    ///     <see cref="TomlTable" /> objects are converted to <see cref="JsonNode" /> using the
    ///     <see cref="TomlTableExtensions.ToJsonObject" /> extension method.
    ///     <see cref="TomlTable" /> and <see cref="TomlTableArray" /> objects are converted the corresponding
    ///     <see cref="JsonArray" /> objects.
    ///     <see cref="TomlDateTime" /> objects are converted to <see cref="JsonValue" /> objects containing the appropriate
    ///     <see cref="DateTime" />, <see cref="DateTimeOffset" />, <see cref="DateOnly" /> or <see cref="TimeOnly" /> value.
    ///     See the <see cref="TomlDateTimeToJsonValue" /> method.
    /// </summary>
    /// <param name="objectInToml">
    ///     A parse result from the <c>Tomlyn</c> library. Either a <c>Tomlyn</c> type, such as
    ///     <see cref="TomlTable" />, or a .NET primitive type that was loaded.
    /// </param>
    /// <returns>
    ///     A <see cref="JsonNode" /> that corresponds to the input, but converted to be representable in
    ///     <c>System.Text.Json</c>.
    /// </returns>
    public static JsonNode? ConvertObjectToJsonNode(object? objectInToml)
    {
        return objectInToml switch
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
            TomlDateTime tdt => TomlDateTimeToJsonValue(tdt),
            string str => JsonValue.Create(str),
            _ => JsonValue.Create(objectInToml.ToString())
        };
    }

    /// <summary>
    ///     Convert a <see cref="TomlDateTime" /> into a <see cref="JsonValue" /> containing the most appropriate data type:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>TomlDateTime.Kind</term>
    ///             <description>Result (wrapped in a <see cref="JsonNode" />)</description>
    ///         </listheader>
    ///         <item>
    ///             <term>TomlDateTimeKind.OffsetDateTimeByZ</term>
    ///             <description>A <see cref="DateTimeOffset" /> representing the exact offset date and time.</description>
    ///         </item>
    ///         <item>
    ///             <term>TomlDateTimeKind.OffsetDateTimeByNumber</term>
    ///             <description>A <see cref="DateTimeOffset" /> representing the exact offset date and time.</description>
    ///         </item>
    ///         <item>
    ///             <term>TomlDateTimeKind.LocalDateTime</term>
    ///             <description>A <see cref="DateTime" /> representing the local (i.e., unknown time zone) date-time.</description>
    ///         </item>
    ///         <item>
    ///             <term>TomlDateTimeKind.LocalDate</term>
    ///             <description>A <see cref="DateOnly" /> representing the local date (no time).</description>
    ///         </item>
    ///         <item>
    ///             <term>TomlDateTimeKind.LocalTime</term>
    ///             <description>A <see cref="TimeOnly" /> representing the local time (no date).</description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <param name="tdt"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">The input is of an unknown kind.</exception>
    public static JsonValue TomlDateTimeToJsonValue(TomlDateTime tdt)
    {
        var dateTime = tdt.DateTime;

        return tdt.Kind switch
        {
            TomlDateTimeKind.OffsetDateTimeByZ => JsonValue.Create(dateTime),
            TomlDateTimeKind.OffsetDateTimeByNumber => JsonValue.Create(dateTime),
            TomlDateTimeKind.LocalDateTime => JsonValue.Create(dateTime.DateTime),
            TomlDateTimeKind.LocalDate => JsonValue.Create(new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day))!,
            TomlDateTimeKind.LocalTime => JsonValue.Create(new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second,
                dateTime.Millisecond))!,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}