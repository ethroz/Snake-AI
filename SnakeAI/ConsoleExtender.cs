using System.Runtime.InteropServices;

namespace System;

public struct COORD
{
    public short X;
    public short Y;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct CONSOLE_FONT_INFO_EX
{
    public CONSOLE_FONT_INFO_EX()
    {
        cbSize = (uint)Marshal.SizeOf(typeof(CONSOLE_FONT_INFO_EX));
        nFont = 0;
        dwFontSize = new();
        FontFamily = 0;
        FontWeight = 0;
        FaceName = "";
    }

    public uint cbSize;
    public uint nFont;
    public COORD dwFontSize;
    public int FontFamily;
    public int FontWeight;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string FaceName;
}

public static class ConsoleEx
{
    public const int STD_OUTPUT_HANDLE = -11;
    public const int SW_MAXIMIZE = 3;
    public const uint FLASHW_ALL = 3;
    public const uint FLASHW_TIMERNOFG = 12;

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr Hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;

        public FLASHWINFO(IntPtr hwnd, uint dwFlags, uint uCount)
        {
            cbSize = (uint)Marshal.SizeOf<FLASHWINFO>();
            Hwnd = hwnd;
            this.dwFlags = dwFlags;
            this.uCount = uCount;
            dwTimeout = 0;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public extern static bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, [In, Out] CONSOLE_FONT_INFO_EX lpConsoleCurrentFont);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, [In, Out] CONSOLE_FONT_INFO_EX lpConsoleCurrentFontEx);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    public static bool Maximize()
	{
		return ShowWindow(GetConsoleWindow(), SW_MAXIMIZE);
	}

    public static bool Flash()
    {
        FLASHWINFO fwi = new(GetConsoleWindow(), FLASHW_ALL | FLASHW_TIMERNOFG, uint.MaxValue);
        return FlashWindowEx(ref fwi);
    }
}
