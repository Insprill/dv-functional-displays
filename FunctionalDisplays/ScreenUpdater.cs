using System.Collections;
using FunctionalDisplays.Capture;
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

        settings.captureSourceType.SettingChanged += (_, _) =>
        {
            InitSource(settings);
        };
        settings.adapter.SettingChanged += (_, _) =>
        {
            if (settings.captureSourceType.Value == CaptureSourceType.Screen)
                InitSource(settings);
        };
        settings.display.SettingChanged += (_, _) =>
        {
            if (settings.captureSourceType.Value == CaptureSourceType.Screen)
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

            yield return WaitFor.SecondsRealtime(1f / FunctionalDisplays.Instance.Settings.framerate.Value);
        }

        captureSource.Cleanup();
    }

    private static void InitSource(Settings settings)
    {
        captureSource?.Cleanup();
        captureSource = CaptureSource.CreateSource(settings);
    }
}
