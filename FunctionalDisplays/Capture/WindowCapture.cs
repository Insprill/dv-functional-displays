using System;
using System.Drawing;
using System.Drawing.Imaging;
using FunctionalDisplays.Native;
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

    public WindowCapture(Settings settings)
    {
        this.settings = settings;
    }

    public override Texture2D Texture => texture;

    public override void Capture()
    {
        uint pid = settings.windowPid.Value;
        if (!User32.Helper.TryGetRootWindowOfProcess(pid, out IntPtr windowHandle))
        {
            FunctionalDisplays.Instance.Logger.LogError($"No windows found for PID {pid}!");
            return;
        }

        User32.GetClientRect(windowHandle, out Rectangle windowRect);

        int width = windowRect.Width;
        int height = windowRect.Height;

        if (width <= 0 || height <= 0)
        {
            FunctionalDisplays.Instance.Logger.LogError($"Invalid window dimensions for PID {pid} ({width}x{height})!");
            return;
        }

        if (lastWidth != width || lastHeight != height)
        {
            FunctionalDisplays.Instance.Logger.LogDebug($"Window dimensions changed for PID {pid} Before: {lastWidth}x{lastHeight} After: {width}x{height}");
            texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            lastWidth = width;
            lastHeight = height;
        }

        // Get the handle to the client area's device context
        IntPtr hdcSrc = User32.GetDC(windowHandle);

        // Draw the captured image (hBitmap) into our bitmap object
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr hdcBitmap = graphics.GetHdc();
            Gdi32.BitBlt(hdcBitmap, 0, 0, width, height, hdcSrc, 0, 0, Gdi32.SRCCOPY);
            graphics.ReleaseHdc(hdcBitmap);
        }

        BitmapData bitmapData = bitmap.LockBits(windowRect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

        texture.LoadRawTextureData(bitmapData.Scan0, width * height * 4);
        texture.Apply();

        bitmap.UnlockBits(bitmapData);

        // Clean up
        User32.ReleaseDC(windowHandle, hdcSrc);
    }

    public override void Cleanup()
    {
        bitmap?.Dispose();
    }
}
