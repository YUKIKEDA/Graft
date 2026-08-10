using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows;
using Graft.Instrumentation.Dialogs;
using HarmonyLib;

namespace Graft.Instrumentation.Wpf.Dialogs;

/// <summary>
/// Harmony prefix on <c>MessageBox.Show</c> overloads that consumes <see cref="MessageBoxArm"/>.
/// </summary>
internal static class MessageBoxPatch
{
    private static int _applied;

    /// <summary>
    /// Applies MessageBox.Show prefixes once per process.
    /// </summary>
    public static void Apply()
    {
        if (Interlocked.Exchange(ref _applied, 1) != 0)
        {
            return;
        }

        var harmony = new Harmony("Graft.Instrumentation.Wpf.MessageBox");
        var methods = typeof(MessageBox)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m =>
                m.Name == nameof(MessageBox.Show) && m.ReturnType == typeof(MessageBoxResult)
            )
            .ToArray();
        if (methods.Length == 0)
        {
            Interlocked.Exchange(ref _applied, 0);
            throw new InvalidOperationException(
                "Could not locate System.Windows.MessageBox.Show overloads for MessageBox seam."
            );
        }

        var prefix = new HarmonyMethod(
            typeof(MessageBoxPatch).GetMethod(
                nameof(Prefix),
                BindingFlags.Static | BindingFlags.NonPublic
            )!
        );
        foreach (var method in methods)
        {
            harmony.Patch(method, prefix: prefix);
        }
    }

    /// <summary>
    /// Harmony prefix: when a MessageBox arm is pending, stub the result.
    /// </summary>
    /// <param name="__result">Stubbed <see cref="MessageBoxResult"/> when skipping the original.</param>
    /// <returns>
    /// <see langword="false"/> to skip the original when an arm was consumed; otherwise
    /// <see langword="true"/> to show the real MessageBox.
    /// </returns>
    [SuppressMessage(
        "Style",
        "SA1313:Parameter names should begin with lower-case letter",
        Justification = "Harmony injects __result by conventional name."
    )]
    private static bool Prefix(ref MessageBoxResult __result)
    {
        if (!MessageBoxArm.TryConsume(out var resultName) || resultName is null)
        {
            return true;
        }

        if (!Enum.TryParse(resultName, ignoreCase: false, out MessageBoxResult parsed))
        {
            return true;
        }

        __result = parsed;
        return false;
    }
}
