using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace SampleTodoApp.FlaUI.Tests;

/// <summary>
/// Automates Win32 common file dialogs hosted as <c>#32770</c> under the WPF owner window.
/// </summary>
internal static class Win32FileDialog
{
    public static void CompleteOpen(
        UIA3Automation automation,
        AutomationElement ownerWindow,
        string filePath,
        TimeSpan timeout
    ) => Complete(ownerWindow, filePath, isSave: false, timeout);

    public static void CompleteSave(
        UIA3Automation automation,
        AutomationElement ownerWindow,
        string filePath,
        TimeSpan timeout
    ) => Complete(ownerWindow, filePath, isSave: true, timeout);

    private static void Complete(
        AutomationElement ownerWindow,
        string filePath,
        bool isSave,
        TimeSpan timeout
    )
    {
        var dialog = WaitForFileDialog(ownerWindow, timeout);
        if (isSave)
        {
            CompleteSaveViaKeyboard(dialog, filePath);
        }
        else
        {
            CompleteOpenViaValuePattern(dialog, filePath);
        }
    }

    private static Window WaitForFileDialog(AutomationElement ownerWindow, TimeSpan timeout)
    {
        var dialog = Retry
            .WhileNull(
                () =>
                {
                    try
                    {
                        foreach (
                            var window in ownerWindow.FindAllDescendants(cf =>
                                cf.ByControlType(ControlType.Window)
                            )
                        )
                        {
                            var className = window.ClassName ?? string.Empty;
                            if (className.Contains("32770", StringComparison.Ordinal))
                            {
                                return window.AsWindow();
                            }
                        }
                    }
                    catch
                    {
                        // tree unstable while opening
                    }

                    return null;
                },
                timeout
            )
            .Result;

        return dialog
            ?? throw new TimeoutException(
                "File dialog (#32770) did not appear under the owner window."
            );
    }

    private static void CompleteOpenViaValuePattern(Window dialog, string filePath)
    {
        dialog.SetForeground();
        dialog.Focus();
        Thread.Sleep(200);

        var edit =
            SafeFind(
                dialog,
                cf => cf.ByControlType(ControlType.Edit).And(cf.ByName("ファイル名(N):"))
            )
            ?? SafeFind(
                dialog,
                cf => cf.ByControlType(ControlType.Edit).And(cf.ByName("File name:"))
            )
            ?? SafeFind(dialog, cf => cf.ByControlType(ControlType.Edit))
            ?? throw new InvalidOperationException("File-name Edit not found.");

        edit.Focus();
        if (!edit.Patterns.Value.IsSupported)
        {
            TypePathAndEnter(filePath);
            return;
        }

        edit.Patterns.Value.Pattern.SetValue(filePath);
        Thread.Sleep(200);

        var open =
            SafeFind(dialog, cf => cf.ByControlType(ControlType.Button).And(cf.ByName("開く(O)")))
            ?? SafeFind(dialog, cf => cf.ByControlType(ControlType.Button).And(cf.ByName("開く")))
            ?? SafeFind(dialog, cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Open")));

        if (open is not null)
        {
            open.AsButton().Invoke();
        }
        else
        {
            Keyboard.Type(VirtualKeyShort.ENTER);
        }

        Thread.Sleep(400);
    }

    private static void CompleteSaveViaKeyboard(Window dialog, string filePath)
    {
        // Save dialog filename Edit often has empty/odd UIA names; ValuePattern.SetValue
        // also times out frequently. Focus the dialog and type the full path + Enter.
        dialog.SetForeground();
        dialog.Focus();
        Thread.Sleep(400);
        TypePathAndEnter(filePath);
        Thread.Sleep(600);
    }

    private static void TypePathAndEnter(string filePath)
    {
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Thread.Sleep(50);
        Keyboard.Type(filePath);
        Thread.Sleep(100);
        Keyboard.Type(VirtualKeyShort.ENTER);
    }

    private static AutomationElement? SafeFind(
        AutomationElement root,
        Func<
            global::FlaUI.Core.Conditions.ConditionFactory,
            global::FlaUI.Core.Conditions.ConditionBase
        > condition
    )
    {
        try
        {
            return root.FindFirstDescendant(condition);
        }
        catch
        {
            return null;
        }
    }
}
