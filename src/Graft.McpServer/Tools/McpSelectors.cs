using Graft.Core;
using Graft.Core.Selectors;
using Graft.Protocol;

namespace Graft.McpServer.Tools;

/// <summary>
/// Resolves the short automation id and the nested selector on an MCP tool.
/// </summary>
internal static class McpSelectors
{
    /// <summary>
    /// Returns the selector from exactly one of <paramref name="automationId"/> or <paramref name="target"/>.
    /// </summary>
    /// <param name="automationId">Short-form automation id.</param>
    /// <param name="target">Nested public selector.</param>
    /// <returns>The selector to resolve.</returns>
    public static Selector Require(string? automationId, Selector? target) => RequireSide(automationId, target, "automationId", "target");

    /// <summary>
    /// Returns the selector from exactly one side, or <see langword="null"/> when both are omitted.
    /// </summary>
    /// <param name="automationId">Short-form automation id.</param>
    /// <param name="target">Nested public selector.</param>
    /// <returns>The selector, or <see langword="null"/> when the call clips the window.</returns>
    public static Selector? Optional(string? automationId, Selector? target)
    {
        if (string.IsNullOrWhiteSpace(automationId) && target is null)
        {
            return null;
        }

        return Require(automationId, target);
    }

    /// <summary>
    /// Returns the selector from exactly one of the named short form or object.
    /// </summary>
    /// <param name="automationId">Short-form automation id.</param>
    /// <param name="target">Nested public selector.</param>
    /// <param name="idName">Short-form argument name.</param>
    /// <param name="objectName">Object argument name.</param>
    /// <returns>The selector to resolve.</returns>
    public static Selector RequireSide(string? automationId, Selector? target, string idName, string objectName)
    {
        var hasId = !string.IsNullOrWhiteSpace(automationId);
        var hasTarget = target is not null;
        if (hasId && hasTarget)
        {
            throw new GraftException(GraftErrorCodes.SelectorInvalid, $"Set {idName} or {objectName}, not both.");
        }

        if (!hasId && !hasTarget)
        {
            throw new GraftException(GraftErrorCodes.SelectorInvalid, $"Set {idName} or {objectName}.");
        }

        if (hasId)
        {
            return Selector.ByAutomationId(automationId!);
        }

        return RequireObject(target!);
    }

    private static Selector RequireObject(Selector target)
    {
        if (target.Nth is < 0)
        {
            throw new GraftException(GraftErrorCodes.SelectorInvalid, "Selector nth must be >= 0.");
        }

        var hasString =
            !string.IsNullOrWhiteSpace(target.AutomationId)
            || !string.IsNullOrWhiteSpace(target.Name)
            || !string.IsNullOrWhiteSpace(target.ControlType)
            || !string.IsNullOrWhiteSpace(target.NearAutomationId);
        if (!hasString)
        {
            throw new GraftException(GraftErrorCodes.SelectorInvalid, "Selector must set automationId, name, controlType, or nearAutomationId.");
        }

        return target;
    }
}
