using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

// Restores on release so player 2 never keeps dead widgets.
internal sealed class PlayerTwoHudBindings
{
    private readonly Func<Transform, Transform> _findCopy;
    private readonly List<Action> _restores = new();
    private readonly List<string> _boundNames = new();
    private readonly List<string> _sharedNames = new();
    private Material _staminaBarMaterial;

    public PlayerTwoHudBindings(Func<Transform, Transform> findCopy)
    {
        _findCopy = findCopy;
    }

    public void BindStaminaBar(Player playerTwo)
    {
        var sourceBar = playerTwo.staminaSlider;
        var copyBar = sourceBar == null ? null : CopyOf(sourceBar);
        if (copyBar == null)
        {
            var path = sourceBar == null ? "<none>" : HierarchyPaths.Of(sourceBar.transform);
            Plugin.Logger.LogWarning($"Stamina bar '{path}' not in player 2's HUD copy; both players share it.");
            return;
        }

        // Fill set via the shared material would reach P1's bar.
        var usesCustomMaterial = IsCustomMaterial(sourceBar);
        if (usesCustomMaterial)
        {
            _staminaBarMaterial = new Material(sourceBar.material);
            copyBar.material = _staminaBarMaterial;
        }

        Rebind(playerTwo, sourceBar, copyBar, (owner, bar) => owner.staminaSlider = bar);
        Plugin.Logger.LogInfo(
            $"Player 2 stamina bar bound to '{HierarchyPaths.Of(copyBar.transform)}' (own material: {usesCustomMaterial}).");
    }

    public void BindBulletCounter(BulletCounter source, BulletCounter target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.visual = CopyOf(source.visual);
        target.title = CopyOf(source.title);
        target.text = CopyOf(source.text);
        target.icon = CopyOf(source.icon);
        Plugin.Logger.LogInfo("Player 2 ammo counter bound to its HUD copy.");
    }

    public PlayerTwoHudMirror BuildSharedTextMirror()
    {
        var mirror = new PlayerTwoHudMirror();
        var sharedTexts = new[]
        {
            ("MoneyManager.moneyText", MoneyManager.Instance?.moneyText),
            ("GameManager.timerText", GameManager.Instance?.timerText),
        };

        foreach (var (name, source) in sharedTexts)
        {
            var copy = source == null ? null : CopyOf(source);
            mirror.Add(source, copy);
            Plugin.Logger.LogInfo($"{name} {(copy == null ? "has no copy on player 2's HUD" : "mirrored to player 2's HUD")}.");
        }

        return mirror;
    }

    public void BindWidgets(Player playerTwo, Inventory inventory)
    {
        Bind("Player.grabDropUI", playerTwo, owner => owner.grabDropUI, (owner, widget) => owner.grabDropUI = widget);
        Bind("Player.grabDropText", playerTwo, owner => owner.grabDropText, (owner, widget) => owner.grabDropText = widget);
        Bind("Player.throwUI", playerTwo, owner => owner.throwUI, (owner, widget) => owner.throwUI = widget);
        Bind("Player.rotateUI", playerTwo, owner => owner.rotateUI, (owner, widget) => owner.rotateUI = widget);

        if (inventory != null)
        {
            Bind("Inventory.switchIcon", inventory, owner => owner.switchIcon, (owner, widget) => owner.switchIcon = widget);
            Bind("Inventory.weaponsPanel", inventory, owner => owner.weaponsPanel, (owner, widget) => owner.weaponsPanel = widget);
            Bind("Inventory.vacuumPanel", inventory, owner => owner.vacuumPanel, (owner, widget) => owner.vacuumPanel = widget);
            Bind("Inventory.arrow", inventory, owner => owner.arrow, (owner, widget) => owner.arrow = widget);
            BindWeaponIcons(inventory);
        }

        Plugin.Logger.LogInfo($"Player 2 HUD widgets bound: [{string.Join(", ", _boundNames)}].");
        if (_sharedNames.Count > 0)
        {
            Plugin.Logger.LogInfo($"Not on player 2's HUD copy, left shared: [{string.Join(", ", _sharedNames)}].");
        }
    }

    // A live player 2 must never point at the destroyed copy.
    public void Release()
    {
        _restores.ForEach(restore => restore());
        _restores.Clear();
        _boundNames.Clear();
        _sharedNames.Clear();

        if (_staminaBarMaterial != null)
        {
            UnityObject.Destroy(_staminaBarMaterial);
        }

        _staminaBarMaterial = null;
    }

    private void Bind<TOwner, TWidget>(string name, TOwner owner, Func<TOwner, TWidget> read, Action<TOwner, TWidget> write)
        where TOwner : UnityObject
        where TWidget : UnityObject
    {
        var source = read(owner);
        if (source == null)
        {
            return;
        }

        var copy = CopyOf(source);
        if (copy == null)
        {
            _sharedNames.Add(name);
            return;
        }

        Rebind(owner, source, copy, write);
        _boundNames.Add(name);
    }

    private void BindWeaponIcons(Inventory inventory)
    {
        var sourceIcons = inventory.weaponsIcon;
        if (sourceIcons == null || sourceIcons.Length == 0)
        {
            return;
        }

        // A new array, so player 1's array is never modified.
        var copyIcons = new Il2CppReferenceArray<Image>(sourceIcons.Length);
        var missingCount = 0;
        for (var index = 0; index < sourceIcons.Length; index++)
        {
            var sourceIcon = sourceIcons[index];
            var copyIcon = sourceIcon == null ? null : CopyOf(sourceIcon);
            copyIcons[index] = copyIcon;
            missingCount += sourceIcon != null && copyIcon == null ? 1 : 0;
        }

        // A mixed array would let player 2 drive player 1's icons.
        var name = $"Inventory.weaponsIcon ({sourceIcons.Length - missingCount}/{sourceIcons.Length})";
        if (missingCount > 0)
        {
            _sharedNames.Add(name);
            return;
        }

        Rebind(inventory, sourceIcons, copyIcons, (owner, icons) => owner.weaponsIcon = icons);
        _boundNames.Add(name);
    }

    private void Rebind<TOwner, TValue>(TOwner owner, TValue source, TValue copy, Action<TOwner, TValue> write)
        where TOwner : UnityObject
    {
        write(owner, copy);
        _restores.Add(() =>
        {
            if (owner != null)
            {
                write(owner, source);
            }
        });
    }

    private TWidget CopyOf<TWidget>(TWidget source) where TWidget : UnityObject
    {
        var copy = _findCopy(TransformOf(source));
        if (copy == null)
        {
            return null;
        }

        return typeof(TWidget) == typeof(GameObject) ? copy.gameObject.Cast<TWidget>() : copy.GetComponent<TWidget>();
    }

    private static Transform TransformOf(UnityObject widget) => widget switch
    {
        GameObject gameObject => gameObject.transform,
        Component component => component.transform,
        _ => null,
    };

    private static bool IsCustomMaterial(Image image) =>
        image.material != image.defaultMaterial && image.material != Image.defaultETC1GraphicMaterial;
}
