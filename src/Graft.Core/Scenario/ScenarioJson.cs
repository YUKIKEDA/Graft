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
            ScenarioActions.SetValue => CompileSetValue(step, index),
            ScenarioActions.Toggle => CompileToggle(step, index),
            ScenarioActions.SendKeys => CompileSendKeys(step, index),
            ScenarioActions.ScrollIntoView => CompileScrollIntoView(step, index),
            ScenarioActions.Select => CompileSelect(step, index),
            ScenarioActions.Expand => CompileExpand(step, index),
            ScenarioActions.Collapse => CompileCollapse(step, index),
            ScenarioActions.ExpectName => CompileExpectName(step, index),
            ScenarioActions.ExpectSelected => CompileExpectSelected(step, index),
            ScenarioActions.ExpectExpanded => CompileExpectExpanded(step, index),
            ScenarioActions.ExpectChecked => CompileExpectChecked(step, index),
            ScenarioActions.GetCellText => CompileGetCellText(step, index),
            ScenarioActions.SetCellValue => CompileSetCellValue(step, index),
            ScenarioActions.ExpectCellText => CompileExpectCellText(step, index),
            ScenarioActions.ArmOpenFile => CompileArmOpenFile(step, index),
            ScenarioActions.ArmOpenFileCancel => new ArmOpenFileCancelOperation(),
            ScenarioActions.ArmSaveFile => CompileArmSaveFile(step, index),
            ScenarioActions.ArmSaveFileCancel => new ArmSaveFileCancelOperation(),
            ScenarioActions.ListWindows => new ListWindowsOperation(),
            ScenarioActions.SwitchWindow => CompileSwitchWindow(step, index),
            ScenarioActions.WaitForWindow => CompileWaitForWindow(step, index),
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
        if (
            !step.TryGetProperty("index", out var indexElement)
            || indexElement.ValueKind != JsonValueKind.Number
            || !indexElement.TryGetInt32(out var itemIndex)
        )
        {
            throw Invalid($"steps[{index}] select requires integer property 'index'.");
        }

        return new SelectOperation(automationId, itemIndex);
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

    private static GetCellTextOperation CompileGetCellText(JsonElement step, int index)
    {
        var automationId = RequireNonEmptyString(step, "automationId", index);
        return new GetCellTextOperation(
            automationId,
            RequireNonNegativeInt(step, "row", index),
            RequireNonNegativeInt(step, "column", index)
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

        return new SetCellValueOperation(
            automationId,
            RequireNonNegativeInt(step, "row", index),
            RequireNonNegativeInt(step, "column", index),
            valueElement.GetString() ?? string.Empty
        );
    }

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

        return new ExpectCellTextOperation(
            automationId,
            RequireNonNegativeInt(step, "row", index),
            RequireNonNegativeInt(step, "column", index),
            textElement.GetString() ?? string.Empty
        );
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

    private static ArmOpenFileOperation CompileArmOpenFile(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "path", index));

    private static ArmSaveFileOperation CompileArmSaveFile(JsonElement step, int index) =>
        new(RequireNonEmptyString(step, "path", index));

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
