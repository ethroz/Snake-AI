using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System;

public struct COORD
{
    public short X;
    public short Y;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct CONSOLE_FONT_INFO_EX
{
    public uint cbSize;
    public uint nFont;
    public COORD dwFontSize;
    public int FontFamily;
    public int FontWeight;
    public fixed char FaceName[32];
}

public static class ConsoleEx
{
    public const int STD_OUTPUT_HANDLE = -11;
    public const int SW_MAXIMIZE = 3;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFO_EX lpConsoleCurrentFontEx);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFO_EX lpConsoleCurrentFontEx);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static bool Maximize()
	{
		return ShowWindow(GetConsoleWindow(), SW_MAXIMIZE);
	}
}
