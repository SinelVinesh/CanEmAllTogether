using System.Reflection;
using CanEmAll.SplitScreenMod.PlayerTwo;
using HarmonyLib;
using UnityEngine;

namespace CanEmAll.SplitScreenMod.Patches;

// Runs player-owned components' Unity messages as their owner.
internal static class PlayerContextPatches
{
    private const BindingFlags DeclaredInstanceMethods =
        BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly string[] UnityMessages =
    {
        "Awake", "Start", "OnEnable", "OnDisable", "Update", "FixedUpdate", "LateUpdate",
    };

    public static void Apply(Harmony harmony)
    {
        var prefix = new HarmonyMethod(AccessTools.Method(typeof(PlayerContextPatches), nameof(EnterOwnerContext)));
        var postfix = new HarmonyMethod(AccessTools.Method(typeof(PlayerContextPatches), nameof(ExitOwnerContext)));

        foreach (var method in OwnedMessageMethods())
        {
            harmony.Patch(method, prefix, postfix);
        }

        var bulletCounterAwake = AccessTools.Method(typeof(BulletCounter), "Awake");
        if (bulletCounterAwake != null)
        {
            harmony.Patch(
                bulletCounterAwake,
                new HarmonyMethod(AccessTools.Method(
                    typeof(PlayerContextPatches),
                    nameof(SkipBulletCounterAwake))));
        }

    }

    private static IEnumerable<MethodInfo> OwnedMessageMethods()
    {
        foreach (var type in OwnedComponentTypes())
        {
            foreach (var message in UnityMessages)
            {
                var method = type.GetMethod(message, DeclaredInstanceMethods, null, Type.EmptyTypes, null);
                if (method != null)
                {
                    yield return method;
                }
            }
        }
    }

    private static IEnumerable<Type> OwnedComponentTypes()
    {
        yield return typeof(Player);
        yield return typeof(Inventory);

        // Their Awake claims Instance; VacuumBar reads Vacuum.
        yield return typeof(BulletCounter);
        yield return typeof(VacuumBar);

        foreach (var type in typeof(Weapon).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Weapon))))
        {
            yield return type;
        }
    }

    private static void EnterOwnerContext(Component __instance, out bool __state)
    {
        __state = PlayerTwoSession.Owns(__instance);
        if (__state)
        {
            PlayerContext.Enter(PlayerTwoSession.Rig);
        }
    }

    private static void ExitOwnerContext(bool __state)
    {
        if (__state)
        {
            PlayerContext.Exit();
        }
    }

    private static bool SkipBulletCounterAwake(BulletCounter __instance) =>
        !PlayerTwoHud.ConsumeSkippedBulletCounterAwake(__instance);

}
