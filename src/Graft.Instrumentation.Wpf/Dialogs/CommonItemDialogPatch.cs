using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Graft.Instrumentation.Dialogs;
using HarmonyLib;
using Microsoft.Win32;

namespace Graft.Instrumentation.Wpf.Dialogs;

/// <summary>
/// Harmony prefix on <c>CommonItemDialog.RunDialog</c> for OpenFile / SaveFile arms.
/// </summary>
/// <remarks>
/// On modern .NET, <see cref="OpenFileDialog"/> and <see cref="SaveFileDialog"/> implement
/// <c>RunDialog</c> via <c>Microsoft.Win32.CommonItemDialog</c>.
/// Patches that <c>bool</c>-returning method instead of <c>ShowDialog</c> (<c>bool?</c>)
/// to avoid Harmony ABI issues with nullable value-type returns.
/// </remarks>
internal static class CommonItemDialogPatch
{
    private static int _applied;

    /// <summary>
    /// Applies the RunDialog prefix once per process.
    /// </summary>
    public static void Apply()
    {
        if (Interlocked.Exchange(ref _applied, 1) != 0)
        {
            return;
        }

        var harmony = new Harmony("Graft.Instrumentation.Wpf.CommonItemDialog");
        var runDialog = ResolveRunDialogMethod();
        if (runDialog is null)
        {
            Interlocked.Exchange(ref _applied, 0);
            throw new InvalidOperationException(
                "Could not locate CommonItemDialog/FileDialog.RunDialog(IntPtr) for file dialog seam."
            );
        }

        harmony.Patch(
            runDialog,
            prefix: new HarmonyMethod(
                typeof(CommonItemDialogPatch).GetMethod(
                    nameof(Prefix),
                    BindingFlags.Static | BindingFlags.NonPublic
                )!
            )
        );
    }

    private static MethodInfo? ResolveRunDialogMethod()
    {
        var commonItemDialog = AccessTools.TypeByName("Microsoft.Win32.CommonItemDialog");
        if (commonItemDialog is not null)
        {
            var method = AccessTools.Method(commonItemDialog, "RunDialog", [typeof(IntPtr)]);
            if (method is not null)
            {
                return method;
            }
        }

        return AccessTools.Method(typeof(FileDialog), "RunDialog", [typeof(IntPtr)]);
    }

    /// <summary>
    /// Harmony prefix: when an Open/Save File arm is pending, stub the dialog result.
    /// </summary>
    /// <param name="__instance">File dialog instance.</param>
    /// <param name="__result">Stubbed RunDialog result when skipping the original.</param>
    /// <returns>
    /// <see langword="false"/> to skip the original when an arm was consumed; otherwise
    /// <see langword="true"/> to run the real dialog.
    /// </returns>
    [SuppressMessage(
        "Style",
        "SA1313:Parameter names should begin with lower-case letter",
        Justification = "Harmony injects __instance / __result by conventional names."
    )]
    private static bool Prefix(object __instance, ref bool __result)
    {
        if (__instance is OpenFileDialog open)
        {
            return TryApplyArm(open, OpenFileArm.TryConsume, ref __result);
        }

        if (__instance is SaveFileDialog save)
        {
            return TryApplyArm(save, SaveFileArm.TryConsume, ref __result);
        }

        return true;
    }

    private static bool TryApplyArm(FileDialog dialog, TryConsumeArm tryConsume, ref bool result)
    {
        if (!tryConsume(out var path, out var canceled))
        {
            return true;
        }

        if (canceled)
        {
            result = false;
            return false;
        }

        dialog.FileName = path!;
        result = true;
        return false;
    }

    private delegate bool TryConsumeArm(out string? path, out bool canceled);
}
