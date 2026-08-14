using System.Text.Json;
using TomlToObject;
using Tomlyn;
using Tomlyn.Model;
using TUnit.Assertions.Conditions.Json;

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
    public async Task LocalDateTimeToDateTimeOffset()
    {
        var input = TomlSerializer.Deserialize<TomlTable>("x=2026-08-01T01:09:10");
        var obj = TomlObjectConverter.ConvertObjectToJsonNode(input)!.AsObject();
        await Assert.That(obj).IsNotNull();
        var x = obj["x"].AsValue();
        await Assert.That(x).IsNotNull();
        // We're not trying to litigate what Tomlyn should be parsing, just checking that we pass it through unchanged.
        var actual = x.GetValue<DateTimeOffset>();
        var expected =
            new DateTimeOffset(new DateOnly(2026,8, 1), new TimeOnly(1, 9,10), TimeSpan.Zero);
        await Assert.That(actual).IsEqualTo(expected);
    }
    [Test]
    public async Task DateToDateTimeOffset()
    {
        var input = TomlSerializer.Deserialize<TomlTable>("x=2026-08-01");
        var actual = TomlObjectConverter.ConvertObjectToJsonNode(input)!.AsObject();
        await Assert.That(actual).IsNotNull();
        var x = actual["x"].AsValue();
        await Assert.That(x).IsNotNull();
        // We're not trying to litigate what Tomlyn should be parsing, just checking that we pass it through unchanged.
        await Assert.That(x.GetValue<DateTimeOffset>()).IsEqualTo(new DateTimeOffset(new DateOnly(2026,8, 1), new TimeOnly(0, 0,0), TimeSpan.Zero));
    }

    [Test]
    public async Task TimeToDateTimeOffset()
    {
        var input = TomlSerializer.Deserialize<TomlTable>("x=14:51:01");
        var obj = TomlObjectConverter.ConvertObjectToJsonNode(input)!.AsObject();
        await Assert.That(obj).IsNotNull();
        var x = obj["x"].AsValue();
        await Assert.That(x).IsNotNull();
        // We're not trying to litigate what Tomlyn should be parsing, just checking that we pass it through unchanged.
        var actual = x.GetValue<DateTimeOffset>();
        var expected =
            new DateTimeOffset(DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(14, 51, 1), TimeSpan.Zero);
        await Assert.That(actual).IsEqualTo(expected);
    }
    
}