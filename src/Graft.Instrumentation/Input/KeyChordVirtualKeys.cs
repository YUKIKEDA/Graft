using Graft.Protocol;

namespace Graft.Instrumentation.Input;

#if GRAFT_TEST

/// <summary>
/// Maps <see cref="KeyChord"/> tokens to Win32 virtual-key codes.
/// </summary>
internal static class KeyChordVirtualKeys
{
    /// <summary>
    /// Resolves modifiers and key VKs for <paramref name="chord"/>.
    /// </summary>
    /// <param name="chord">Normalized chord.</param>
    /// <returns>Modifier VKs (press order), key VK, and whether the key is extended.</returns>
    public static (byte[] Modifiers, byte Key, bool Extended) Resolve(KeyChord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);

        var mods = new byte[chord.Modifiers.Count];
        for (var i = 0; i < chord.Modifiers.Count; i++)
        {
            mods[i] = chord.Modifiers[i] switch
            {
                "Control" => NativeMethods.VkControl,
                "Alt" => NativeMethods.VkMenu,
                "Shift" => NativeMethods.VkShift,
                _ => throw new ArgumentException(
                    $"Unsupported modifier '{chord.Modifiers[i]}'.",
                    nameof(chord)
                ),
            };
        }

        var (key, extended) = ResolveKey(chord.Key);
        return (mods, key, extended);
    }

    private static (byte Vk, bool Extended) ResolveKey(string key) =>
        key switch
        {
            { Length: 1 } when key[0] is >= 'A' and <= 'Z' => ((byte)key[0], false),
            { Length: 1 } when key[0] is >= '0' and <= '9' => ((byte)key[0], false),
            "Enter" => (NativeMethods.VkReturn, false),
            "Tab" => (NativeMethods.VkTab, false),
            "Escape" => (NativeMethods.VkEscape, false),
            "Backspace" => (NativeMethods.VkBack, false),
            "Delete" => (NativeMethods.VkDelete, true),
            "Space" => (NativeMethods.VkSpace, false),
            "ArrowLeft" => (NativeMethods.VkLeft, true),
            "ArrowUp" => (NativeMethods.VkUp, true),
            "ArrowRight" => (NativeMethods.VkRight, true),
            "ArrowDown" => (NativeMethods.VkDown, true),
            _ => throw new ArgumentException($"Unsupported key '{key}'.", nameof(key)),
        };
}

#endif
