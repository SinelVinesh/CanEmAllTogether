using UnityEngine;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

// Holding a stick click while moving is tiring, so L3 toggles.
internal static class PlayerTwoSprint
{
    private static int _refreshedFrame = -1;
    private static bool _active;
    private static bool _activeLastFrame;

    public static bool IsActive
    {
        get
        {
            Refresh();
            return _active;
        }
    }

    public static bool Started
    {
        get
        {
            Refresh();
            return _active && !_activeLastFrame;
        }
    }

    public static bool Stopped
    {
        get
        {
            Refresh();
            return !_active && _activeLastFrame;
        }
    }

    // The controller calls it each frame so no L3 press is lost.
    public static void Refresh()
    {
        if (Time.frameCount == _refreshedFrame)
        {
            return;
        }

        _refreshedFrame = Time.frameCount;
        _activeLastFrame = _active;
        _active = NextState();
    }

    // A respawn in the same frame must not inherit the sprint.
    public static void Reset() => _active = false;

    private static bool NextState()
    {
        if (!PlayerTwoSession.IsSpawned)
        {
            return false;
        }

        var gamepad = PlayerTwoSession.Gamepad;
        var requested = gamepad.WasPressed(PlayerTwoBindings.Sprint) ? !_active : _active;
        return requested && gamepad.LeftStick != Vector2.zero && !IsOutOfStamina();
    }

    private static bool IsOutOfStamina() => PlayerTwoSession.Rig.Player.currentStamina <= 0f;
}
