using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CanEmAll.SplitScreenMod.Patches;
using CanEmAll.SplitScreenMod.PlayerTwo;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace CanEmAll.SplitScreenMod;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class Plugin : BasePlugin
{
    internal static ManualLogSource Logger { get; private set; }

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} starting.");

        ModConfig.Bind(Config);
        PlayerTwoSession.Initialize(ModConfig.GamepadIndex.Value);
        ApplyPatches();
        ClassInjector.RegisterTypeInIl2Cpp<SplitScreenController>();

        var host = new GameObject("CanEmAll.SplitScreenMod");
        UnityObject.DontDestroyOnLoad(host);
        host.AddComponent<SplitScreenController>();

        Logger.LogInfo($"{PluginInfo.Name} loaded. Press {ModConfig.ToggleKey.Value} to toggle split screen.");
    }

    private static void ApplyPatches()
    {
        var harmony = new Harmony(PluginInfo.Guid);
        harmony.PatchAll(typeof(InputPatches));
        PlayerContextPatches.Apply(harmony);
    }
}

internal static class PluginInfo
{
    public const string Guid = "com.example.canemall.splitscreen";
    public const string Name = "Can 'Em All Split Screen";
    public const string Version = "0.3.0";
}
