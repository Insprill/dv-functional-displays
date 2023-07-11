using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UnityEngine;
using Graphics = System.Drawing.Graphics;

namespace FunctionalDisplays.Capture;

public class WindowCapture : CaptureSource
{
    private readonly Settings settings;
    private int lastWidth;
    private int lastHeight;
    private Texture2D texture;
    private Bitmap bitmap;
    private Rectangle boundsRect;

    public WindowCapture(Settings settings)
    {
        this.settings = settings;
    }

    public override Texture2D Texture => texture;

    #region Windows API

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.Dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    private const uint SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

    #endregion

    public override void Capture()
    {
        int pid = settings.windowPid.Value;
        List<IntPtr> rootWindows = GetRootWindowsOfProcess(pid);
        if (rootWindows.Count == 0)
        {
            FunctionalDisplays.Instance.Logger.LogError($"No windows found for PID {pid}!");
            return;
        }

        int index = settings.windowIndex.Value;
        if (index >= rootWindows.Count || index < 0)
        {
            FunctionalDisplays.Instance.Logger.LogError($"Invalid window index {index} for process {pid}!");
            return;
        }

        IntPtr windowHandle = rootWindows[index];
        if (windowHandle == IntPtr.Zero)
        {
            FunctionalDisplays.Instance.Logger.LogError("Window not found!");
            return;
        }

        GetClientRect(windowHandle, out Rectangle clientRect);
        int width = clientRect.Width;
        int height = clientRect.Height;

        if (width <= 0 || height <= 0)
        {
            FunctionalDisplays.Instance.Logger.LogError("Invalid window dimensions!");
            return;
        }

        if (lastWidth != width || lastHeight != height)
        {
            texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            boundsRect = new Rectangle(0, 0, width, height);
            bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            lastWidth = width;
            lastHeight = height;
        }

        // Get the handle to the client area's device context
        IntPtr hdcSrc = GetWindowDC(windowHandle);

        // Create a memory device context compatible with the client area
        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);

        // Create a bitmap compatible with the client area
        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);

        // Bit block transfer into our compatible memory DC.
        if (!BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY))
        {
            FunctionalDisplays.Instance.Logger.LogError("Failed to capture window!");
            return;
        }

        // Draw the captured image (hBitmap) into our bitmap object
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr hdcBitmap = graphics.GetHdc();
            BitBlt(hdcBitmap, 0, 0, width, height, hdcDest, 0, 0, SRCCOPY);
            graphics.ReleaseHdc(hdcBitmap);
        }

        BitmapData bitmapData = bitmap.LockBits(boundsRect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

        texture.LoadRawTextureData(bitmapData.Scan0, lastWidth * lastHeight * 4);
        texture.Apply();

        bitmap.UnlockBits(bitmapData);

        // Clean up
        DeleteObject(hBitmap);
        DeleteDC(hdcDest);
        ReleaseDC(windowHandle, hdcSrc);
    }

    private static List<IntPtr> GetRootWindowsOfProcess(int pid)
    {
        List<IntPtr> rootWindows = GetChildWindows(IntPtr.Zero);
        List<IntPtr> dsProcRootWindows = new();
        foreach (IntPtr hWnd in rootWindows)
        {
            GetWindowThreadProcessId(hWnd, out uint lpdwProcessId);
            if (lpdwProcessId == pid)
                dsProcRootWindows.Add(hWnd);
        }

        return dsProcRootWindows;
    }

    private static List<IntPtr> GetChildWindows(IntPtr parent)
    {
        List<IntPtr> result = new();
        GCHandle listHandle = GCHandle.Alloc(result);
        try
        {
            Win32Callback childProc = EnumWindow;
            EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
        }
        finally
        {
            if (listHandle.IsAllocated)
                listHandle.Free();
        }

        return result;
    }

    private static bool EnumWindow(IntPtr handle, IntPtr pointer)
    {
        GCHandle gch = GCHandle.FromIntPtr(pointer);
        if (gch.Target is not List<IntPtr> list) throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
        list.Add(handle);
        return true;
    }

    public override void Cleanup()
    {
        bitmap?.Dispose();
    }
}
