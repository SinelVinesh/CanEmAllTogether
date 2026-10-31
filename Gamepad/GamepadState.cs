using UnityEngine;

namespace CanEmAll.SplitScreenMod.Gamepad;

internal sealed class GamepadState
{
    private const float StickRange = 32767f;
    private const float StickDeadZone = 0.24f;
    private const byte TriggerThreshold = 30;

    // Polling a missing pad is slow, so retries are throttled.
    private const float ReconnectIntervalSeconds = 2f;

    private readonly int _userIndex;
    private int _polledFrame = -1;
    private float _nextReconnectTime;
    private bool _connected = true;
    private GamepadButtons _held;
    private GamepadButtons _heldLastFrame;
    private Vector2 _leftStick;
    private Vector2 _rightStick;

    public GamepadState(int userIndex)
    {
        _userIndex = userIndex;
    }

    public Vector2 LeftStick
    {
        get
        {
            Poll();
            return _leftStick;
        }
    }

    public Vector2 RightStick
    {
        get
        {
            Poll();
            return _rightStick;
        }
    }

    public bool IsHeld(GamepadButtons buttons)
    {
        Poll();
        return (_held & buttons) != 0;
    }

    public bool WasPressed(GamepadButtons buttons)
    {
        Poll();
        return (_held & buttons) != 0 && (_heldLastFrame & buttons) == 0;
    }

    public bool WasReleased(GamepadButtons buttons)
    {
        Poll();
        return (_held & buttons) == 0 && (_heldLastFrame & buttons) != 0;
    }

    private void Poll()
    {
        if (Time.frameCount == _polledFrame)
        {
            return;
        }

        _polledFrame = Time.frameCount;
        _heldLastFrame = _held;

        if (!_connected && Time.unscaledTime < _nextReconnectTime)
        {
            return;
        }

        _connected = XInput.TryGetState(_userIndex, out var state);
        if (!_connected)
        {
            _nextReconnectTime = Time.unscaledTime + ReconnectIntervalSeconds;
            Clear();
            return;
        }

        _held = ReadButtons(state);
        _leftStick = ReadStick(state.ThumbLeftX, state.ThumbLeftY);
        _rightStick = ReadStick(state.ThumbRightX, state.ThumbRightY);
    }

    private void Clear()
    {
        _held = GamepadButtons.None;
        _leftStick = Vector2.zero;
        _rightStick = Vector2.zero;
    }

    private static GamepadButtons ReadButtons(XInput.State state)
    {
        var buttons = (GamepadButtons)state.Buttons;
        if (state.LeftTrigger > TriggerThreshold)
        {
            buttons |= GamepadButtons.LeftTrigger;
        }

        if (state.RightTrigger > TriggerThreshold)
        {
            buttons |= GamepadButtons.RightTrigger;
        }

        return buttons;
    }

    private static Vector2 ReadStick(short rawX, short rawY)
    {
        var stick = new Vector2(rawX / StickRange, rawY / StickRange);
        var magnitude = Mathf.Min(stick.magnitude, 1f);
        if (magnitude < StickDeadZone)
        {
            return Vector2.zero;
        }

        var rescaled = (magnitude - StickDeadZone) / (1f - StickDeadZone);
        return stick.normalized * rescaled;
    }
}
