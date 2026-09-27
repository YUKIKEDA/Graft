using System.Text.Json;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Screenshot;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Pipe;

#if GRAFT_TEST

/// <summary>
/// Deserializes wire params into <see cref="ProtocolMethodCatalog"/> types and checks required fields.
/// </summary>
internal static class RequestParamsReader
{
    internal readonly record struct Parsed<T>(T Value, bool HadObject)
        where T : class;

    public static Parsed<T> Read<T>(JsonElement? paramsElement)
        where T : class, new()
    {
        if (paramsElement is not { ValueKind: JsonValueKind.Object } element)
        {
            return new Parsed<T>(new T(), false);
        }

        try
        {
            var value = element.Deserialize<T>(JsonMessageCodec.Options) ?? new T();
            return new Parsed<T>(value, true);
        }
        catch (JsonException ex)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, ex.Message);
        }
    }

    public static ElementSelector ReadSelector(JsonElement? paramsElement)
    {
        var parsed = Read<ElementTargetParams>(paramsElement);
        return ToSelector(parsed.Value.AutomationId, parsed.Value.RuntimeId);
    }

    public static GetTreeOptions ReadGetTreeOptions(JsonElement? paramsElement)
    {
        GetTreeParams parsed;
        try
        {
            parsed = Read<GetTreeParams>(paramsElement).Value;
        }
        catch (ElementResolveException)
        {
            return new GetTreeOptions();
        }

        var maxDepth = parsed.Depth is >= 0 ? parsed.Depth.Value : GetTreeOptions.DefaultMaxDepth;
        var maxNodes = parsed.MaxNodes is > 0 ? parsed.MaxNodes.Value : GetTreeOptions.DefaultMaxNodes;
        return new GetTreeOptions { MaxDepth = maxDepth, MaxNodes = maxNodes };
    }

    public static string ReadHandshakeToken(JsonElement? paramsElement)
    {
        try
        {
            return Read<HandshakeParams>(paramsElement).Value.Token ?? string.Empty;
        }
        catch (ElementResolveException)
        {
            return string.Empty;
        }
    }

    public static ScreenshotOptions ReadScreenshotOptions(JsonElement? paramsElement)
    {
        ElementTargetParams target;
        try
        {
            target = Read<ElementTargetParams>(paramsElement).Value;
        }
        catch (ElementResolveException)
        {
            return ScreenshotOptions.Default;
        }

        if (string.IsNullOrWhiteSpace(target.AutomationId) && target.RuntimeId is null)
        {
            return ScreenshotOptions.Default;
        }

        return new ScreenshotOptions { Selector = ToSelector(target.AutomationId, target.RuntimeId) };
    }

    public static (ElementSelector Selector, string ToAutomationId) ReadDrag(JsonElement? paramsElement)
    {
        var parsed = Read<DragParams>(paramsElement);
        var toAutomationId = RequireString(parsed.Value.ToAutomationId, "toAutomationId", allowEmpty: false);
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), toAutomationId);
    }

    public static (ElementSelector Selector, double OffsetX, double OffsetY) ReadClickAt(JsonElement? paramsElement)
    {
        var parsed = Read<ClickAtParams>(paramsElement);
        if (!parsed.HadObject)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.offsetX is required.");
        }

        if (parsed.Value.OffsetX is not double offsetX)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.offsetX must be a number.");
        }

        if (parsed.Value.OffsetY is not double offsetY)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.offsetY must be a number.");
        }

        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), offsetX, offsetY);
    }

    public static (ElementSelector Selector, int Delta) ReadWheel(JsonElement? paramsElement)
    {
        var parsed = Read<WheelParams>(paramsElement);
        var delta = RequireInt(parsed, parsed.Value.Delta, "delta");
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), delta);
    }

    public static (ElementSelector Selector, string Value) ReadSetValue(JsonElement? paramsElement)
    {
        var parsed = Read<SetValueParams>(paramsElement);
        var value = RequireString(parsed.Value.Value, "value", allowEmpty: true);
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), value);
    }

    public static (ElementSelector Selector, string Text) ReadSendKeys(JsonElement? paramsElement)
    {
        var parsed = Read<SendKeysParams>(paramsElement);
        var text = RequireString(parsed.Value.Text, "text", allowEmpty: true);
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), text);
    }

    public static (ElementSelector Selector, string Text, TimeSpan Delay) ReadTypeHuman(JsonElement? paramsElement)
    {
        var parsed = Read<TypeHumanParams>(paramsElement);
        var text = RequireString(parsed.Value.Text, "text", allowEmpty: true);
        if (parsed.Value.DelayMs is not int delayMs || delayMs < 0)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.delayMs must be a non-negative integer.");
        }

        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), text, TimeSpan.FromMilliseconds(delayMs));
    }

    public static (ElementSelector Selector, string Keys) ReadPressKeys(JsonElement? paramsElement)
    {
        var parsed = Read<PressKeysParams>(paramsElement);
        var keys = RequireString(parsed.Value.Keys, "keys", allowEmpty: false, emptyMessage: "params.keys must be a non-empty chord string.");
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), keys);
    }

    public static (ElementSelector Selector, int? Index) ReadScrollIntoView(JsonElement? paramsElement)
    {
        var parsed = Read<ScrollIntoViewParams>(paramsElement);
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), parsed.Value.Index);
    }

    public static (ElementSelector Selector, int? Index, string? Key) ReadSelect(JsonElement? paramsElement)
    {
        var parsed = Read<SelectParams>(paramsElement);
        var hasIndex = parsed.Value.Index is not null;
        var hasKey = parsed.Value.Key is not null;
        if (hasIndex == hasKey)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params must have exactly one of index or key.");
        }

        if (hasIndex)
        {
            return (ToSelector(parsed.Value.AutomationId, runtimeId: null), parsed.Value.Index, null);
        }

        if (string.IsNullOrWhiteSpace(parsed.Value.Key))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.key must be a non-empty string.");
        }

        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), null, parsed.Value.Key);
    }

    public static (ElementSelector Selector, IReadOnlyList<int> Indexes) ReadSelectMany(JsonElement? paramsElement)
    {
        var parsed = Read<SelectManyParams>(paramsElement);
        if (parsed.Value.Indexes is null)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.indexes is required.");
        }

        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), parsed.Value.Indexes);
    }

    public static (ElementSelector Selector, string Path) ReadElementPath(JsonElement? paramsElement)
    {
        var parsed = Read<ElementPathParams>(paramsElement);
        var path = RequireString(parsed.Value.Path, "path", allowEmpty: false);
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), path);
    }

    public static (ElementSelector Selector, int Row, int? Column, string? ColumnKey) ReadCell(JsonElement? paramsElement)
    {
        var parsed = Read<CellParams>(paramsElement);
        if (!parsed.HadObject)
        {
            throw new ElementResolveException(
                GraftErrorCodes.SelectorInvalid,
                "params.row and exactly one of params.column or params.columnKey are required."
            );
        }

        if (parsed.Value.Row is not int row)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.row must be an integer.");
        }

        var hasColumn = parsed.Value.Column is not null;
        var hasKey = !string.IsNullOrWhiteSpace(parsed.Value.ColumnKey);
        if (hasColumn == hasKey)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "Exactly one of params.column or params.columnKey is required.");
        }

        return (
            ToSelector(parsed.Value.AutomationId, runtimeId: null),
            row,
            hasColumn ? parsed.Value.Column : null,
            hasKey ? parsed.Value.ColumnKey : null
        );
    }

    public static (ElementSelector Selector, int Row, int? Column, string? ColumnKey, string Value) ReadSetCellValue(JsonElement? paramsElement)
    {
        var (selector, row, column, columnKey) = ReadCell(paramsElement);
        var parsed = Read<CellParams>(paramsElement);
        var value = RequireString(parsed.Value.Value, "value", allowEmpty: true);
        return (selector, row, column, columnKey, value);
    }

    public static (ElementSelector Selector, string ColumnKey, string Value) ReadSelectRow(JsonElement? paramsElement)
    {
        var parsed = Read<SelectRowParams>(paramsElement);
        var columnKey = RequireString(parsed.Value.ColumnKey, "columnKey", allowEmpty: false);
        var value = RequireString(parsed.Value.Value, "value", allowEmpty: false);
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), columnKey, value);
    }

    public static (ElementSelector Selector, string ColumnKey) ReadColumnKey(JsonElement? paramsElement)
    {
        var parsed = Read<ColumnKeyParams>(paramsElement);
        var columnKey = RequireString(parsed.Value.ColumnKey, "columnKey", allowEmpty: false);
        return (ToSelector(parsed.Value.AutomationId, runtimeId: null), columnKey);
    }

    public static int ReadWindowId(JsonElement? paramsElement)
    {
        var parsed = Read<WindowIdParams>(paramsElement);
        return RequireInt(parsed, parsed.Value.WindowId, "windowId");
    }

    public static string ReadDialogPath(JsonElement? paramsElement)
    {
        var parsed = Read<DialogPathParams>(paramsElement);
        return RequireString(parsed.Value.Path, "path", allowEmpty: false);
    }

    public static string ReadMessageBoxResult(JsonElement? paramsElement)
    {
        var parsed = Read<MessageBoxArmParams>(paramsElement);
        return RequireString(parsed.Value.Result, "result", allowEmpty: false);
    }

    private static ElementSelector ToSelector(string? automationId, int? runtimeId) => new() { AutomationId = automationId, RuntimeId = runtimeId };

    private static string RequireString(string? value, string propertyName, bool allowEmpty, string? emptyMessage = null)
    {
        if (value is null)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} is required.");
        }

        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, emptyMessage ?? $"params.{propertyName} must be a non-empty string.");
        }

        return value;
    }

    private static int RequireInt<T>(Parsed<T> parsed, int? value, string propertyName)
        where T : class
    {
        if (!parsed.HadObject)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} is required.");
        }

        if (value is not int number)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} must be an integer.");
        }

        return number;
    }
}

#endif
