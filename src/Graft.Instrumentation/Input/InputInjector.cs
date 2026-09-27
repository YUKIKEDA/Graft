using System.Runtime.InteropServices;
using Graft.Instrumentation.Actions;
using Graft.Protocol;

namespace Graft.Instrumentation.Input;

#if GRAFT_TEST

/// <summary>
/// Win32 <c>SendInput</c> helpers for mouse click and Unicode text (agent-side).
/// </summary>
public static class InputInjector
{
    /// <summary>
    /// Attempts to bring <paramref name="windowHandle"/> to the foreground.
    /// </summary>
    /// <param name="windowHandle">HWND.</param>
    public static void SetForegroundWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.SetForegroundWindow(windowHandle);
    }

    /// <summary>
    /// Moves the cursor to screen coordinates and performs a left click.
    /// </summary>
    /// <param name="screenX">Screen X in pixels.</param>
    /// <param name="screenY">Screen Y in pixels.</param>
    public static void LeftClick(int screenX, int screenY) =>
        Click(screenX, screenY, NativeMethods.MouseEventFLeftDown, NativeMethods.MouseEventFLeftUp);

    /// <summary>
    /// Moves the cursor to screen coordinates and performs a right click.
    /// </summary>
    /// <param name="screenX">Screen X in pixels.</param>
    /// <param name="screenY">Screen Y in pixels.</param>
    public static void RightClick(int screenX, int screenY) =>
        Click(screenX, screenY, NativeMethods.MouseEventFRightDown, NativeMethods.MouseEventFRightUp);

    /// <summary>
    /// Moves the cursor to screen coordinates and performs a left double-click.
    /// </summary>
    /// <param name="screenX">Screen X in pixels.</param>
    /// <param name="screenY">Screen Y in pixels.</param>
    public static void DoubleClick(int screenX, int screenY)
    {
        if (!NativeMethods.SetCursorPos(screenX, screenY))
        {
            throw CreateFailed($"SetCursorPos failed at ({screenX},{screenY}).");
        }

        var (absX, absY) = ToAbsolute(screenX, screenY);
        const uint moveAbsolute = NativeMethods.MouseEventFMove | NativeMethods.MouseEventFAbsolute | NativeMethods.MouseEventFVirtualDesk;
        Send(
            [
                CreateMouse(absX, absY, moveAbsolute | NativeMethods.MouseEventFLeftDown),
                CreateMouse(absX, absY, moveAbsolute | NativeMethods.MouseEventFLeftUp),
                CreateMouse(absX, absY, moveAbsolute | NativeMethods.MouseEventFLeftDown),
                CreateMouse(absX, absY, moveAbsolute | NativeMethods.MouseEventFLeftUp),
            ]
        );
    }

    /// <summary>
    /// Moves the cursor to screen coordinates without clicking.
    /// </summary>
    /// <param name="screenX">Screen X in pixels.</param>
    /// <param name="screenY">Screen Y in pixels.</param>
    public static void MoveTo(int screenX, int screenY)
    {
        if (!NativeMethods.SetCursorPos(screenX, screenY))
        {
            throw CreateFailed($"SetCursorPos failed at ({screenX},{screenY}).");
        }

        var (absX, absY) = ToAbsolute(screenX, screenY);
        const uint moveAbsolute = NativeMethods.MouseEventFMove | NativeMethods.MouseEventFAbsolute | NativeMethods.MouseEventFVirtualDesk;
        Send([CreateMouse(absX, absY, moveAbsolute)]);
    }

    /// <summary>
    /// Drags with the left button from one screen point to another.
    /// </summary>
    /// <param name="fromScreenX">Start screen X.</param>
    /// <param name="fromScreenY">Start screen Y.</param>
    /// <param name="toScreenX">End screen X.</param>
    /// <param name="toScreenY">End screen Y.</param>
    public static void Drag(int fromScreenX, int fromScreenY, int toScreenX, int toScreenY)
    {
        MoveTo(fromScreenX, fromScreenY);
        var (fromAbsX, fromAbsY) = ToAbsolute(fromScreenX, fromScreenY);
        var (toAbsX, toAbsY) = ToAbsolute(toScreenX, toScreenY);
        const uint moveAbsolute = NativeMethods.MouseEventFMove | NativeMethods.MouseEventFAbsolute | NativeMethods.MouseEventFVirtualDesk;

        Send([CreateMouse(fromAbsX, fromAbsY, moveAbsolute | NativeMethods.MouseEventFLeftDown)]);
        Thread.Sleep(30);
        Send([CreateMouse(toAbsX, toAbsY, moveAbsolute)]);
        Thread.Sleep(30);
        Send([CreateMouse(toAbsX, toAbsY, moveAbsolute | NativeMethods.MouseEventFLeftUp)]);
    }

    /// <summary>
    /// Moves to screen coordinates and scrolls the mouse wheel by <paramref name="delta"/>.
    /// </summary>
    /// <param name="screenX">Screen X in pixels.</param>
    /// <param name="screenY">Screen Y in pixels.</param>
    /// <param name="delta">Wheel delta (typically multiples of 120; positive = away from user).</param>
    public static void Wheel(int screenX, int screenY, int delta)
    {
        MoveTo(screenX, screenY);
        var (absX, absY) = ToAbsolute(screenX, screenY);
        const uint moveAbsolute = NativeMethods.MouseEventFMove | NativeMethods.MouseEventFAbsolute | NativeMethods.MouseEventFVirtualDesk;
        Send([CreateMouse(absX, absY, moveAbsolute), CreateMouseWheel(absX, absY, delta)]);
    }

    /// <summary>
    /// Types <paramref name="text"/> via Unicode key events (no chord DSL).
    /// </summary>
    /// <param name="text">Literal text to type (may be empty).</param>
    public static void TypeText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return;
        }

        var inputs = new NativeMethods.INPUT[text.Length * 2];
        var index = 0;
        foreach (var ch in text)
        {
            inputs[index++] = CreateUnicodeKey(ch, keyUp: false);
            inputs[index++] = CreateUnicodeKey(ch, keyUp: true);
        }

        Send(inputs);
    }

    /// <summary>
    /// Sends Ctrl+A then Delete (select-all + clear) via virtual keys.
    /// </summary>
    public static void SelectAllAndDelete()
    {
        PressChord(KeyChordParser.Parse("Control+A"));
        PressChord(KeyChordParser.Parse("Delete"));
    }

    /// <summary>
    /// Sends one keyboard chord (modifiers down → key → modifiers up).
    /// </summary>
    /// <param name="chord">Normalized chord.</param>
    public static void PressChord(KeyChord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);
        var (mods, key, extended) = KeyChordVirtualKeys.Resolve(chord);
        var inputs = new NativeMethods.INPUT[(mods.Length * 2) + 2];
        var index = 0;
        foreach (var mod in mods)
        {
            inputs[index++] = CreateVk(mod, keyUp: false, extended: false);
        }

        inputs[index++] = CreateVk(key, keyUp: false, extended);
        inputs[index++] = CreateVk(key, keyUp: true, extended);

        for (var i = mods.Length - 1; i >= 0; i--)
        {
            inputs[index++] = CreateVk(mods[i], keyUp: true, extended: false);
        }

        Send(inputs);
    }

    private static void Click(int screenX, int screenY, uint downFlag, uint upFlag)
    {
        if (!NativeMethods.SetCursorPos(screenX, screenY))
        {
            throw CreateFailed($"SetCursorPos failed at ({screenX},{screenY}).");
        }

        var (absX, absY) = ToAbsolute(screenX, screenY);

        const uint moveAbsolute = NativeMethods.MouseEventFMove | NativeMethods.MouseEventFAbsolute | NativeMethods.MouseEventFVirtualDesk;
        var inputs = new NativeMethods.INPUT[] { CreateMouse(absX, absY, moveAbsolute | downFlag), CreateMouse(absX, absY, moveAbsolute | upFlag) };

        Send(inputs);
    }

    private static (int AbsX, int AbsY) ToAbsolute(int screenX, int screenY) =>
        NormalizeToVirtualDesktop(
            screenX,
            screenY,
            NativeMethods.GetSystemMetrics(NativeMethods.SmXVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmYVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmCxVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmCyVirtualScreen)
        );

    /// <summary>
    /// Maps a screen pixel to the 0..65535 range used by <c>MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK</c>,
    /// relative to the whole virtual desktop (secondary monitors and negative origins included).
    /// </summary>
    internal static (int AbsX, int AbsY) NormalizeToVirtualDesktop(int screenX, int screenY, int left, int top, int width, int height)
    {
        var spanX = Math.Max(1, width - 1);
        var spanY = Math.Max(1, height - 1);
        var absX = (int)Math.Round((screenX - left) * 65535.0 / spanX);
        var absY = (int)Math.Round((screenY - top) * 65535.0 / spanY);
        return (Math.Clamp(absX, 0, 65535), Math.Clamp(absY, 0, 65535));
    }

    private static void Send(NativeMethods.INPUT[] inputs)
    {
        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        if (sent != inputs.Length)
        {
            throw CreateFailed($"SendInput injected {sent}/{inputs.Length} events (Win32={Marshal.GetLastWin32Error()}).");
        }
    }

    private static NativeMethods.INPUT CreateMouse(int absX, int absY, uint flags) =>
        new()
        {
            Type = NativeMethods.InputMouse,
            Data = new NativeMethods.InputUnion
            {
                Mouse = new NativeMethods.MOUSEINPUT
                {
                    Dx = absX,
                    Dy = absY,
                    DwFlags = flags,
                },
            },
        };

    private static NativeMethods.INPUT CreateMouseWheel(int absX, int absY, int delta) =>
        new()
        {
            Type = NativeMethods.InputMouse,
            Data = new NativeMethods.InputUnion
            {
                Mouse = new NativeMethods.MOUSEINPUT
                {
                    Dx = absX,
                    Dy = absY,
                    MouseData = unchecked((uint)delta),
                    DwFlags =
                        NativeMethods.MouseEventFWheel
                        | NativeMethods.MouseEventFMove
                        | NativeMethods.MouseEventFAbsolute
                        | NativeMethods.MouseEventFVirtualDesk,
                },
            },
        };

    private static NativeMethods.INPUT CreateUnicodeKey(char ch, bool keyUp) =>
        new()
        {
            Type = NativeMethods.InputKeyboard,
            Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KEYBDINPUT
                {
                    WVk = 0,
                    WScan = ch,
                    DwFlags = NativeMethods.KeyEventFUnicode | (keyUp ? NativeMethods.KeyEventFKeyUp : 0),
                },
            },
        };

    private static NativeMethods.INPUT CreateVk(byte vk, bool keyUp, bool extended)
    {
        var scan = (ushort)NativeMethods.MapVirtualKey(vk, NativeMethods.MapVkToVsc);
        var flags = keyUp ? NativeMethods.KeyEventFKeyUp : 0u;
        if (extended)
        {
            flags |= NativeMethods.KeyEventFExtendedKey;
        }

        return new NativeMethods.INPUT
        {
            Type = NativeMethods.InputKeyboard,
            Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KEYBDINPUT
                {
                    WVk = vk,
                    WScan = scan,
                    DwFlags = flags,
                },
            },
        };
    }

    private static ElementActionException CreateFailed(string message) => new(GraftErrorCodes.ActionFailed, message);
}

#endif
