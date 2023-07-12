using System;
using System.Runtime.InteropServices;

namespace FunctionalDisplays.Native;

public static class Gdi32
{
    public const uint SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
}
