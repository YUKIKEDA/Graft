using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Scrolls WPF elements / list items into view on the UI dispatcher.
/// </summary>
internal sealed class WpfElementScroller : IElementScroller
{
    /// <inheritdoc />
    public ElementIdentity ScrollIntoView(ElementSelector selector, int? index)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot scrollIntoView."
            );
        }

        if (dispatcher.CheckAccess())
        {
            return ScrollOnUiThread(selector, index);
        }

        return dispatcher.Invoke(
            () => ScrollOnUiThread(selector, index),
            DispatcherPriority.Normal
        );
    }

    /// <summary>
    /// Realizes and scrolls a list/combo item (shared with <see cref="WpfElementChooser"/>).
    /// </summary>
    internal static ElementIdentity ScrollListItem(FrameworkElement listElement, int index)
    {
        if (index < 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.SelectorInvalid,
                "params.index must be >= 0."
            );
        }

        // ListView derives from ListBox — one arm covers both.
        return listElement switch
        {
            ListBox listBox => ScrollWithListBoxApi(listBox, index),
            ComboBox comboBox => ScrollComboBox(comboBox, index),
            ItemsControl itemsControl => ScrollGenericItemsControl(itemsControl, index),
            _ => throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"scrollIntoView(index) is not supported for control type '{listElement.GetType().Name}'."
            ),
        };
    }

    /// <summary>
    /// Ensures the container has an automation id (assigns graft-item-N when missing).
    /// </summary>
    internal static ElementIdentity EnsureIdentity(FrameworkElement element, int index)
    {
        var automationId = AutomationProperties.GetAutomationId(element);
        if (string.IsNullOrWhiteSpace(automationId))
        {
            automationId = $"graft-item-{index}";
            AutomationProperties.SetAutomationId(element, automationId);
        }

        return ToIdentity(element, automationId);
    }

    private static ElementIdentity ScrollOnUiThread(ElementSelector selector, int? index)
    {
        var resolver =
            AgentServices.ElementResolver
            ?? throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "No element resolver is registered. Call WpfGraft.Use() before Agent.Start()."
            );

        var resolved = resolver.Resolve(selector);
        if (resolved.Target is not FrameworkElement element)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Resolved target is not a FrameworkElement (got {resolved.Target.GetType().Name})."
            );
        }

        if (index is null)
        {
            element.BringIntoView();
            element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
            return ToIdentity(element, resolved.AutomationId);
        }

        return ScrollListItem(element, index.Value);
    }

    private static ElementIdentity ScrollWithListBoxApi(ListBox listBox, int index)
    {
        EnsureIndexInRange(listBox, index);
        var item = listBox.Items[index]!;
        listBox.ScrollIntoView(item);
        listBox.UpdateLayout();
        listBox.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);

        var container =
            listBox.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
        if (container is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Failed to realize list item at index {index}."
            );
        }

        container.BringIntoView();
        container.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
        return EnsureIdentity(container, index);
    }

    private static ElementIdentity ScrollComboBox(ComboBox comboBox, int index)
    {
        EnsureIndexInRange(comboBox, index);
        var wasOpen = comboBox.IsDropDownOpen;
        comboBox.IsDropDownOpen = true;
        comboBox.UpdateLayout();
        comboBox.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);

        try
        {
            var container =
                comboBox.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
            if (container is null)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"Failed to realize ComboBox item at index {index}."
                );
            }

            container.BringIntoView();
            container.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
            return EnsureIdentity(container, index);
        }
        finally
        {
            comboBox.IsDropDownOpen = wasOpen;
        }
    }

    private static ElementIdentity ScrollGenericItemsControl(ItemsControl itemsControl, int index)
    {
        EnsureIndexInRange(itemsControl, index);
        if (
            itemsControl.ItemContainerGenerator.ContainerFromIndex(index)
            is FrameworkElement existing
        )
        {
            existing.BringIntoView();
            existing.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
            return EnsureIdentity(existing, index);
        }

        throw new ElementActionException(
            GraftErrorCodes.ActionFailed,
            $"Failed to realize items-control item at index {index} (control type '{itemsControl.GetType().Name}')."
        );
    }

    private static void EnsureIndexInRange(ItemsControl itemsControl, int index)
    {
        if (index >= itemsControl.Items.Count)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotFound,
                $"Item index {index} is out of range (count={itemsControl.Items.Count})."
            );
        }
    }

    private static ElementIdentity ToIdentity(
        FrameworkElement element,
        string? fallbackAutomationId
    )
    {
        var automationId = AutomationProperties.GetAutomationId(element);
        if (string.IsNullOrWhiteSpace(automationId))
        {
            automationId = fallbackAutomationId;
        }

        if (string.IsNullOrWhiteSpace(automationId))
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "Scrolled element has no automationId; cannot return identity."
            );
        }

        return new ElementIdentity
        {
            AutomationId = automationId,
            RuntimeId = element.GetHashCode(),
        };
    }
}
