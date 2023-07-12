using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using DV.Utils;
using FunctionalDisplays.Config;
using HarmonyLib;

namespace FunctionalDisplays;

[BepInPlugin("net.insprill.dv-functional-displays", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class FunctionalDisplays : BaseUnityPlugin
{
    public static FunctionalDisplays Instance { get; private set; }

    public Settings Settings;
    private Harmony harmony;
    internal new ManualLogSource Logger => base.Logger;

    private void Awake()
    {
        if (Instance != null)
        {
            Logger.LogFatal($"{Info.Metadata.Name} is already loaded!");
            Destroy(this);
            return;
        }

        Instance = this;

        Settings = new Settings(Config);

        WorldStreamingInit.LoadingFinished += () => SingletonBehaviour<CoroutineManager>.Instance.StartCoroutine(ScreenUpdater.UpdateScreens(this));

        try
        {
            Patch();
        }
        catch (Exception ex)
        {
            Logger.LogFatal($"Failed to load {Info.Metadata.Name}: {ex}");
            Destroy(this);
        }
    }

    private void Patch()
    {
        Logger.LogInfo("Patching...");
        harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo("Successfully patched");
    }

    private void OnDestroy()
    {
        Logger.LogInfo("Unpatching...");
        harmony?.UnpatchSelf();
        Logger.LogInfo("Successfully Unpatched");
    }
}
