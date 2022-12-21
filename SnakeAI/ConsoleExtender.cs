using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System;

public struct Coord
{
	public short X;
	public short Y;

    public Coord(short x, short y)
    {
        X = x;
        Y = y;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
public unsafe struct ConsoleFont
{
	private const int LF_FACESIZE = 32;
	public ulong cbSize;
	public uint nFont;
	public Coord dwFontSize;
	public uint FontFamily;
	public uint FontWeight;
	public fixed char FaceName[LF_FACESIZE];
}

public static class ConsoleEx
{
	private static void PrintLastError()
    {
		Console.WriteLine("Error Code: 0x{0:X}", Marshal.GetLastWin32Error());
	}

	[DllImport("kernel32", SetLastError = true)]
	private extern static bool SetCurrentConsoleFontEx(IntPtr hOutput, bool bMaximumWindow, ConsoleFont lpFont);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern int SetConsoleFont(IntPtr hOut, uint fontNum);

	public static void SetConsoleFont(int fontNum)
    {
		if (SetConsoleFont(GetStdHandle(StdHandle.OutputHandle), (uint)fontNum) == 0)
			PrintLastError();
	}

	[DllImport("kernel32", SetLastError = true)]
	private extern static bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, [Out] ConsoleFont lpFont);

	private enum StdHandle : int
	{
		OutputHandle = -11
	}

	[DllImport("kernel32")]
	private static extern IntPtr GetStdHandle(StdHandle index);

	public static void SetConsoleFontEx(ConsoleFont font)
	{
		if (!SetCurrentConsoleFontEx(GetStdHandle(StdHandle.OutputHandle), false, font))
			PrintLastError();
	}

	public static ConsoleFont CreateTinyFont()
    {
		ConsoleFont font = new();
		Console.WriteLine("Handle: " + GetStdHandle(StdHandle.OutputHandle));
		if (!GetCurrentConsoleFontEx(GetStdHandle(StdHandle.OutputHandle), false, font))
			PrintLastError();
		font.dwFontSize = new(1, 1);
		return font;
    }

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

	public static void Maximize()
	{
		Process p = Process.GetCurrentProcess();
		ShowWindow(p.MainWindowHandle, 3); //SW_MAXIMIZE = 3
	}
}
