using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

// HUD copy for display 2, driven by player 2.
internal static class PlayerTwoHud
{
    private const string NoHudSuffix = "player 2 gets no HUD.";

    private static readonly string[] VisualNamespaces = { "UnityEngine", "TMPro" };

    private static Canvas _copy;
    private static PlayerTwoRig _rig;
    private static PlayerTwoHudBindings _bindings;
    private static PlayerTwoHudMirror _mirror;
    private static BulletCounter _boundBulletCounter;
    private static Transform _bulletCounterCopyRoot;
    private static Transform _vacuumBarCopyRoot;
    private static readonly HashSet<IntPtr> SkippedBulletCounterAwakes = new();

    public static bool IsSpawned => _copy != null;

    internal static void SkipBulletCounterAwake(BulletCounter counter)
    {
        if (counter != null)
        {
            SkippedBulletCounterAwakes.Add(counter.Pointer);
        }
    }

    internal static bool ConsumeSkippedBulletCounterAwake(BulletCounter counter) =>
        counter != null && SkippedBulletCounterAwakes.Remove(counter.Pointer);

    public static void Spawn(int targetDisplay, PlayerTwoRig rig)
    {
        Despawn();

        if (!TrySpawn(targetDisplay, rig))
        {
            Plugin.Logger.LogWarning("Both players keep sharing player 1's HUD.");
        }
    }

    public static void Despawn()
    {
        var rig = _rig;
        if (IsSpawned || _bulletCounterCopyRoot != null || _vacuumBarCopyRoot != null)
        {
            if (rig != null)
            {
                PlayerContext.Enter(rig);
            }

            try
            {
                _copy.gameObject.SetActive(false);
                UnityObject.Destroy(_copy.gameObject);
                if (_bulletCounterCopyRoot != null)
                {
                    _bulletCounterCopyRoot.gameObject.SetActive(false);
                    UnityObject.Destroy(_bulletCounterCopyRoot.gameObject);
                }

                if (_vacuumBarCopyRoot != null)
                {
                    _vacuumBarCopyRoot.gameObject.SetActive(false);
                    UnityObject.Destroy(_vacuumBarCopyRoot.gameObject);
                }
            }
            finally
            {
                if (rig != null)
                {
                    PlayerContext.Exit();
                }
            }

            Plugin.Logger.LogInfo("Player 2 HUD removed.");
        }

        if (rig != null)
        {
            UnbindHudSingletons(rig);
        }

        _bindings?.Release();
        _copy = null;
        _rig = null;
        _bindings = null;
        _mirror = null;
        _bulletCounterCopyRoot = null;
        _vacuumBarCopyRoot = null;
        SkippedBulletCounterAwakes.Clear();
    }

    private static bool TrySpawn(int targetDisplay, PlayerTwoRig rig)
    {
        var playerTwo = rig?.Player;
        if (playerTwo == null)
        {
            Plugin.Logger.LogWarning($"Player 2 is missing; {NoHudSuffix}");
            return false;
        }

        var source = FindHudCanvas(out var staminaBar);
        if (source == null)
        {
            return false;
        }

        var keptSubtrees = KeptSubtrees(source.transform, staminaBar.transform);
        if (keptSubtrees.Count == 0)
        {
            Plugin.Logger.LogWarning($"Stamina widget has no top-level subtree; {NoHudSuffix}");
            return false;
        }

        WarnAboutHiddenSource(source, keptSubtrees);
        var keptIndices = keptSubtrees.Select(subtree => subtree.GetSiblingIndex()).ToHashSet();
        _rig = rig;
        _copy = CreateCopy(source, keptIndices, targetDisplay, rig, out var strippedTypes);
        if (_copy == null)
        {
            return false;
        }

        Plugin.Logger.LogInfo(
            $"Player 2 HUD created on display {targetDisplay} from '{HierarchyPaths.Of(source.transform)}' " +
            $"keeping [{string.Join(", ", keptSubtrees.Select(subtree => subtree.name))}]; " +
            $"stripped [{string.Join(", ", strippedTypes.Distinct())}].");

        _bindings = new PlayerTwoHudBindings(widget => FindCopyOf(widget, source.transform, keptIndices));
        _bindings.BindBulletCounter(ResolveSourceBulletCounter(source.transform), rig.BulletCounter);
        _bindings.BindVacuumBar(VacuumBar.Instance, rig.VacuumBar);
        DetachHudPartCopies(rig);
        _bindings.BindStaminaBar(playerTwo);
        _bindings.BindWidgets(playerTwo, rig.Inventory);
        _mirror = _bindings.BuildSharedTextMirror();
        return true;
    }

    public static void RefreshSharedTexts() => _mirror?.Refresh();

    // Bound before activation so a late Awake runs as player 2.
    private static void BindHudSingletons(Transform copy, PlayerTwoRig rig)
    {
        var bulletCounter = _bulletCounterCopyRoot == null
            ? copy.GetComponentInChildren<BulletCounter>(true)
            : _bulletCounterCopyRoot.GetComponent<BulletCounter>();
        if (bulletCounter != null)
        {
            _boundBulletCounter = bulletCounter;
            rig.BulletCounter = bulletCounter;
        }

        rig.VacuumBar = FindVacuumBarCopy(copy);
        WarnIfMissing(rig.BulletCounter, "ammo counter");
        WarnIfMissing(rig.VacuumBar, "vacuum bar");
        WarnIfMissing(rig.Vacuum, "vacuum in player 2's rig");
    }

    private static VacuumBar FindVacuumBarCopy(Transform hudCopy)
    {
        var inHud = hudCopy.GetComponentInChildren<VacuumBar>(true);
        if (inHud != null)
        {
            return inHud;
        }

        var copyRoot = _vacuumBarCopyRoot != null ? _vacuumBarCopyRoot : _bulletCounterCopyRoot;
        return copyRoot == null ? null : copyRoot.GetComponentInChildren<VacuumBar>(true);
    }

    private static void WarnIfMissing(Component part, string label)
    {
        if (part == null)
        {
            Plugin.Logger.LogWarning($"No {label}; player 2 keeps using player 1's.");
        }
    }

    private static void UnbindHudSingletons(PlayerTwoRig rig)
    {
        if (rig.BulletCounter == _boundBulletCounter)
        {
            rig.BulletCounter = null;
        }

        _boundBulletCounter = null;
        rig.VacuumBar = null;
    }

    private static void DetachHudPartCopies(PlayerTwoRig rig)
    {
        PlayerContext.Enter(rig);
        try
        {
            foreach (var copyRoot in new[] { _bulletCounterCopyRoot, _vacuumBarCopyRoot })
            {
                if (copyRoot == null)
                {
                    continue;
                }

                copyRoot.SetParent(null, false);
                copyRoot.gameObject.SetActive(false);
            }
        }
        finally
        {
            PlayerContext.Exit();
        }
    }

    private static BulletCounter ResolveSourceBulletCounter(Transform hudRoot)
    {
        var current = BulletCounter.Instance;
        if (current != null)
        {
            return current;
        }

        return Resources.FindObjectsOfTypeAll<BulletCounter>()
            .FirstOrDefault(counter => counter != null);
    }

    private static bool HasVisualUnder(BulletCounter counter, Transform root) =>
        counter != null &&
        counter.visual != null &&
        counter.visual.transform.IsChildOf(root);

    private static Canvas FindHudCanvas(out Image staminaBar)
    {
        var player = Player.Instance;
        staminaBar = player == null ? null : player.staminaSlider;
        var canvas = staminaBar == null ? null : CanvasHierarchy.RootCanvasOf(staminaBar.transform);
        if (canvas == null)
        {
            Plugin.Logger.LogWarning($"HUD canvas not found; {NoHudSuffix}");
            return null;
        }

        // Other render modes ignore Canvas.targetDisplay.
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Plugin.Logger.LogWarning($"HUD canvas uses {canvas.renderMode}, not an overlay; {NoHudSuffix}");
            return null;
        }

        return canvas;
    }

    // Names can repeat, so the copy is matched by sibling indices.
    private static Transform FindCopyOf(Transform sourceWidget, Transform sourceRoot, HashSet<int> keptIndices)
    {
        var indices = CanvasHierarchy.SiblingIndicesBelow(sourceRoot, sourceWidget);
        if (indices == null || indices.Count == 0)
        {
            return null;
        }

        indices[0] = keptIndices.OrderBy(index => index).ToList().IndexOf(indices[0]);
        return CanvasHierarchy.ChildAt(_copy.transform, indices);
    }

    private static List<Transform> KeptSubtrees(Transform root, Transform staminaWidget)
    {
        var subtrees = new List<Transform>();
        var gameplaySubtree = CanvasHierarchy.TopLevelChildOf(root, staminaWidget);
        if (gameplaySubtree == null)
        {
            return subtrees;
        }

        subtrees.Add(gameplaySubtree);

        var bulletCounter = ResolveSourceBulletCounter(root);

        var counterSubtree = bulletCounter == null
            ? null
            : CanvasHierarchy.TopLevelChildOf(root, bulletCounter.transform);
        var ammoVisual = bulletCounter == null ? null : bulletCounter.visual;
        var ammoSubtree = ammoVisual == null
            ? null
            : CanvasHierarchy.TopLevelChildOf(root, ammoVisual.transform);
        if (counterSubtree == null && ammoSubtree == null)
        {
            Plugin.Logger.LogWarning("Ammo counter visual not found on the HUD canvas; player 2 keeps using player 1's.");
        }
        else
        {
            if (counterSubtree != null && !subtrees.Contains(counterSubtree))
            {
                subtrees.Add(counterSubtree);
            }

            if (ammoSubtree != null && !subtrees.Contains(ammoSubtree))
            {
                subtrees.Add(ammoSubtree);
            }
        }

        var moneyManager = MoneyManager.Instance;
        var moneyText = moneyManager == null ? null : moneyManager.moneyText;
        var moneySubtree = moneyText == null ? null : CanvasHierarchy.TopLevelChildOf(root, moneyText.transform);
        if (moneySubtree == null)
        {
            Plugin.Logger.LogWarning("Money widget not found on the HUD canvas; player 2's HUD shows no money.");
        }
        else if (moneySubtree != gameplaySubtree)
        {
            subtrees.Add(moneySubtree);
        }

        return subtrees;
    }

    private static void WarnAboutHiddenSource(Canvas source, List<Transform> subtrees)
    {
        if (!source.enabled || !source.gameObject.activeInHierarchy)
        {
            Plugin.Logger.LogWarning("HUD canvas is hidden, so player 2's copy starts hidden.");
            return;
        }

        foreach (var subtree in subtrees.Where(subtree => !subtree.gameObject.activeSelf))
        {
            Plugin.Logger.LogWarning($"HUD subtree '{subtree.name}' is hidden, so player 2's copy starts hidden.");
        }
    }

    private static Canvas CreateCopy(
        Canvas source,
        HashSet<int> keptIndices,
        int targetDisplay,
        PlayerTwoRig rig,
        out List<string> strippedTypes)
    {
        // An inactive parent defers Awake until the copy is stripped.
        var holder = CloneParts.CreateInactiveHolder("HudHolder");

        try
        {
            var copy = UnityObject.Instantiate(source.gameObject, holder.transform, false).transform;
            copy.name = CloneParts.PlayerTwoName(source.name);
            RemoveSubtreesExcept(copy, keptIndices);
            HidePopUps(copy);
            strippedTypes = StripGameBehaviours(copy, out var survivorCount);

            // A surviving game script would run its Awake on the copy.
            if (survivorCount > 0)
            {
                Plugin.Logger.LogWarning($"{survivorCount} game script(s) could not be stripped; {NoHudSuffix}");
                return null;
            }

            FreezeUiBehaviours(copy);
            CreateBulletCounterCopy(ResolveSourceBulletCounter(source.transform), holder.transform);
            CreateVacuumBarCopy(VacuumBar.Instance, holder.transform);
            var canvas = copy.GetComponent<Canvas>();
            canvas.targetDisplay = targetDisplay;
            foreach (var nestedCanvas in copy.GetComponentsInChildren<Canvas>(true))
            {
                nestedCanvas.targetDisplay = targetDisplay;
            }
            BindHudSingletons(copy, rig);
            Activate(copy, rig);
            return canvas;
        }
        finally
        {
            UnityObject.Destroy(holder);
        }
    }

    private static void CreateBulletCounterCopy(BulletCounter source, Transform holder)
    {
        if (source == null)
        {
            return;
        }

        var copy = UnityObject.Instantiate(source.gameObject, holder, false).transform;
        copy.name = CloneParts.PlayerTwoName(source.gameObject.name);
        copy.gameObject.SetActive(false);
        _bulletCounterCopyRoot = copy;
        SkipBulletCounterAwake(copy.GetComponent<BulletCounter>());
    }

    // Skipped when the bar already lives in a copied object.
    private static void CreateVacuumBarCopy(VacuumBar source, Transform holder)
    {
        var alreadyCopied = _bulletCounterCopyRoot != null &&
            _bulletCounterCopyRoot.GetComponentInChildren<VacuumBar>(true) != null;
        if (source == null || alreadyCopied)
        {
            return;
        }

        var copy = UnityObject.Instantiate(source.gameObject, holder, false).transform;
        copy.name = CloneParts.PlayerTwoName(source.gameObject.name);
        copy.gameObject.SetActive(false);
        _vacuumBarCopyRoot = copy;
    }

    private static void Activate(Transform copy, PlayerTwoRig rig)
    {
        var counterVisual = rig.BulletCounter == null ? null : rig.BulletCounter.visual;
        var counterWasVisible = counterVisual != null && counterVisual.activeSelf;

        try
        {
            copy.SetParent(null, false);
        }
        catch
        {
            // Out of the holder, so its Destroy no longer covers it.
            UnbindHudSingletons(rig);
            UnityObject.Destroy(copy.gameObject);
            throw;
        }

        // BulletCounter.Awake hides it until the next weapon switch.
        if (counterVisual != null)
        {
            counterVisual.SetActive(counterWasVisible);
        }
    }

    private static void RemoveSubtreesExcept(Transform root, HashSet<int> keptIndices)
    {
        for (var index = root.childCount - 1; index >= 0; index--)
        {
            if (!keptIndices.Contains(index))
            {
                UnityObject.DestroyImmediate(root.GetChild(index).gameObject);
            }
        }
    }

    // In-flight pop-ups would otherwise stay on screen forever.
    private static void HidePopUps(Transform root)
    {
        foreach (var popUp in root.GetComponentsInChildren<FloatingText>(true))
        {
            popUp.gameObject.SetActive(false);
        }
    }

    // RequireComponent may block removal until its dependent goes.
    private static List<string> StripGameBehaviours(Transform root, out int survivorCount)
    {
        var strippedTypes = new List<string>();
        var remaining = GameBehaviours(root);
        while (remaining.Count > 0)
        {
            var removedCount = 0;
            foreach (var behaviour in remaining)
            {
                if (TryDestroy(behaviour, strippedTypes))
                {
                    removedCount++;
                }
            }

            if (removedCount == 0)
            {
                break;
            }

            remaining = GameBehaviours(root);
        }

        foreach (var survivor in remaining)
        {
            Plugin.Logger.LogWarning($"Could not strip {TypeNameOf(survivor)} from player 2's HUD.");
        }

        survivorCount = remaining.Count;
        return strippedTypes;
    }

    private static List<MonoBehaviour> GameBehaviours(Transform root) =>
        root.GetComponentsInChildren<MonoBehaviour>(true)
            .Where(behaviour => behaviour != null && !IsVisual(behaviour) && !IsPlayerTwoHudScript(behaviour))
            .ToList();

    // These only display what player 2's code pushes to them.
    private static bool IsPlayerTwoHudScript(MonoBehaviour behaviour) =>
        behaviour.TryCast<BulletCounter>() != null || behaviour.TryCast<VacuumBar>() != null;

    private static bool TryDestroy(MonoBehaviour behaviour, List<string> strippedTypes)
    {
        var typeName = TypeNameOf(behaviour);
        UnityObject.DestroyImmediate(behaviour);
        if (behaviour != null)
        {
            return false;
        }

        strippedTypes.Add(typeName);
        return true;
    }

    private static bool IsVisual(MonoBehaviour behaviour)
    {
        var typeNamespace = behaviour.GetIl2CppType().Namespace;
        return typeNamespace != null && VisualNamespaces.Any(visualNamespace =>
            typeNamespace == visualNamespace ||
            typeNamespace.StartsWith(visualNamespace + ".", StringComparison.Ordinal));
    }

    private static string TypeNameOf(MonoBehaviour behaviour) => behaviour.GetIl2CppType().Name;

    // Disabled before activation so none start or register.
    private static void FreezeUiBehaviours(Transform root)
    {
        CloneParts.DisableAll<GraphicRaycaster>(root);
        CloneParts.DisableAll<Selectable>(root);
        CloneParts.DisableAll<EventSystem>(root);
        CloneParts.DisableAll<BaseInputModule>(root);
        CloneParts.DisableAll<Animator>(root);
        CloneParts.DisableAll<Animation>(root);
        CloneParts.DisableAll<AudioSource>(root);
    }
}
