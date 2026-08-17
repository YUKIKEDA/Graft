namespace Graft.Protocol;

/// <summary>
/// Parses Playwright-like chord strings (<c>Control+A</c>, <c>Enter</c>) into <see cref="KeyChord"/>.
/// One string = one chord; sequences require multiple calls.
/// </summary>
public static class KeyChordParser
{
    private static readonly Dictionary<string, string> CanonicalKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = "Enter",
        ["Tab"] = "Tab",
        ["Escape"] = "Escape",
        ["Backspace"] = "Backspace",
        ["Delete"] = "Delete",
        ["Space"] = "Space",
        ["ArrowUp"] = "ArrowUp",
        ["ArrowDown"] = "ArrowDown",
        ["ArrowLeft"] = "ArrowLeft",
        ["ArrowRight"] = "ArrowRight",
        ["F1"] = "F1",
        ["F2"] = "F2",
        ["F3"] = "F3",
        ["F4"] = "F4",
        ["F5"] = "F5",
        ["F6"] = "F6",
        ["F7"] = "F7",
        ["F8"] = "F8",
        ["F9"] = "F9",
        ["F10"] = "F10",
        ["F11"] = "F11",
        ["F12"] = "F12",
        ["NumPad0"] = "NumPad0",
        ["NumPad1"] = "NumPad1",
        ["NumPad2"] = "NumPad2",
        ["NumPad3"] = "NumPad3",
        ["NumPad4"] = "NumPad4",
        ["NumPad5"] = "NumPad5",
        ["NumPad6"] = "NumPad6",
        ["NumPad7"] = "NumPad7",
        ["NumPad8"] = "NumPad8",
        ["NumPad9"] = "NumPad9",
        ["NumPadAdd"] = "NumPadAdd",
        ["NumPadSubtract"] = "NumPadSubtract",
        ["NumPadDecimal"] = "NumPadDecimal",
        ["NumPadEnter"] = "NumPadEnter",
    };

    /// <summary>
    /// Parses <paramref name="keys"/> into a single chord.
    /// </summary>
    /// <param name="keys">Chord DSL (e.g. <c>Control+A</c>, <c>Shift+Tab</c>, <c>Delete</c>).</param>
    /// <returns>Normalized chord.</returns>
    /// <exception cref="ArgumentException">Empty, unknown token, or more than one non-modifier key.</exception>
    public static KeyChord Parse(string keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (string.IsNullOrWhiteSpace(keys))
        {
            throw new ArgumentException("keys must be a non-empty chord string.", nameof(keys));
        }

        var parts = keys.Split('+', StringSplitOptions.None);
        if (parts.Length == 0)
        {
            throw new ArgumentException("keys must be a non-empty chord string.", nameof(keys));
        }

        var modifiers = new List<string>();
        string? key = null;

        for (var i = 0; i < parts.Length; i++)
        {
            var raw = parts[i].Trim();
            if (raw.Length == 0)
            {
                throw new ArgumentException($"Invalid chord '{keys}': empty token between '+' separators.", nameof(keys));
            }

            if (TryCanonicalModifier(raw, out var modifier))
            {
                if (key is not null)
                {
                    throw new ArgumentException($"Invalid chord '{keys}': modifier '{raw}' cannot follow the key.", nameof(keys));
                }

                if (modifiers.Contains(modifier, StringComparer.Ordinal))
                {
                    throw new ArgumentException($"Invalid chord '{keys}': duplicate modifier '{modifier}'.", nameof(keys));
                }

                modifiers.Add(modifier);
                continue;
            }

            if (!TryCanonicalKey(raw, out var canonicalKey))
            {
                throw new ArgumentException($"Invalid chord '{keys}': unknown key '{raw}'.", nameof(keys));
            }

            if (key is not null)
            {
                throw new ArgumentException($"Invalid chord '{keys}': only one non-modifier key is allowed per call.", nameof(keys));
            }

            key = canonicalKey;
        }

        if (key is null)
        {
            throw new ArgumentException($"Invalid chord '{keys}': missing key (modifiers alone are not allowed).", nameof(keys));
        }

        return new KeyChord(modifiers, key);
    }

    private static bool TryCanonicalModifier(string token, out string canonical)
    {
        if (token.Equals("Control", StringComparison.OrdinalIgnoreCase))
        {
            canonical = "Control";
            return true;
        }

        if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
        {
            canonical = "Alt";
            return true;
        }

        if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
        {
            canonical = "Shift";
            return true;
        }

        canonical = string.Empty;
        return false;
    }

    private static bool TryCanonicalKey(string token, out string canonical)
    {
        if (token.Length == 1)
        {
            var ch = token[0];
            if (ch is >= 'a' and <= 'z')
            {
                canonical = char.ToUpperInvariant(ch).ToString();
                return true;
            }

            if (ch is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                canonical = ch.ToString();
                return true;
            }
        }

        return CanonicalKeys.TryGetValue(token, out canonical!);
    }
}
