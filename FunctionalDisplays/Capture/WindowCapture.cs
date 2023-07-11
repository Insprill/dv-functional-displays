using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX.Mathematics.Interop;
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
    private static extern bool GetWindowRect(IntPtr hWnd, out RawRectangle rect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.Dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

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

        GetWindowRect(windowHandle, out RawRectangle windowRect);

        int width = windowRect.Right - windowRect.Left;
        int height = windowRect.Bottom - windowRect.Top;

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

        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr deviceContext = graphics.GetHdc();

            // Capture the screen region of the target window
            if (!PrintWindow(windowHandle, deviceContext, 0))
            {
                FunctionalDisplays.Instance.Logger.LogError("Failed to print window!");
                return;
            }

            // Do something with the captured image
            graphics.ReleaseHdc(deviceContext);
        }

        BitmapData bitmapData = bitmap.LockBits(boundsRect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

        texture.LoadRawTextureData(bitmapData.Scan0, lastWidth * lastHeight * 4);
        texture.Apply();

        bitmap.UnlockBits(bitmapData);
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
