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
    private Rectangle boundsRect;

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

        User32.GetClientRect(windowHandle, out Rectangle clientRect);
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
        IntPtr hdcSrc = User32.GetWindowDC(windowHandle);

        // Create a memory device context compatible with the client area
        IntPtr hdcDest = Gdi32.CreateCompatibleDC(hdcSrc);

        // Create a bitmap compatible with the client area
        IntPtr hBitmap = Gdi32.CreateCompatibleBitmap(hdcSrc, width, height);

        // Bit block transfer into our compatible memory DC.
        if (!Gdi32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, User32.SRCCOPY))
        {
            FunctionalDisplays.Instance.Logger.LogError("Failed to capture window!");
            return;
        }

        // Draw the captured image (hBitmap) into our bitmap object
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr hdcBitmap = graphics.GetHdc();
            Gdi32.BitBlt(hdcBitmap, 0, 0, width, height, hdcDest, 0, 0, User32.SRCCOPY);
            graphics.ReleaseHdc(hdcBitmap);
        }

        BitmapData bitmapData = bitmap.LockBits(boundsRect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

        texture.LoadRawTextureData(bitmapData.Scan0, lastWidth * lastHeight * 4);
        texture.Apply();

        bitmap.UnlockBits(bitmapData);

        // Clean up
        Gdi32.DeleteObject(hBitmap);
        Gdi32.DeleteDC(hdcDest);
        User32.ReleaseDC(windowHandle, hdcSrc);
    }

    public override void Cleanup()
    {
        bitmap?.Dispose();
    }
}
