using CanEmAll.SplitScreenMod.Gamepad;
using UnityEngine;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

internal static class PlayerTwoBindings
{
    public const GamepadButtons NextWeapon = GamepadButtons.RightShoulder;
    public const GamepadButtons PreviousWeapon = GamepadButtons.LeftShoulder;
    public const GamepadButtons Sprint = GamepadButtons.LeftStick;
    public const KeyCode SprintKey = KeyCode.LeftShift;

    private static readonly Dictionary<KeyCode, GamepadButtons> KeyBindings = new()
    {
        [KeyCode.Space] = GamepadButtons.A,
        [KeyCode.E] = GamepadButtons.X,
        [KeyCode.F] = GamepadButtons.Y,
        [KeyCode.Q] = GamepadButtons.RightShoulder,
    };

    // Indexed by Unity mouse button: 0 is left, 1 is right.
    private static readonly GamepadButtons[] MouseButtonBindings =
    {
        GamepadButtons.RightTrigger,
        GamepadButtons.LeftTrigger,
    };

    public static GamepadButtons ForKey(KeyCode key) =>
        KeyBindings.TryGetValue(key, out var buttons) ? buttons : GamepadButtons.None;

    public static GamepadButtons ForMouseButton(int button) =>
        button >= 0 && button < MouseButtonBindings.Length ? MouseButtonBindings[button] : GamepadButtons.None;
}
