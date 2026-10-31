using CanEmAll.SplitScreenMod.PlayerTwo;
using HarmonyLib;
using UnityEngine;

namespace CanEmAll.SplitScreenMod.Patches;

// The game calls the *Int key wrappers, not Input.GetKey.
[HarmonyPatch(typeof(Input))]
internal static class InputPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.GetAxis))]
    private static void GetAxis(string axisName, ref float __result) =>
        __result = PlayerTwoInput.Axis(axisName, __result);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.GetAxisRaw))]
    private static void GetAxisRaw(string axisName, ref float __result) =>
        __result = PlayerTwoInput.Axis(axisName, __result);

    [HarmonyPostfix]
    [HarmonyPatch("GetKeyInt")]
    private static void GetKey(KeyCode key, ref bool __result)
    {
        if (PlayerContext.IsPlayerTwo)
        {
            __result = PlayerTwoInput.KeyHeld(key);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("GetKeyDownInt")]
    private static void GetKeyDown(KeyCode key, ref bool __result)
    {
        if (PlayerContext.IsPlayerTwo)
        {
            __result = PlayerTwoInput.KeyPressed(key);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("GetKeyUpInt")]
    private static void GetKeyUp(KeyCode key, ref bool __result)
    {
        if (PlayerContext.IsPlayerTwo)
        {
            __result = PlayerTwoInput.KeyReleased(key);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.GetMouseButton))]
    private static void GetMouseButton(int button, ref bool __result)
    {
        if (PlayerContext.IsPlayerTwo)
        {
            __result = PlayerTwoInput.MouseButtonHeld(button);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.GetMouseButtonDown))]
    private static void GetMouseButtonDown(int button, ref bool __result)
    {
        if (PlayerContext.IsPlayerTwo)
        {
            __result = PlayerTwoInput.MouseButtonPressed(button);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.GetMouseButtonUp))]
    private static void GetMouseButtonUp(int button, ref bool __result)
    {
        if (PlayerContext.IsPlayerTwo)
        {
            __result = PlayerTwoInput.MouseButtonReleased(button);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.mouseScrollDelta), MethodType.Getter)]
    private static void MouseScrollDelta(ref Vector2 __result)
    {
        if (PlayerContext.IsPlayerTwo)
        {
            __result = PlayerTwoInput.ScrollDelta();
        }
    }
}
