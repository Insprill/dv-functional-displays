using System;
using UnityEngine;

namespace FunctionalDisplays.Capture;

public abstract class CaptureSource
{
    public abstract Texture2D Texture { get; }

    public abstract void Capture();

    public abstract void Cleanup();

    public static CaptureSource CreateSource(Settings settings)
    {
        return settings.captureSourceType.Value switch {
            CaptureSourceType.Screen => new ScreenCapture(settings.adapter.Value, settings.display.Value),
            CaptureSourceType.Window => new WindowCapture(settings),
            _ => throw new ArgumentOutOfRangeException($"Invalid capture source type {settings.captureSourceType.Value}")
        };
    }
}

public enum CaptureSourceType : byte
{
    Screen,
    Window
}
