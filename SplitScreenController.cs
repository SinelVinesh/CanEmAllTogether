using CanEmAll.SplitScreenMod.Diagnostics;
using CanEmAll.SplitScreenMod.PlayerTwo;
using UnityEngine;

namespace CanEmAll.SplitScreenMod;

internal sealed class SplitScreenController : MonoBehaviour
{
    private const int PrimaryDisplayIndex = 0;
    private const int SecondaryDisplayIndex = 1;
    private const float DividerThickness = 2f;

    private bool _enabled;
    private SplitScreenLayout _layout;
    private Camera _playerOneCamera;
    private Rect _playerOneOriginalViewport;
    private Camera _playerTwoCamera;

    public SplitScreenController(IntPtr ptr) : base(ptr)
    {
    }

    private bool IsAttached => _playerOneCamera != null && _playerTwoCamera != null && PlayerTwoSession.IsSpawned;

    private void Update()
    {
        PlayerContext.ResetIfLeaked();

        if (Input.GetKeyDown(ModConfig.ToggleKey.Value))
        {
            SetSplitScreen(!_enabled);
        }
        // Despawned objects linger until frame end and skew a report.
        else if (Input.GetKeyDown(ModConfig.HudReportKey.Value))
        {
            RunOptionalFeature("HUD report", HudReport.Log);
        }

        // Last, so a sprint error never blocks the toggle key.
        PlayerTwoSprint.Refresh();
    }

    private void LateUpdate()
    {
        if (!_enabled)
        {
            return;
        }

        // Scene loads destroy both players and their cameras.
        if (!IsAttached && !TryAttach())
        {
            return;
        }

        ApplyViewports();
        PlayerTwoHud.RefreshSharedTexts();
    }

    private void OnGUI()
    {
        if (!_enabled || !IsAttached || !_layout.SharesOneScreen())
        {
            return;
        }

        GUI.DrawTexture(DividerRect(_layout), Texture2D.whiteTexture);
    }

    private void SetSplitScreen(bool enabled)
    {
        _enabled = enabled;
        Plugin.Logger.LogInfo($"Split screen {(enabled ? "enabled" : "disabled")}.");

        if (!enabled)
        {
            Detach();
            return;
        }

        _layout = ResolveLayout(ModConfig.Layout.Value);
        if (!TryAttach())
        {
            Plugin.Logger.LogWarning("No player found yet; will retry every frame.");
        }
    }

    // A failing extra must never break split screen itself.
    private static void RunOptionalFeature(string featureName, Action feature)
    {
        try
        {
            feature();
        }
        catch (Exception exception)
        {
            Plugin.Logger.LogError($"{featureName} failed: {exception}");
        }
    }

    private bool TryAttach()
    {
        Detach();

        var playerOne = Player.Instance;
        if (playerOne == null || playerOne.mainCamera == null)
        {
            return false;
        }

        _playerOneCamera = playerOne.mainCamera;
        _playerOneOriginalViewport = _playerOneCamera.rect;

        var rig = PlayerTwoSession.Spawn(playerOne);
        _playerTwoCamera = rig.Camera;
        _playerTwoCamera.targetDisplay = _layout.SharesOneScreen()
            ? _playerOneCamera.targetDisplay
            : SecondaryDisplayIndex;

        if (!_layout.SharesOneScreen())
        {
            RunOptionalFeature("Player 2 HUD", () => PlayerTwoHud.Spawn(SecondaryDisplayIndex, rig));
        }

        Plugin.Logger.LogInfo($"Attached to camera '{_playerOneCamera.name}' using {_layout} layout.");
        return true;
    }

    private void Detach()
    {
        PlayerTwoSession.Despawn();
        PlayerTwoHud.Despawn();
        _playerTwoCamera = null;

        if (_playerOneCamera != null)
        {
            _playerOneCamera.rect = _playerOneOriginalViewport;
            _playerOneCamera.ResetAspect();
        }

        _playerOneCamera = null;
    }

    // Game scripts may reset the viewport at any time.
    private void ApplyViewports()
    {
        SetViewport(_playerOneCamera, _layout.PlayerOneViewport());
        SetViewport(_playerTwoCamera, _layout.PlayerTwoViewport());
    }

    private static void SetViewport(Camera camera, Rect viewport)
    {
        if (camera.rect == viewport)
        {
            return;
        }

        camera.rect = viewport;
        camera.ResetAspect();
    }

    private static SplitScreenLayout ResolveLayout(SplitScreenLayout requested)
    {
        if (requested != SplitScreenLayout.SeparateDisplays || TryActivateSecondaryDisplay())
        {
            return requested;
        }

        Plugin.Logger.LogWarning("Only one monitor detected; falling back to the Vertical layout.");
        return SplitScreenLayout.Vertical;
    }

    // Unity cannot deactivate a display once it is activated.
    private static bool TryActivateSecondaryDisplay()
    {
        var displays = Display.displays;
        if (displays.Length <= SecondaryDisplayIndex)
        {
            return false;
        }

        displays[SecondaryDisplayIndex].Activate();
        return true;
    }

    private static Rect DividerRect(SplitScreenLayout layout)
    {
        var display = Display.displays[PrimaryDisplayIndex];
        float width = display.renderingWidth;
        float height = display.renderingHeight;

        return layout == SplitScreenLayout.Vertical
            ? new Rect((width - DividerThickness) / 2f, 0f, DividerThickness, height)
            : new Rect(0f, (height - DividerThickness) / 2f, width, DividerThickness);
    }
}
