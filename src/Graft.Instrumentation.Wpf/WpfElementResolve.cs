using System.Windows;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Resolves a selector to a live WPF element and checks that it can be acted on.
/// </summary>
internal static class WpfElementResolve
{
    private const string MissingResolverMessage = "No element resolver is registered. Call WpfGraft.Use() before Agent.Start().";

    private const string FrameworkElementOperation = "Resolved target is not a FrameworkElement";

    /// <summary>
    /// Resolves <paramref name="selector"/> to an enabled and visible <see cref="FrameworkElement"/>.
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <returns>The element and its automation id.</returns>
    /// <exception cref="ElementActionException">No resolver is registered, the target is the wrong type, or the element is not actionable.</exception>
    internal static (FrameworkElement Element, string AutomationId) ResolveActionable(ElementSelector selector) =>
        ResolveActionable<FrameworkElement>(selector, FrameworkElementOperation);

    /// <summary>
    /// Resolves <paramref name="selector"/> to an enabled and visible <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Expected element type.</typeparam>
    /// <param name="selector">Element selector.</param>
    /// <param name="operation">Type-mismatch text before <c>(got …)</c>.</param>
    /// <param name="subject">Noun in the not-actionable message. <c>Element</c> or <c>DataGrid</c>.</param>
    /// <returns>The element and its automation id.</returns>
    /// <exception cref="ElementActionException">No resolver is registered, the target is the wrong type, or the element is not actionable.</exception>
    internal static (T Element, string AutomationId) ResolveActionable<T>(ElementSelector selector, string operation, string subject = "Element")
        where T : FrameworkElement
    {
        var (element, automationId) = Resolve<T>(selector, operation);
        RequireActionable(element, automationId, subject);
        return (element, automationId);
    }

    /// <summary>
    /// Resolves <paramref name="selector"/> to a <see cref="FrameworkElement"/> without an enabled or visible check.
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <returns>The element and its automation id.</returns>
    /// <exception cref="ElementActionException">No resolver is registered, or the target is not a <see cref="FrameworkElement"/>.</exception>
    internal static (FrameworkElement Element, string AutomationId) Resolve(ElementSelector selector) =>
        Resolve<FrameworkElement>(selector, FrameworkElementOperation);

    /// <summary>
    /// Resolves <paramref name="selector"/> to a <typeparamref name="T"/> without an enabled or visible check.
    /// </summary>
    /// <typeparam name="T">Expected element type.</typeparam>
    /// <param name="selector">Element selector.</param>
    /// <param name="operation">Type-mismatch text before <c>(got …)</c>.</param>
    /// <returns>The element and its automation id.</returns>
    /// <exception cref="ElementActionException">No resolver is registered, or the target is not a <typeparamref name="T"/>.</exception>
    internal static (T Element, string AutomationId) Resolve<T>(ElementSelector selector, string operation)
        where T : FrameworkElement
    {
        return Cast<T>(Lookup(selector), operation);
    }

    /// <summary>
    /// Returns the registered resolver's match for <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <returns>The resolved element.</returns>
    /// <exception cref="ElementActionException">No resolver is registered.</exception>
    internal static ResolvedElement Lookup(ElementSelector selector)
    {
        var resolver = AgentServices.ElementResolver ?? throw new ElementActionException(GraftErrorCodes.ActionFailed, MissingResolverMessage);
        return resolver.Resolve(selector);
    }

    /// <summary>
    /// Casts an already resolved target to an enabled and visible <see cref="FrameworkElement"/>.
    /// </summary>
    /// <param name="resolved">Result of <see cref="Lookup"/>.</param>
    /// <returns>The element and its automation id.</returns>
    /// <exception cref="ElementActionException">The target is not a <see cref="FrameworkElement"/>, or it is not actionable.</exception>
    internal static (FrameworkElement Element, string AutomationId) CastActionable(ResolvedElement resolved)
    {
        var (element, automationId) = Cast<FrameworkElement>(resolved, FrameworkElementOperation);
        RequireActionable(element, automationId);
        return (element, automationId);
    }

    /// <summary>
    /// Throws <see cref="GraftErrorCodes.ElementNotActionable"/> when <paramref name="element"/> is disabled or hidden.
    /// </summary>
    /// <param name="element">Resolved element.</param>
    /// <param name="automationId">Automation id placed in the message.</param>
    /// <param name="subject">Noun in the message. <c>Element</c> or <c>DataGrid</c>.</param>
    /// <exception cref="ElementActionException">The element is disabled or hidden.</exception>
    internal static void RequireActionable(FrameworkElement element, string automationId, string subject = "Element")
    {
        if (element.IsEnabled && element.IsVisible)
        {
            return;
        }

        throw new ElementActionException(
            GraftErrorCodes.ElementNotActionable,
            $"{subject} '{automationId}' is not actionable (enabled={element.IsEnabled}, visible={element.IsVisible})."
        );
    }

    private static (T Element, string AutomationId) Cast<T>(ResolvedElement resolved, string operation)
        where T : FrameworkElement
    {
        if (resolved.Target is not T element)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"{operation} (got {resolved.Target.GetType().Name}).");
        }

        return (element, resolved.AutomationId);
    }
}
