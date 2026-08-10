using System.Runtime.InteropServices;

namespace Graft.Instrumentation.Input;

#if GRAFT_TEST

internal static partial class NativeMethods
{
    public const uint InputMouse = 0;
    public const uint InputKeyboard = 1;

    public const uint MouseEventFMove = 0x0001;
    public const uint MouseEventFLeftDown = 0x0002;
    public const uint MouseEventFLeftUp = 0x0004;
    public const uint MouseEventFRightDown = 0x0008;
    public const uint MouseEventFRightUp = 0x0010;
    public const uint MouseEventFWheel = 0x0800;
    public const uint MouseEventFAbsolute = 0x8000;

    /// <summary>Standard Win32 wheel notch (positive = away from user).</summary>
    public const int WheelDelta = 120;

    public const uint KeyEventFExtendedKey = 0x0001;
    public const uint KeyEventFKeyUp = 0x0002;
    public const uint KeyEventFUnicode = 0x0004;

    public const uint MapVkToVsc = 0;

    public const byte VkBack = 0x08;
    public const byte VkTab = 0x09;
    public const byte VkReturn = 0x0D;
    public const byte VkShift = 0x10;
    public const byte VkControl = 0x11;
    public const byte VkMenu = 0x12;
    public const byte VkEscape = 0x1B;
    public const byte VkSpace = 0x20;
    public const byte VkLeft = 0x25;
    public const byte VkUp = 0x26;
    public const byte VkRight = 0x27;
    public const byte VkDown = 0x28;
    public const byte VkDelete = 0x2E;
    public const byte VkA = 0x41;

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetrics(int nIndex);

    public const int SmCxScreen = 0;
    public const int SmCyScreen = 1;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetCursorPos(int x, int y);

    [LibraryImport("user32.dll")]
    public static partial uint MapVirtualKey(uint uCode, uint uMapType);

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT Mouse;

        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }
}

#endif
