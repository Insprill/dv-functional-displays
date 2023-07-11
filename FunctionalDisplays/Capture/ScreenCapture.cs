using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using UnityEngine;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.DXGI.Resource;
using Texture2D = UnityEngine.Texture2D;

namespace FunctionalDisplays.Capture;

public class ScreenCapture
{
    private readonly Factory1 factory;
    private readonly Adapter1 adapter;
    private readonly Device device;
    private readonly Output output;
    private readonly Output1 output1;
    private readonly OutputDuplication duplication;
    private readonly int width;
    private readonly int height;
    private readonly Rectangle boundsRect;
    private readonly Bitmap bitmap;
    private readonly Texture2D texture;
    private readonly SharpDX.Direct3D11.Texture2D screenTexture;

    private bool hasFrameToRelease;

    public ScreenCapture(byte adapterIndex, byte displayIndex)
    {
        factory = new Factory1();
        adapter = factory.GetAdapter1(adapterIndex);
        device = new Device(adapter, DeviceCreationFlags.None, FeatureLevel.Level_11_0);
        output = adapter.GetOutput(displayIndex);
        output1 = output.QueryInterface<Output1>();
        duplication = output1.DuplicateOutput(device);
        width = output.Description.DesktopBounds.Right;
        height = output.Description.DesktopBounds.Bottom;
        boundsRect = new Rectangle(0, 0, width, height);
        bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        screenTexture = new SharpDX.Direct3D11.Texture2D(device, new Texture2DDescription {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        });
    }

    public void Cleanup()
    {
        screenTexture.Dispose();
        bitmap.Dispose();
        if (hasFrameToRelease) duplication.ReleaseFrame();
        duplication.Dispose();
        output1.Dispose();
        output.Dispose();
        device.Dispose();
        adapter.Dispose();
        factory.Dispose();
    }

    public Texture2D CaptureScreen()
    {
        // Free the last capture from GPU memory
        if (hasFrameToRelease)
        {
            duplication.ReleaseFrame();
            hasFrameToRelease = false;
        }

        // Capture the screen
        duplication.AcquireNextFrame(1000, out OutputDuplicateFrameInformation _, out Resource screenResource);
        hasFrameToRelease = true;

        // Copy the resource into CPU memory
        device.ImmediateContext.CopyResource(screenResource.QueryInterface<SharpDX.Direct3D11.Resource>(), screenTexture);

        // Get the desktop capture texture
        DataBox mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, MapFlags.None);

        // Copy pixels from screen capture Texture to GDI bitmap
        BitmapData mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
        IntPtr sourcePtr = mapSource.DataPointer;
        IntPtr destPtr = IntPtr.Add(mapDest.Scan0, mapDest.Stride * (height - 1));
        for (int y = 0; y < height; y++)
        {
            // Copy a single line
            Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

            // Advance pointers
            sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
            destPtr = IntPtr.Subtract(destPtr, mapDest.Stride);
        }

        // Release source and dest locks
        bitmap.UnlockBits(mapDest);
        device.ImmediateContext.UnmapSubresource(screenTexture, 0);

        // Release all resources
        screenResource.Dispose();

        return BitmapToTexture2D();
    }

    private Texture2D BitmapToTexture2D()
    {
        // Lock the bitmap's bits
        BitmapData bitmapData = bitmap.LockBits(boundsRect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

        // Declare an array to hold the bytes of the bitmap
        int bytes = Mathf.Abs(bitmapData.Stride) * height;
        byte[] rgbValues = new byte[bytes];

        // Get the address of the first line
        IntPtr ptr = bitmapData.Scan0;

        // Copy the RGB values into the array
        Marshal.Copy(ptr, rgbValues, 0, bytes);

        bitmap.UnlockBits(bitmapData);

        texture.LoadRawTextureData(rgbValues);
        texture.Apply();

        return texture;
    }
}
