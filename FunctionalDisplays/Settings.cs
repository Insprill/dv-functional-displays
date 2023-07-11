using System;
using BepInEx.Configuration;
using FunctionalDisplays.Capture;

namespace FunctionalDisplays;

public class Settings
{
    public readonly ConfigEntry<bool> enabled;
    public readonly ConfigEntry<CaptureSourceType> captureSourceType;

    public readonly ConfigEntry<byte> framerate;
    public readonly ConfigEntry<byte> adapter;
    public readonly ConfigEntry<byte> display;

    public readonly ConfigEntry<int> windowPid;
    public readonly ConfigEntry<int> windowIndex;

    public Settings(ConfigFile config)
    {
        enabled = config.Bind("General", "Enabled", true, "Whether in-game displays are enabled");
        captureSourceType = config.Bind("General", "Capture Source", CaptureSourceType.Screen, "What method to use for capturing the display");

        framerate = config.Bind("Screen Capture", "Framerate", (byte)15, new ConfigDescription("Framerate of the display", new AcceptableValueRange<byte>(1, 60)));
        adapter = config.Bind("Screen Capture", "Adapter", (byte)0, "Which graphics adapter to use");
        display = config.Bind("Screen Capture", "Display", (byte)0, "Which display to capture");

        windowPid = config.Bind("Window Capture", "PID", 0, "The PID of the window to capture");
        windowIndex = config.Bind("Window Capture", "Index", 0, "The index of the window to capture");
    }
}
