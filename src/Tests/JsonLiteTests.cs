using AllO.Services.Mcp;
using Xunit;

namespace AllO.Tests;

public class JsonLiteTests
{
    [Fact]
    public void Parse_JsonRpcRequest()
    {
        var obj = JsonLite.Parse(
            "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"get_element\",\"arguments\":{\"element_id\":123456,\"flag\":true,\"note\":null}}}")
            as Dictionary<string, object?>;

        Assert.NotNull(obj);
        Assert.Equal("2.0", obj!["jsonrpc"]);
        Assert.Equal(3d, obj["id"]);
        var p = Assert.IsType<Dictionary<string, object?>>(obj["params"]);
        var args = Assert.IsType<Dictionary<string, object?>>(p["arguments"]);
        Assert.Equal(123456d, args["element_id"]);
        Assert.Equal(true, args["flag"]);
        Assert.Null(args["note"]);
    }

    [Fact]
    public void Roundtrip_PreservesStructure()
    {
        var original = new Dictionary<string, object?>
        {
            ["text"] = "línea1\nlínea2 \"quoted\" \\path\\ ₡500",
            ["number"] = 12.5,
            ["list"] = new List<object?> { 1d, "two", null, false },
            ["nested"] = new Dictionary<string, object?> { ["k"] = "v" }
        };

        var parsed = JsonLite.Parse(JsonLite.Serialize(original)) as Dictionary<string, object?>;

        Assert.NotNull(parsed);
        Assert.Equal(original["text"], parsed!["text"]);
        Assert.Equal(12.5d, parsed["number"]);
        var list = Assert.IsType<List<object?>>(parsed["list"]);
        Assert.Equal(new List<object?> { 1d, "two", null, false }, list);
        var nested = Assert.IsType<Dictionary<string, object?>>(parsed["nested"]);
        Assert.Equal("v", nested["k"]);
    }

    [Fact]
    public void Parse_EscapesAndUnicode()
    {
        Assert.Equal("a\tb", JsonLite.Parse("\"a\\tb\""));
        Assert.Equal("ñ", JsonLite.Parse("\"\\u00f1\""));
        Assert.Equal(-1.5e3, JsonLite.Parse("-1.5e3"));
    }

    [Fact]
    public void Parse_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => JsonLite.Parse("{\"a\":}"));
        Assert.Throws<FormatException>(() => JsonLite.Parse("{\"a\":1} trailing"));
        Assert.Throws<FormatException>(() => JsonLite.Parse("\"unterminated"));
    }

    [Fact]
    public void Serialize_EscapesSpecialAndControlChars()
    {
        string json = JsonLite.Serialize(new Dictionary<string, object?>
        {
            ["k"] = "a\"b\\c\nd" + (char)1
        });
        Assert.Equal("{\"k\":\"a\\\"b\\\\c\\nd\\u0001\"}", json);
    }
}
