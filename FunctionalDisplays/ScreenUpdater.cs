using System.Collections;
using UnityEngine;
using ScreenCapture = FunctionalDisplays.Capture.ScreenCapture;

namespace FunctionalDisplays;

public static class ScreenUpdater
{
    private static Material material;

    public static Material GetMaterial()
    {
        if (material != null)
            return material;
        material = new Material(Shader.Find("Unlit/Texture"));
        return material;
    }

    public static IEnumerator UpdateScreens(FunctionalDisplays functionalDisplays)
    {
        byte adapter = functionalDisplays.Settings.adapter.Value;
        byte display = functionalDisplays.Settings.display.Value;
        ScreenCapture screenCapture = new(adapter, display);

        while (WorldStreamingInit.IsLoaded)
        {
            if (!functionalDisplays.Settings.enabled.Value)
            {
                yield return WaitFor.SecondsRealtime(1f);
                continue;
            }

            if (adapter != functionalDisplays.Settings.adapter.Value || display != functionalDisplays.Settings.display.Value)
            {
                adapter = functionalDisplays.Settings.adapter.Value;
                display = functionalDisplays.Settings.display.Value;
                screenCapture.Cleanup();
                screenCapture = new ScreenCapture(adapter, display);
            }

            Texture2D texture = screenCapture.CaptureScreen();
            material.mainTexture ??= texture;

            yield return WaitFor.SecondsRealtime(1f / FunctionalDisplays.Instance.Settings.framerate.Value);
        }

        screenCapture.Cleanup();
    }
}
