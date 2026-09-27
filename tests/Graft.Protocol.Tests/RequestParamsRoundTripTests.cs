using System.Reflection;
using System.Text.Json;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Protocol.Tests;

public sealed class RequestParamsRoundTripTests
{
    /// <summary>
    /// Each wire params type serializes with camelCase names and deserializes back to the same JSON.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - One populated instance of every params type in <see cref="ProtocolMethodCatalog"/>
    ///
    /// Steps:
    /// - Serialize with <see cref="JsonMessageCodec.SerializeParams{T}"/>
    /// - Deserialize with <see cref="JsonMessageCodec.Options"/> and serialize again
    ///
    /// Expected:
    /// - The JSON contains the wire property name
    /// - The second serialization matches the first
    /// - getCellText params omit a null value and keep column 0
    /// </remarks>
    [Fact]
    public void EachParamsType_RoundTrip_PreservesJson()
    {
        AssertRoundTrip(new HandshakeParams { Token = "abc" }, "token");
        AssertRoundTrip(new GetTreeParams { Depth = 25, MaxNodes = 2000 }, "maxNodes");
        AssertRoundTrip(new EmptyParams(), "{}");
        AssertRoundTrip(new WindowIdParams { WindowId = 3 }, "windowId");
        AssertRoundTrip(new ElementTargetParams { AutomationId = "Button", RuntimeId = 4 }, "runtimeId");
        AssertRoundTrip(new DragParams { AutomationId = "From", ToAutomationId = "To" }, "toAutomationId");
        AssertRoundTrip(
            new ClickAtParams
            {
                AutomationId = "Target",
                OffsetX = 1.5,
                OffsetY = -2,
            },
            "offsetX"
        );
        AssertRoundTrip(new WheelParams { AutomationId = "List", Delta = 120 }, "delta");
        AssertRoundTrip(new SetValueParams { AutomationId = "Box", Value = "hello" }, "value");
        AssertRoundTrip(new SendKeysParams { AutomationId = "Box", Text = "ab" }, "text");
        AssertRoundTrip(
            new TypeHumanParams
            {
                AutomationId = "Box",
                Text = "ab",
                DelayMs = 40,
            },
            "delayMs"
        );
        AssertRoundTrip(new PressKeysParams { AutomationId = "Box", Keys = "Control+A" }, "keys");
        AssertRoundTrip(new ScrollIntoViewParams { AutomationId = "List", Index = 2 }, "index");
        AssertRoundTrip(new SelectParams { AutomationId = "List", Key = "Item" }, "key");
        AssertRoundTrip(new SelectManyParams { AutomationId = "List", Indexes = [1, 3] }, "indexes");
        AssertRoundTrip(new ElementPathParams { AutomationId = "Menu", Path = "File/Save" }, "path");
        AssertRoundTrip(
            new CellParams
            {
                AutomationId = "Grid",
                Row = 1,
                ColumnKey = "Name",
                Value = "Ada",
            },
            "columnKey"
        );
        AssertRoundTrip(
            new SelectRowParams
            {
                AutomationId = "Grid",
                ColumnKey = "Name",
                Value = "Ada",
            },
            "columnKey"
        );
        AssertRoundTrip(new ColumnKeyParams { AutomationId = "Grid", ColumnKey = "Name" }, "columnKey");
        AssertRoundTrip(new DialogPathParams { Path = @"C:\temp\a.txt" }, "path");
        AssertRoundTrip(new MessageBoxArmParams { Result = "OK" }, "result");

        var cellWithoutValue = JsonMessageCodec.SerializeParams(
            new CellParams
            {
                AutomationId = "Grid",
                Row = 1,
                Column = 0,
            }
        );
        Assert.DoesNotContain("value", cellWithoutValue.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("\"column\":0", cellWithoutValue.GetRawText(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A JSON null string becomes empty, and an omitted string stays null.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - setValue JSON with value null, and setValue JSON with no value property
    ///
    /// Steps:
    /// - Deserialize both documents as <see cref="SetValueParams"/>
    ///
    /// Expected:
    /// - Null value is an empty string
    /// - Omitted value is null
    /// </remarks>
    [Fact]
    public void SetValue_NullValue_BecomesEmpty_OmittedStaysNull()
    {
        var present = JsonSerializer.Deserialize<SetValueParams>("""{"automationId":"Box","value":null}""", JsonMessageCodec.Options);
        var omitted = JsonSerializer.Deserialize<SetValueParams>("""{"automationId":"Box"}""", JsonMessageCodec.Options);

        Assert.NotNull(present);
        Assert.Equal(string.Empty, present.Value);
        Assert.NotNull(omitted);
        Assert.Null(omitted.Value);
    }

    /// <summary>
    /// Every <see cref="ProtocolMethods"/> constant has exactly one params type.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="ProtocolMethodCatalog.ParamTypes"/> is populated
    ///
    /// Steps:
    /// - Read public string constants on <see cref="ProtocolMethods"/>
    /// - Compare them to the catalog keys
    ///
    /// Expected:
    /// - The two sets are equal
    /// </remarks>
    [Fact]
    public void Catalog_Covers_EveryProtocolMethod()
    {
        var names = typeof(ProtocolMethods)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(names.Order(StringComparer.Ordinal), ProtocolMethodCatalog.ParamTypes.Keys.Order(StringComparer.Ordinal));
    }

    private static void AssertRoundTrip<T>(T value, string expectedFragment)
    {
        var element = JsonMessageCodec.SerializeParams(value);
        var json = element.GetRawText();
        Assert.Contains(expectedFragment, json, StringComparison.Ordinal);

        var again = element.Deserialize<T>(JsonMessageCodec.Options);
        Assert.NotNull(again);
        Assert.Equal(json, JsonMessageCodec.SerializeParams(again).GetRawText());
    }
}
