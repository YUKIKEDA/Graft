using System.Text.Json;
using Graft.Protocol;

namespace Graft.Core.Scenario;

/// <summary>
/// Parses Scenario JSON (exchange form) into a compiled <see cref="ScenarioDocument"/>.
/// </summary>
/// <remarks>
/// JSON Schema: <c>.dev/scenario.schema.json</c>. Execute with <see cref="ScenarioRunner"/>.
/// </remarks>
public static class ScenarioJson
{
    /// <summary>
    /// Shared serializer options for Scenario JSON.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Parses Scenario JSON text into a compiled scenario.
    /// </summary>
    /// <param name="json">Scenario JSON.</param>
    /// <returns>Compiled scenario with typed operations.</returns>
    /// <exception cref="GraftException">JSON is invalid or fails Scenario contract checks.</exception>
    public static ScenarioDocument Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                }
            );
        }
        catch (JsonException ex)
        {
            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                $"Scenario JSON is invalid: {ex.Message}",
                ex
            );
        }

        using (document)
        {
            return Compile(document.RootElement);
        }
    }

    /// <summary>
    /// Reads a Scenario JSON file and parses it.
    /// </summary>
    /// <param name="path">Path to a <c>.json</c> Scenario file.</param>
    /// <returns>Compiled scenario.</returns>
    /// <exception cref="GraftException">File or JSON is invalid.</exception>
    public static ScenarioDocument ParseFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            var json = File.ReadAllText(path);
            return Parse(json);
        }
        catch (GraftException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                $"Failed to read Scenario file '{path}': {ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    /// Compiles a Scenario JSON root element into the internal operation model.
    /// </summary>
    /// <param name="root">JSON object root.</param>
    /// <returns>Compiled scenario.</returns>
    /// <exception cref="GraftException">Contract validation failed.</exception>
    public static ScenarioDocument Compile(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw Invalid("Scenario root must be a JSON object.");
        }

        if (
            !root.TryGetProperty("v", out var versionElement)
            || versionElement.ValueKind != JsonValueKind.Number
        )
        {
            throw Invalid("Scenario requires integer property 'v'.");
        }

        var version = versionElement.GetInt32();
        if (version != ScenarioDocument.CurrentVersion)
        {
            throw Invalid(
                $"Unsupported Scenario version {version}; expected {ScenarioDocument.CurrentVersion}."
            );
        }

        string? name = null;
        if (root.TryGetProperty("name", out var nameElement))
        {
            if (nameElement.ValueKind != JsonValueKind.String)
            {
                throw Invalid("Scenario 'name' must be a string when present.");
            }

            name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw Invalid("Scenario 'name' must be non-empty when present.");
            }
        }

        if (
            !root.TryGetProperty("steps", out var stepsElement)
            || stepsElement.ValueKind != JsonValueKind.Array
        )
        {
            throw Invalid("Scenario requires a non-empty 'steps' array.");
        }

        if (stepsElement.GetArrayLength() == 0)
        {
            throw Invalid("Scenario 'steps' must contain at least one step.");
        }

        var operations = new List<ScenarioOperation>(stepsElement.GetArrayLength());
        var index = 0;
        foreach (var step in stepsElement.EnumerateArray())
        {
            operations.Add(CompileStep(step, index));
            index++;
        }

        return new ScenarioDocument
        {
            Version = version,
            Name = name,
            Operations = operations,
        };
    }

    private static ScenarioOperation CompileStep(JsonElement step, int index)
    {
        if (step.ValueKind != JsonValueKind.Object)
        {
            throw Invalid($"steps[{index}] must be a JSON object.");
        }

        if (
            !step.TryGetProperty("action", out var actionElement)
            || actionElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] requires string property 'action'.");
        }

        var action = actionElement.GetString();
        return action switch
        {
            ScenarioActions.Launch => CompileLaunch(step, index),
            ScenarioActions.Invoke => CompileInvoke(step, index),
            ScenarioActions.RightClick => CompileRightClick(step, index),
            ScenarioActions.DoubleClick => CompileDoubleClick(step, index),
            ScenarioActions.Hover => CompileHover(step, index),
            ScenarioActions.Drag => CompileDrag(step, index),
            ScenarioActions.ClickAt => CompileClickAt(step, index),
            ScenarioActions.Wheel => CompileWheel(step, index),
            ScenarioActions.SetValue => CompileSetValue(step, index),
            ScenarioActions.Toggle => CompileToggle(step, index),
            ScenarioActions.SendKeys => CompileSendKeys(step, index),
            ScenarioActions.PressKeys => CompilePressKeys(step, index),
            ScenarioActions.Screenshot => CompileScreenshot(step, index),
            ScenarioActions.ScrollIntoView => CompileScrollIntoView(step, index),
            ScenarioActions.Select => CompileSelect(step, index),
            ScenarioActions.SelectMany => CompileSelectMany(step, index),
            ScenarioActions.SelectMenu => CompileSelectMenu(step, index),
            ScenarioActions.SelectTree => CompileSelectTree(step, index),
            ScenarioActions.Expand => CompileExpand(step, index),
            ScenarioActions.Collapse => CompileCollapse(step, index),
            ScenarioActions.ExpectName => CompileExpectName(step, index),
            ScenarioActions.ExpectSelected => CompileExpectSelected(step, index),
            ScenarioActions.ExpectExpanded => CompileExpectExpanded(step, index),
            ScenarioActions.ExpectChecked => CompileExpectChecked(step, index),
            ScenarioActions.ExpectEnabled => CompileExpectEnabled(step, index),
            ScenarioActions.ExpectVisible => CompileExpectVisible(step, index),
            ScenarioActions.ExpectFocused => CompileExpectFocused(step, index),
            ScenarioActions.ExpectNameContains => CompileExpectNameContains(step, index),
            ScenarioActions.ExpectNameMatches => CompileExpectNameMatches(step, index),
            ScenarioActions.ExpectValue => CompileExpectValue(step, index),
            ScenarioActions.ExpectToolTip => CompileExpectToolTip(step, index),
            ScenarioActions.WaitFor => CompileWaitFor(step, index),
            ScenarioActions.ExpectGone => CompileExpectGone(step, index),
            ScenarioActions.GetCellText => CompileGetCellText(step, index),
            ScenarioActions.SetCellValue => CompileSetCellValue(step, index),
            ScenarioActions.SelectCell => CompileSelectCell(step, index),
            ScenarioActions.SelectRow => CompileSelectRow(step, index),
            ScenarioActions.ClickColumnHeader => CompileClickColumnHeader(step, index),
            ScenarioActions.AddRow => CompileAddRow(step, index),
            ScenarioActions.DeleteSelectedRows => CompileDeleteSelectedRows(step, index),
            ScenarioActions.ExpectCellText => CompileExpectCellText(step, index),
            ScenarioActions.ArmOpenFile => CompileArmOpenFile(step, index),
            ScenarioActions.ArmOpenFileCancel => new ArmOpenFileCancelOperation(),
            ScenarioActions.ArmSaveFile => CompileArmSaveFile(step, index),
            ScenarioActions.ArmSaveFileCancel => new ArmSaveFileCancelOperation(),
            ScenarioActions.ArmOpenFolder => CompileArmOpenFolder(step, index),
            ScenarioActions.ArmOpenFolderCancel => new ArmOpenFolderCancelOperation(),
            ScenarioActions.ArmMessageBox => CompileArmMessageBox(step, index),
            ScenarioActions.ListWindows => new ListWindowsOperation(),
            ScenarioActions.SwitchWindow => CompileSwitchWindow(step, index),
            ScenarioActions.WaitForWindow => CompileWaitForWindow(step, index),
            ScenarioActions.WaitForWindowClosed => CompileWaitForWindowClosed(step, index),
            ScenarioActions.InvokeOpeningWindow => CompileInvokeOpeningWindow(step, index),
            _ => throw Invalid($"steps[{index}] has unknown action '{action}'."),
        };
    }

    private static LaunchOperation CompileLaunch(JsonElement step, int index)
    {
        var appPath = RequireNonEmptyString(step, "appPath", index);
        string? configuration = null;
        if (step.TryGetProperty("configuration", out _))
        {
            configuration = RequireNonEmptyString(step, "configuration", index);
        }

        TimeSpan? timeout = null;
        if (step.TryGetProperty("timeoutSeconds", out var timeoutElement))
        {
            if (timeoutElement.ValueKind != JsonValueKind.Number)
            {
                throw Invalid($"steps[{index}].timeoutSeconds must be a number.");
            }

            var seconds = timeoutElement.GetDouble();
            if (seconds <= 0)
            {
                throw Invalid($"steps[{index}].timeoutSeconds must be positive.");
            }

            timeout = TimeSpan.FromSeconds(seconds);
        }

        return new LaunchOperation(appPath, configuration, timeout);
    }

    private static InvokeOperation CompileInvoke(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static RightClickOperation CompileRightClick(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static DoubleClickOperation CompileDoubleClick(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static HoverOperation CompileHover(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static DragOperation CompileDrag(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var toAutomationId = RequireNonEmptyString(step, "toAutomationId", index);
        return new DragOperation(automationId, toAutomationId);
    }

    private static ClickAtOperation CompileClickAt(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        return new ClickAtOperation(
            automationId,
            RequireNumber(step, "offsetX", index),
            RequireNumber(step, "offsetY", index)
        );
    }

    private static WheelOperation CompileWheel(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        return new WheelOperation(automationId, RequireInt(step, "delta", index));
    }

    private static SetValueOperation CompileSetValue(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("value", out var valueElement)
            || valueElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] setValue requires string property 'value'.");
        }

        return new SetValueOperation(automationId, valueElement.GetString() ?? string.Empty);
    }

    private static ToggleOperation CompileToggle(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static SendKeysOperation CompileSendKeys(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("text", out var textElement)
            || textElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] sendKeys requires string property 'text'.");
        }

        return new SendKeysOperation(automationId, textElement.GetString() ?? string.Empty);
    }

    private static PressKeysOperation CompilePressKeys(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var keys = RequireNonEmptyString(step, "keys", index);
        try
        {
            _ = KeyChordParser.Parse(keys);
        }
        catch (ArgumentException ex)
        {
            throw Invalid($"steps[{index}] pressKeys has invalid keys: {ex.Message}");
        }

        return new PressKeysOperation(automationId, keys);
    }

    private static ScreenshotOperation CompileScreenshot(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "path", index));

    private static ScrollIntoViewOperation CompileScrollIntoView(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        int? itemIndex = null;
        if (step.TryGetProperty("index", out var indexElement))
        {
            if (
                indexElement.ValueKind != JsonValueKind.Number
                || !indexElement.TryGetInt32(out var i)
            )
            {
                throw Invalid($"steps[{index}] scrollIntoView.index must be an integer.");
            }

            itemIndex = i;
        }

        return new ScrollIntoViewOperation(automationId, itemIndex);
    }

    private static SelectOperation CompileSelect(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var hasIndex = false;
        var itemIndex = 0;
        if (
            step.TryGetProperty("index", out var indexElement)
            && indexElement.ValueKind == JsonValueKind.Number
            && indexElement.TryGetInt32(out itemIndex)
        )
        {
            hasIndex = true;
        }

        string? key = null;
        var hasKey =
            step.TryGetProperty("key", out var keyElement)
            && keyElement.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(key = keyElement.GetString());

        if (hasIndex == hasKey)
        {
            throw Invalid($"steps[{index}] select requires exactly one of 'index' or 'key'.");
        }

        return hasIndex
            ? new SelectOperation(automationId, Index: itemIndex)
            : new SelectOperation(automationId, Key: key);
    }

    private static SelectMenuOperation CompileSelectMenu(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var path = RequireNonEmptyString(step, "path", index);
        return new SelectMenuOperation(automationId, path);
    }

    private static SelectTreeOperation CompileSelectTree(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var path = RequireNonEmptyString(step, "path", index);
        return new SelectTreeOperation(automationId, path);
    }

    private static SelectManyOperation CompileSelectMany(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("indexes", out var indexesElement)
            || indexesElement.ValueKind != JsonValueKind.Array
        )
        {
            throw Invalid($"steps[{index}] selectMany requires array property 'indexes'.");
        }

        var indexes = new List<int>(indexesElement.GetArrayLength());
        foreach (var entry in indexesElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Number || !entry.TryGetInt32(out var itemIndex))
            {
                throw Invalid($"steps[{index}] selectMany.indexes must be an array of integers.");
            }

            indexes.Add(itemIndex);
        }

        return new SelectManyOperation(automationId, indexes);
    }

    private static ExpandOperation CompileExpand(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static CollapseOperation CompileCollapse(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static ExpectNameOperation CompileExpectName(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] expectName requires string property 'name'.");
        }

        return new ExpectNameOperation(automationId, nameElement.GetString() ?? string.Empty);
    }

    private static ExpectSelectedOperation CompileExpectSelected(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        return new ExpectSelectedOperation(automationId, RequireBoolean(step, "selected", index));
    }

    private static ExpectExpandedOperation CompileExpectExpanded(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        return new ExpectExpandedOperation(automationId, RequireBoolean(step, "expanded", index));
    }

    private static ExpectCheckedOperation CompileExpectChecked(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        return new ExpectCheckedOperation(automationId, RequireBoolean(step, "checked", index));
    }

    private static ExpectEnabledOperation CompileExpectEnabled(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        return new ExpectEnabledOperation(automationId, RequireBoolean(step, "enabled", index));
    }

    private static ExpectVisibleOperation CompileExpectVisible(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        return new ExpectVisibleOperation(automationId, RequireBoolean(step, "visible", index));
    }

    private static ExpectFocusedOperation CompileExpectFocused(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static ExpectNameContainsOperation CompileExpectNameContains(
        JsonElement step,
        int index
    )
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("substring", out var substringElement)
            || substringElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid(
                $"steps[{index}] expectNameContains requires string property 'substring'."
            );
        }

        var substring = substringElement.GetString();
        if (string.IsNullOrEmpty(substring))
        {
            throw Invalid($"steps[{index}] expectNameContains 'substring' must be non-empty.");
        }

        return new ExpectNameContainsOperation(automationId, substring);
    }

    private static ExpectNameMatchesOperation CompileExpectNameMatches(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("pattern", out var patternElement)
            || patternElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] expectNameMatches requires string property 'pattern'.");
        }

        var pattern = patternElement.GetString();
        if (string.IsNullOrEmpty(pattern))
        {
            throw Invalid($"steps[{index}] expectNameMatches 'pattern' must be non-empty.");
        }

        return new ExpectNameMatchesOperation(automationId, pattern);
    }

    private static ExpectValueOperation CompileExpectValue(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("value", out var valueElement)
            || valueElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] expectValue requires string property 'value'.");
        }

        return new ExpectValueOperation(automationId, valueElement.GetString() ?? string.Empty);
    }

    private static ExpectToolTipOperation CompileExpectToolTip(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("toolTip", out var toolTipElement)
            || toolTipElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] expectToolTip requires string property 'toolTip'.");
        }

        return new ExpectToolTipOperation(automationId, toolTipElement.GetString() ?? string.Empty);
    }

    private static WaitForOperation CompileWaitFor(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static ExpectGoneOperation CompileExpectGone(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static GetCellTextOperation CompileGetCellText(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var (column, columnKey) = RequireColumnOrColumnKey(step, index, "getCellText");
        return new GetCellTextOperation(
            automationId,
            RequireNonNegativeInt(step, "row", index),
            column,
            columnKey
        );
    }

    private static SetCellValueOperation CompileSetCellValue(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("value", out var valueElement)
            || valueElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] setCellValue requires string property 'value'.");
        }

        var (column, columnKey) = RequireColumnOrColumnKey(step, index, "setCellValue");
        return new SetCellValueOperation(
            automationId,
            RequireNonNegativeInt(step, "row", index),
            column,
            columnKey,
            valueElement.GetString() ?? string.Empty
        );
    }

    private static SelectCellOperation CompileSelectCell(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var (column, columnKey) = RequireColumnOrColumnKey(step, index, "selectCell");
        return new SelectCellOperation(
            automationId,
            RequireNonNegativeInt(step, "row", index),
            column,
            columnKey
        );
    }

    private static SelectRowOperation CompileSelectRow(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var columnKey = RequireNonEmptyString(step, "columnKey", index);
        if (
            !step.TryGetProperty("value", out var valueElement)
            || valueElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] selectRow requires string property 'value'.");
        }

        return new SelectRowOperation(
            automationId,
            columnKey,
            valueElement.GetString() ?? string.Empty
        );
    }

    private static ClickColumnHeaderOperation CompileClickColumnHeader(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var columnKey = RequireNonEmptyString(step, "columnKey", index);
        return new ClickColumnHeaderOperation(automationId, columnKey);
    }

    private static AddRowOperation CompileAddRow(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "automationId", index));

    private static DeleteSelectedRowsOperation CompileDeleteSelectedRows(
        JsonElement step,
        int index
    ) => new(RequireNonEmptyString(step, "automationId", index));

    private static ExpectCellTextOperation CompileExpectCellText(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        if (
            !step.TryGetProperty("text", out var textElement)
            || textElement.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] expectCellText requires string property 'text'.");
        }

        var (column, columnKey) = RequireColumnOrColumnKey(step, index, "expectCellText");
        return new ExpectCellTextOperation(
            automationId,
            RequireNonNegativeInt(step, "row", index),
            column,
            columnKey,
            textElement.GetString() ?? string.Empty
        );
    }

    private static (int? Column, string? ColumnKey) RequireColumnOrColumnKey(
        JsonElement step,
        int index,
        string action
    )
    {
        int? column = null;
        if (step.TryGetProperty("column", out var columnElement))
        {
            if (
                columnElement.ValueKind != JsonValueKind.Number
                || !columnElement.TryGetInt32(out var columnValue)
                || columnValue < 0
            )
            {
                throw Invalid($"steps[{index}] {action}.column must be a non-negative integer.");
            }

            column = columnValue;
        }

        string? columnKey = null;
        if (step.TryGetProperty("columnKey", out var columnKeyElement))
        {
            if (columnKeyElement.ValueKind != JsonValueKind.String)
            {
                throw Invalid($"steps[{index}] {action}.columnKey must be a string.");
            }

            columnKey = columnKeyElement.GetString();
        }

        var hasColumn = column is not null;
        var hasKey = !string.IsNullOrWhiteSpace(columnKey);
        if (hasColumn == hasKey)
        {
            throw Invalid(
                $"steps[{index}] {action} requires exactly one of 'column' or 'columnKey'."
            );
        }

        return (column, hasKey ? columnKey : null);
    }

    private static int RequireNonNegativeInt(JsonElement step, string propertyName, int index)
    {
        if (
            !step.TryGetProperty(propertyName, out var element)
            || element.ValueKind != JsonValueKind.Number
            || !element.TryGetInt32(out var value)
        )
        {
            throw Invalid($"steps[{index}] requires integer property '{propertyName}'.");
        }

        if (value < 0)
        {
            throw Invalid($"steps[{index}].{propertyName} must be >= 0.");
        }

        return value;
    }

    private static SwitchWindowOperation CompileSwitchWindow(JsonElement step, int index)
    {
        if (
            !step.TryGetProperty("windowId", out var windowIdElement)
            || windowIdElement.ValueKind != JsonValueKind.Number
            || !windowIdElement.TryGetInt32(out var windowId)
        )
        {
            throw Invalid($"steps[{index}] switchWindow requires integer property 'windowId'.");
        }

        return new SwitchWindowOperation(windowId);
    }

    private static WaitForWindowOperation CompileWaitForWindow(JsonElement step, int index)
    {
        string? title = null;
        string? automationId = null;
        if (step.TryGetProperty("title", out _))
        {
            title = RequireNonEmptyString(step, "title", index);
        }

        if (step.TryGetProperty("automationId", out _))
        {
            automationId = RequireNonEmptyString(step, "automationId", index);
        }

        if (title is null && automationId is null)
        {
            throw Invalid($"steps[{index}] waitForWindow requires 'title' and/or 'automationId'.");
        }

        var switchTo = true;
        if (step.TryGetProperty("switchTo", out var switchElement))
        {
            if (
                switchElement.ValueKind != JsonValueKind.True
                && switchElement.ValueKind != JsonValueKind.False
            )
            {
                throw Invalid($"steps[{index}].switchTo must be a boolean.");
            }

            switchTo = switchElement.GetBoolean();
        }

        return new WaitForWindowOperation(title, automationId, switchTo);
    }

    private static WaitForWindowClosedOperation CompileWaitForWindowClosed(
        JsonElement step,
        int index
    )
    {
        string? title = null;
        string? automationId = null;
        if (step.TryGetProperty("title", out _))
        {
            title = RequireNonEmptyString(step, "title", index);
        }

        if (step.TryGetProperty("automationId", out _))
        {
            automationId = RequireNonEmptyString(step, "automationId", index);
        }

        if (title is null && automationId is null)
        {
            throw Invalid(
                $"steps[{index}] waitForWindowClosed requires 'title' and/or 'automationId'."
            );
        }

        return new WaitForWindowClosedOperation(title, automationId);
    }

    private static ArmOpenFileOperation CompileArmOpenFile(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "path", index));

    private static ArmSaveFileOperation CompileArmSaveFile(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "path", index));

    private static ArmOpenFolderOperation CompileArmOpenFolder(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "path", index));

    private static ArmMessageBoxOperation CompileArmMessageBox(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "result", index));

    private static InvokeOpeningWindowOperation CompileInvokeOpeningWindow(
        JsonElement step,
        int index
    )
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        var waitForNewWindow = true;
        if (step.TryGetProperty("waitForNewWindow", out var waitElement))
        {
            if (
                waitElement.ValueKind != JsonValueKind.True
                && waitElement.ValueKind != JsonValueKind.False
            )
            {
                throw Invalid($"steps[{index}].waitForNewWindow must be a boolean.");
            }

            waitForNewWindow = waitElement.GetBoolean();
        }

        return new InvokeOpeningWindowOperation(automationId, waitForNewWindow);
    }

    private static bool RequireBoolean(JsonElement step, string propertyName, int index)
    {
        if (
            !step.TryGetProperty(propertyName, out var element)
            || (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False)
        )
        {
            throw Invalid($"steps[{index}] requires boolean property '{propertyName}'.");
        }

        return element.GetBoolean();
    }

    private static double RequireNumber(JsonElement step, string propertyName, int index)
    {
        if (
            !step.TryGetProperty(propertyName, out var element)
            || element.ValueKind != JsonValueKind.Number
            || !element.TryGetDouble(out var value)
        )
        {
            throw Invalid($"steps[{index}] requires number property '{propertyName}'.");
        }

        return value;
    }

    private static int RequireInt(JsonElement step, string propertyName, int index)
    {
        if (
            !step.TryGetProperty(propertyName, out var element)
            || !element.TryGetInt32(out var value)
        )
        {
            throw Invalid($"steps[{index}] requires integer property '{propertyName}'.");
        }

        return value;
    }

    private static string RequireNonEmptyString(JsonElement step, string propertyName, int index)
    {
        if (
            !step.TryGetProperty(propertyName, out var element)
            || element.ValueKind != JsonValueKind.String
        )
        {
            throw Invalid($"steps[{index}] requires string property '{propertyName}'.");
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid($"steps[{index}].{propertyName} must be non-empty.");
        }

        return value;
    }

    private static GraftException Invalid(string message) =>
        new(GraftErrorCodes.ActionFailed, message);
}
