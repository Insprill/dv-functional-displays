using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using FunctionalDisplays.Capture;
using FunctionalDisplays.Native;
using UnityEngine;

namespace FunctionalDisplays.Config;

public class Settings
{
    public readonly ConfigFile configFile;

    public readonly ConfigEntry<bool> enabled;
    public readonly ConfigEntry<CaptureSourceType> captureSourceType;

    public readonly ConfigEntry<byte> framerate;
    public readonly ConfigEntry<byte> adapter;
    public readonly ConfigEntry<byte> display;

    public readonly ConfigEntry<uint> windowPid;

    private int selectedProcess;

    public Settings(ConfigFile config)
    {
        configFile = config;

        enabled = config.Bind(
            "General",
            "Enabled",
            true,
            new ConfigDescription("Whether in-game displays are enabled", null, new ConfigAttributes { ReinitializeCaptureSources = false })
        );
        captureSourceType = config.Bind(
            "General",
            "Capture Source",
            CaptureSourceType.Screen,
            "What method to use for capturing the display"
        );

        framerate = config.Bind(
            "Screen Capture",
            "Framerate",
            (byte)15,
            new ConfigDescription("Framerate of the display", new AcceptableValueRange<byte>(1, 60), new ConfigAttributes { ReinitializeCaptureSources = false })
        );
        adapter = config.Bind(
            "Screen Capture",
            "Adapter",
            (byte)0,
            "Which graphics adapter to use"
        );
        display = config.Bind(
            "Screen Capture",
            "Display",
            (byte)0,
            "Which display to capture"
        );

        windowPid = config.Bind(
            "Window Capture",
            "PID",
            (uint)0,
            new ConfigDescription("The PID of the window to capture", null, new ConfigurationManagerAttributes { CustomDrawer = DrawPidPicker })
        );
    }

    private void DrawPidPicker(ConfigEntryBase entry)
    {
        Dictionary<uint, IntPtr> rootWindows = User32.Helper.GetRootWindows();

        Dictionary<string, uint> reverseRootWindows = rootWindows
            .Select(kvp =>
            {
                StringBuilder sb = new(32);
                User32.GetWindowText(kvp.Value, sb, sb.Capacity);
                return new KeyValuePair<string, uint>(sb.ToString().Trim(), kvp.Key);
            })
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .GroupBy(kvp => kvp.Key)
            .Select(group => group.First())
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        string[] titles = reverseRootWindows.Keys.OrderBy(title => title).ToArray();

        selectedProcess = GUILayout.SelectionGrid(selectedProcess, titles, 1);
        uint pid = reverseRootWindows[titles[selectedProcess]];
        windowPid.Value = pid;
    }
}
