using System;
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
    private readonly Texture2D texture;
    private readonly SharpDX.Direct3D11.Texture2D screenTexture;
    private readonly byte[] rgbValues;

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
        rgbValues = new byte[width * 4 * height];
    }

    public void Cleanup()
    {
        screenTexture.Dispose();
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
        try
        {
            CreateCapture();
        }
        catch (SharpDXException e)
        {
            FunctionalDisplays.Instance.Logger.LogError($"Failed to capture screen: {e.Message}");
        }

        return texture;
    }

    private void CreateCapture()
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
        IntPtr sourcePtr = IntPtr.Add(mapSource.DataPointer, mapSource.RowPitch * (height - 1));
        for (int y = 0; y < height; y++)
        {
            // Copy a single line
            Marshal.Copy(sourcePtr, rgbValues, y * width * 4, width * 4);

            // Advance pointer
            sourcePtr = IntPtr.Subtract(sourcePtr, mapSource.RowPitch);
        }

        texture.LoadRawTextureData(rgbValues);
        texture.Apply();

        device.ImmediateContext.UnmapSubresource(screenTexture, 0);

        // Release temporary resources
        screenResource.Dispose();
    }
}
