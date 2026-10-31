using UnityEngine;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

internal static class CloneParts
{
    private const string ModObjectPrefix = "CanEmAll.SplitScreen.";

    public static string PlayerTwoName(string name) => $"{name} (Player 2)";

    public static string ModObjectName(string purpose) => ModObjectPrefix + purpose;

    public static GameObject CreateInactiveHolder(string purpose)
    {
        var holder = new GameObject(ModObjectName(purpose));
        holder.SetActive(false);
        return holder;
    }

    public static void DisableAll<T>(Transform root) where T : Behaviour
    {
        foreach (var behaviour in root.GetComponentsInChildren<T>(true))
        {
            behaviour.enabled = false;
        }
    }
}
