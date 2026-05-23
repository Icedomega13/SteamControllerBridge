using System.Runtime.InteropServices;

namespace SteamControllerBridge.Bridge;

internal static class KeyboardInput
{
    public static bool SetKey(ushort virtualKey, bool down, out int errorCode)
    {
        errorCode = 0;
        if (virtualKey == 0)
        {
            return true;
        }

        var scanCode = (ushort)MapVirtualKey(virtualKey, MAPVK_VK_TO_VSC);
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = scanCode == 0 ? virtualKey : (ushort)0,
                    wScan = scanCode,
                    dwFlags = (scanCode == 0 ? 0u : KEYEVENTF_SCANCODE) |
                        (down ? 0u : KEYEVENTF_KEYUP) |
                        ExtendedFlag(virtualKey)
                }
            }
        };

        if (SendInput(1, [input], Marshal.SizeOf<INPUT>()) == 1)
        {
            return true;
        }

        errorCode = Marshal.GetLastWin32Error();
        return false;
    }

    private static uint ExtendedFlag(ushort virtualKey)
    {
        return virtualKey is
            VK_INSERT or VK_DELETE or VK_HOME or VK_END or VK_PRIOR or VK_NEXT or
            VK_LEFT or VK_RIGHT or VK_UP or VK_DOWN or VK_NUMLOCK or VK_DIVIDE or
            VK_RCONTROL or VK_RMENU
            ? KEYEVENTF_EXTENDEDKEY
            : 0;
    }

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint MAPVK_VK_TO_VSC = 0;
    private const ushort VK_INSERT = 0x2D;
    private const ushort VK_DELETE = 0x2E;
    private const ushort VK_HOME = 0x24;
    private const ushort VK_END = 0x23;
    private const ushort VK_PRIOR = 0x21;
    private const ushort VK_NEXT = 0x22;
    private const ushort VK_LEFT = 0x25;
    private const ushort VK_UP = 0x26;
    private const ushort VK_RIGHT = 0x27;
    private const ushort VK_DOWN = 0x28;
    private const ushort VK_NUMLOCK = 0x90;
    private const ushort VK_DIVIDE = 0x6F;
    private const ushort VK_RCONTROL = 0xA3;
    private const ushort VK_RMENU = 0xA5;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
