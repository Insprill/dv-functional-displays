using System.Collections;
using System.Linq;
using FunctionalDisplays.Capture;
using FunctionalDisplays.Config;
using UnityEngine;

namespace FunctionalDisplays;

public static class ScreenUpdater
{
    private static Material material;
    private static CaptureSource captureSource;

    public static Material GetMaterial()
    {
        if (material != null)
            return material;
        material = new Material(Shader.Find("Unlit/Texture"));
        return material;
    }

    public static IEnumerator UpdateScreens(FunctionalDisplays functionalDisplays)
    {
        Settings settings = functionalDisplays.Settings;
        InitSource(settings);

        settings.configFile.SettingChanged += (_, args) =>
        {
            object[] tags = args.ChangedSetting.Description.Tags;
            if (tags.OfType<ConfigAttributes>().FirstOrDefault()?.ReinitializeCaptureSources == false)
                return;
            InitSource(settings);
        };

        while (WorldStreamingInit.IsLoaded)
        {
            if (!settings.enabled.Value)
            {
                yield return WaitFor.SecondsRealtime(1f);
                continue;
            }

            captureSource.Capture();
            material.mainTexture = captureSource.Texture;

            yield return WaitFor.SecondsRealtime(1f / settings.framerate.Value);
        }

        captureSource.Cleanup();
    }

    private static void InitSource(Settings settings)
    {
        captureSource?.Cleanup();
        captureSource = CaptureSource.CreateSource(settings);
    }
}
