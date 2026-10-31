using UnityEngine;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

internal static class PlayerTwoInput
{
    private const string MoveHorizontalAxis = "Horizontal";
    private const string MoveVerticalAxis = "Vertical";
    private const string LookHorizontalAxis = "Mouse X";
    private const string LookVerticalAxis = "Mouse Y";
    private const float ScrollStep = 1f;

    private static readonly KeyCode[] MoveHorizontalKeys =
        { KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow };

    private static readonly KeyCode[] MoveVerticalKeys =
        { KeyCode.W, KeyCode.S, KeyCode.UpArrow, KeyCode.DownArrow };

    private static float LookDelta => ModConfig.LookSpeed.Value * Time.deltaTime;

    public static float Axis(string axisName, float original)
    {
        if (!PlayerContext.IsPlayerTwo)
        {
            return FilterPlayerOneAxis(axisName, original);
        }

        var gamepad = PlayerTwoSession.Gamepad;
        return axisName switch
        {
            MoveHorizontalAxis => gamepad.LeftStick.x,
            MoveVerticalAxis => gamepad.LeftStick.y,
            LookHorizontalAxis => gamepad.RightStick.x * LookDelta,
            LookVerticalAxis => gamepad.RightStick.y * LookDelta,
            _ => 0f,
        };
    }

    public static bool KeyHeld(KeyCode key) =>
        IsSprintKey(key)
            ? PlayerTwoSprint.IsActive
            : PlayerTwoSession.Gamepad.IsHeld(PlayerTwoBindings.ForKey(key));

    public static bool KeyPressed(KeyCode key) =>
        IsSprintKey(key)
            ? PlayerTwoSprint.Started
            : PlayerTwoSession.Gamepad.WasPressed(PlayerTwoBindings.ForKey(key));

    public static bool KeyReleased(KeyCode key) =>
        IsSprintKey(key)
            ? PlayerTwoSprint.Stopped
            : PlayerTwoSession.Gamepad.WasReleased(PlayerTwoBindings.ForKey(key));

    public static bool MouseButtonHeld(int button) =>
        PlayerTwoSession.Gamepad.IsHeld(PlayerTwoBindings.ForMouseButton(button));

    public static bool MouseButtonPressed(int button) =>
        PlayerTwoSession.Gamepad.WasPressed(PlayerTwoBindings.ForMouseButton(button));

    public static bool MouseButtonReleased(int button) =>
        PlayerTwoSession.Gamepad.WasReleased(PlayerTwoBindings.ForMouseButton(button));

    public static Vector2 ScrollDelta()
    {
        var gamepad = PlayerTwoSession.Gamepad;
        if (gamepad.WasPressed(PlayerTwoBindings.NextWeapon))
        {
            return new Vector2(0f, ScrollStep);
        }

        return gamepad.WasPressed(PlayerTwoBindings.PreviousWeapon)
            ? new Vector2(0f, -ScrollStep)
            : Vector2.zero;
    }

    private static bool IsSprintKey(KeyCode key) => key == PlayerTwoBindings.SprintKey;

    // Unity's default move axes also read every joystick.
    private static float FilterPlayerOneAxis(string axisName, float original)
    {
        if (!PlayerTwoSession.IsSpawned)
        {
            return original;
        }

        return axisName switch
        {
            MoveHorizontalAxis => KeyboardOnly(original, MoveHorizontalKeys, PlayerTwoSession.Gamepad.LeftStick.x),
            MoveVerticalAxis => KeyboardOnly(original, MoveVerticalKeys, PlayerTwoSession.Gamepad.LeftStick.y),
            _ => original,
        };
    }

    private static float KeyboardOnly(float original, KeyCode[] axisKeys, float gamepadValue)
    {
        if (gamepadValue == 0f || Array.Exists(axisKeys, Input.GetKey))
        {
            return original;
        }

        return 0f;
    }
}
