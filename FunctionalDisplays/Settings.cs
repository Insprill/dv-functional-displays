using BepInEx.Configuration;

namespace FunctionalDisplays;

public class Settings
{
    public readonly ConfigEntry<bool> enabled;
    public readonly ConfigEntry<byte> framerate;
    public readonly ConfigEntry<byte> adapter;
    public readonly ConfigEntry<byte> display;

    public Settings(ConfigFile config)
    {
        enabled = config.Bind("General", "Enabled", true, "Whether in-game displays are enabled");
        framerate = config.Bind("Screen Capture", "Framerate", (byte)15, new ConfigDescription("Framerate of the display", new AcceptableValueRange<byte>(1, 60)));
        adapter = config.Bind("Screen Capture", "Adapter", (byte)0, "Which graphics adapter to use");
        display = config.Bind("Screen Capture", "Display", (byte)0, "Which display to capture");
    }
}
