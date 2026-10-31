using BepInEx.Configuration;
using UnityEngine;

namespace CanEmAll.SplitScreenMod;

internal static class ModConfig
{
    private const int MaxXInputUserIndex = 3;

    public static ConfigEntry<SplitScreenLayout> Layout { get; private set; }
    public static ConfigEntry<KeyCode> ToggleKey { get; private set; }
    public static ConfigEntry<KeyCode> HudReportKey { get; private set; }
    public static ConfigEntry<int> GamepadIndex { get; private set; }
    public static ConfigEntry<float> LookSpeed { get; private set; }

    public static void Bind(ConfigFile config)
    {
        Layout = config.Bind(
            "Display",
            "Layout",
            SplitScreenLayout.SeparateDisplays,
            "Vertical: left/right halves. Horizontal: top/bottom halves. " +
            "SeparateDisplays: player 2 is shown full screen on the second monitor.");

        ToggleKey = config.Bind(
            "Controls",
            "ToggleKey",
            KeyCode.F1,
            "Key that toggles split screen and player 2 on and off.");

        HudReportKey = config.Bind(
            "Debug",
            "HudReportKey",
            KeyCode.F2,
            "Key that writes a HUD diagnostic report to the BepInEx log.");

        GamepadIndex = config.Bind(
            "PlayerTwo",
            "GamepadIndex",
            0,
            new ConfigDescription(
                "XInput slot (0-3) of the gamepad that controls player 2.",
                new AcceptableValueRange<int>(0, MaxXInputUserIndex)));

        LookSpeed = config.Bind(
            "PlayerTwo",
            "LookSpeed",
            120f,
            new ConfigDescription(
                "Right stick look speed for player 2.",
                new AcceptableValueRange<float>(1f, 1000f)));
    }
}
