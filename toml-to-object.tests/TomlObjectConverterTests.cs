using System.Text.Json;
using System.Text.Json.Nodes;
using TomlToObject;
using Tomlyn;
using Tomlyn.Model;

namespace toml_to_object.tests;

public class TomlObjectConverterTests
{
    [Test]
    public async Task NullToNull()
    {
        await Assert.That(TomlObjectConverter.ConvertObjectToJsonNode(null)).IsNull();
    }

    [Test]
    public async Task ArrayToArray()
    {
        var table = TomlSerializer.Deserialize<TomlTable>("ints=[1,2,3,4,5]");
        var actual = TomlObjectConverter.ConvertObjectToJsonNode(table!["ints"]);
        await Assert.That(actual).IsNotNull();
        var actualArray = actual.AsArray();
        await Assert.That(actualArray).IsNotNull();
        await Assert.That(actualArray.Count).IsEqualTo(5);
        await Assert.That(JsonSerializer.Serialize(actualArray)).IsEqualTo("[1,2,3,4,5]");
    }

    [Test]
    public async Task BoolToBool()
    {
        await Assert.That(TomlObjectConverter.ConvertObjectToJsonNode(true)).IsNotNull();
        await Assert.That(TomlObjectConverter.ConvertObjectToJsonNode(true)!.GetValue<bool>()).IsTrue();

        await Assert.That(TomlObjectConverter.ConvertObjectToJsonNode(false)).IsNotNull();
        await Assert.That(TomlObjectConverter.ConvertObjectToJsonNode(false)!.GetValue<bool>()).IsFalse();
    }

    [Test]
    [Arguments(1)]
    [Arguments(2L)]
    [Arguments(3U)]
    [Arguments(4UL)]
    [Arguments(5.0F)]
    [Arguments(6.0)]
    public async Task NumbersToNumber(object input)
    {
        var actual = TomlObjectConverter.ConvertObjectToJsonNode(input);

        await Assert.That(actual).IsNotNull();
        await Assert.That(actual.GetValueKind()).IsEqualTo(JsonValueKind.Number);
        await Assert.That(actual.GetValue<object>()).IsEqualTo(input);
    }

    [Test]
    public async Task HexNumberToLong()
    {
        // Tomlyn doesn't appear to ever parse integers as unsigned, even when they are written in hex.
        var table = TomlSerializer.Deserialize<TomlTable>("x=0xfffffffe");
        await Assert.That(table).IsNotNull();
        var actual = TomlObjectConverter.ConvertObjectToJsonNode(table);
        await Assert.That(actual).IsNotNull();

        var actualObject = actual.AsObject();
        var x = actual["x"].AsValue();
        await Assert.That(x).IsNotNull();

        await Assert.That(x.GetValue<long>()).IsEqualTo(4294967294L);
    }

    [Test]
    public async Task TableToObject()
    {
        var table = TomlSerializer.Deserialize<TomlTable>("[abc]\nx=123\ny=123.456");
        await Assert.That(table).IsNotNull();
        var actual = TomlObjectConverter.ConvertObjectToJsonNode(table);

        await Assert.That(actual).IsNotNull();
        var actualObject = actual.AsObject();
        await Assert.That(actualObject).IsNotNull();
        var abc = actualObject["abc"].AsObject();

        await Assert.That(abc).IsNotNull();
        await Assert.That(abc["x"].AsValue().GetValue<long>()).IsEqualTo(123L);
        await Assert.That(abc["y"].AsValue().GetValue<double>()).IsEqualTo(123.456);
    }

    [Test]
    public async Task TableArrayToTableOfObject()
    {
        var tableArray = TomlSerializer.Deserialize<TomlTable>("[[abc]]\nx=123\n[[abc]]\nx=456");
        await Assert.That(tableArray).IsNotNull();
        var actual = TomlObjectConverter.ConvertObjectToJsonNode(tableArray);

        await Assert.That(actual).IsNotNull();
        var actualObject = actual.AsObject();
        await Assert.That(actualObject).IsNotNull();
        var abc = actualObject["abc"];

        await Assert.That(abc).IsNotNull();
        var abcArray = abc.AsArray();
        await Assert.That(abcArray).IsNotNull();
        await Assert.That(abcArray.Count).IsEqualTo(2);
        await Assert.That(JsonSerializer.Serialize(abcArray)).IsEqualTo("[{\"x\":123},{\"x\":456}]");
    }

    [Test]
    public async Task OffsetDateTimeToDateTimeOffset()
    {
        var input = TomlSerializer.Deserialize<TomlTable>("x=2026-08-01T01:09:10Z");
        var actual = TomlObjectConverter.ConvertObjectToJsonNode(input)!.AsObject();
        await Assert.That(actual).IsNotNull();
        var x = actual["x"].AsValue();

        await Assert.That(x).IsNotNull();
        await Assert.That(x.GetValue<DateTimeOffset>()).IsEqualTo(DateTimeOffset.Parse("2026-08-01T01:09:10Z"));
    }

    [Test]
    public async Task LocalDateTimeToDateTime()
    {
        var input = TomlSerializer.Deserialize<TomlTable>("x=2026-08-01T01:09:10");
        var obj = TomlObjectConverter.ConvertObjectToJsonNode(input)!.AsObject();
        await Assert.That(obj).IsNotNull();
        var x = obj["x"].AsValue();

        await Assert.That(x).IsNotNull();
        var actual = x.GetValue<DateTime>();
        var expected = DateTime.Parse("2026-08-01T01:09:10");
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task DateToDateOnly()
    {
        var input = TomlSerializer.Deserialize<TomlTable>("x=2026-08-01");
        var actual = TomlObjectConverter.ConvertObjectToJsonNode(input)!.AsObject();
        await Assert.That(actual).IsNotNull();
        var x = actual["x"].AsValue();

        await Assert.That(x).IsNotNull();
        await Assert.That(x.GetValue<DateOnly>()).IsEqualTo(new DateOnly(2026, 8, 1));
    }

    [Test]
    public async Task TimeToTimeOnly()
    {
        var input = TomlSerializer.Deserialize<TomlTable>("x=14:51:01");
        var obj = TomlObjectConverter.ConvertObjectToJsonNode(input)!.AsObject();
        await Assert.That(obj).IsNotNull();
        var x = obj["x"].AsValue();

        await Assert.That(x).IsNotNull();
        var actual = x.GetValue<TimeOnly>();
        var expected = new TimeOnly(14, 51, 1);
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task DatesAndTimesAreJsonEquivalent()
    {
        var tomlInput = """
                        utc      = 2026-08-01T01:09:10Z
                        withZone = 2026-08-01T01:09:10+05:00
                        local    = 2026-08-01T01:09:10
                        date     = 2026-08-01
                        time     = 14:51:01
                        """;

        var jsonInput = """
                        {
                          "utc":      "2026-08-01T01:09:10+00:00",
                          "withZone": "2026-08-01T01:09:10+05:00",
                          "local":    "2026-08-01T01:09:10",
                          "date":     "2026-08-01",
                          "time":     "14:51:01"
                        }
                        """;

        var input = TomlSerializer.Deserialize<TomlTable>(tomlInput);
        var actualObj = TomlObjectConverter.ConvertObjectToJsonNode(input)!.AsObject();
        await Assert.That(actualObj).IsNotNull();

        var expectedObj = JsonSerializer.Deserialize<JsonObject>(jsonInput);

        // Even though the TOML parser loaded more precise types, we expect them to be equivalent.
        await Assert.That(JsonNode.DeepEquals(actualObj, expectedObj)).IsTrue();
    }
}