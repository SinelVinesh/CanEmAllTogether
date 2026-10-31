using CanEmAll.SplitScreenMod.Gamepad;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

internal static class PlayerTwoSession
{
    private const float SpawnSideOffset = 1.5f;

    private static readonly Dictionary<IntPtr, bool> OwnershipCache = new();

    public static GamepadState Gamepad { get; private set; }
    public static PlayerTwoRig Rig { get; private set; }
    public static bool IsSpawned => Rig != null && Rig.IsAlive;

    public static void Initialize(int gamepadIndex)
    {
        Gamepad = new GamepadState(gamepadIndex);
    }

    public static bool Owns(Component component)
    {
        if (!IsSpawned)
        {
            return false;
        }

        // HUD parts sit outside the rig roots.
        if (Rig.OwnsHudPart(component))
        {
            return true;
        }

        var key = component.Pointer;
        if (!OwnershipCache.TryGetValue(key, out var owned))
        {
            owned = Rig.Contains(component.transform);
            OwnershipCache[key] = owned;
        }

        return owned;
    }

    public static PlayerTwoRig Spawn(Player playerOne)
    {
        Despawn();

        var playerOneSingletons = SingletonSnapshot.Capture();
        var sourceRoots = FindRigRoots(playerOne, playerOneSingletons.Inventory);
        var cloneRoots = CloneTogether(sourceRoots, playerOne.transform.right * SpawnSideOffset, out var cloneTemplate);

        var cloneHolder = cloneTemplate.parent.gameObject;
        Rig = new PlayerTwoRig(
            cloneRoots,
            cloneTemplate.GetComponentInChildren<Player>(true),
            cloneTemplate.GetComponentInChildren<Inventory>(true),
            cloneTemplate.GetComponentInChildren<EZCameraShake.CameraShaker>(true),
            cloneTemplate.GetComponentInChildren<Vacuum>(true),
            cloneTemplate.GetComponentInChildren<BulletCounter>(true));
        PrepareCloneForActivation(cloneTemplate);

        // Moving clones out of the inactive holder runs their Awake.
        for (var i = 0; i < cloneRoots.Count; i++)
        {
            cloneRoots[i].SetParent(sourceRoots[i].parent, true);
        }

        UnityObject.Destroy(cloneHolder);
        playerOneSingletons.Apply();

        Plugin.Logger.LogInfo(
            $"Player 2 spawned from [{string.Join(", ", sourceRoots.Select(HierarchyPaths.Of))}] " +
            $"(inventory cloned: {Rig.Inventory != null}, camera: '{Rig.Camera?.name}').");
        return Rig;
    }

    public static void Despawn()
    {
        if (IsSpawned)
        {
            var roots = Rig.Roots.Where(root => root != null).ToList();
            DeactivateAsPlayerTwo(roots);
            roots.ForEach(root => UnityObject.Destroy(root.gameObject));
            Plugin.Logger.LogInfo("Player 2 removed.");
        }

        Rig = null;
        OwnershipCache.Clear();
        PlayerTwoSprint.Reset();
    }

    // Deferred Destroy would run OnDisable as player 1.
    private static void DeactivateAsPlayerTwo(List<Transform> roots)
    {
        PlayerContext.Enter(Rig);
        try
        {
            roots.ForEach(root => root.gameObject.SetActive(false));
        }
        finally
        {
            PlayerContext.Exit();
        }
    }

    // One Instantiate call keeps cross-root references intact.
    private static List<Transform> CloneTogether(List<Transform> sourceRoots, Vector3 offset, out Transform cloneTemplate)
    {
        var originalParents = sourceRoots.Select(root => root.parent).ToList();
        var template = new GameObject(CloneParts.ModObjectName("RigTemplate")).transform;
        sourceRoots.ForEach(root => root.SetParent(template, true));

        // An inactive parent defers Awake until ownership is known.
        var holder = CloneParts.CreateInactiveHolder("SpawnHolder");
        cloneTemplate = UnityObject.Instantiate(template.gameObject, offset, Quaternion.identity, holder.transform).transform;

        for (var i = 0; i < sourceRoots.Count; i++)
        {
            sourceRoots[i].SetParent(originalParents[i], true);
        }

        UnityObject.Destroy(template.gameObject);

        var cloneRoots = new List<Transform>();
        for (var i = 0; i < cloneTemplate.childCount; i++)
        {
            var cloneRoot = cloneTemplate.GetChild(i);
            cloneRoot.name = CloneParts.PlayerTwoName(sourceRoots[i].name);
            cloneRoots.Add(cloneRoot);
        }

        return cloneRoots;
    }

    private static List<Transform> FindRigRoots(Player playerOne, Inventory playerOneInventory)
    {
        var roots = RigParts(playerOne, playerOneInventory).Select(RigRootOf).ToList();
        return roots
            .Where((root, index) => roots.FindIndex(other => other == root) == index)
            .Where(root => !roots.Exists(other => other != root && root.IsChildOf(other)))
            .ToList();
    }

    private static IEnumerable<Transform> RigParts(Player playerOne, Inventory playerOneInventory)
    {
        yield return playerOne.transform;

        var optionalParts = new Component[]
        {
            playerOne.mainCamera,
            playerOne.mainCameraParent,
            playerOne.playerHand,
            playerOneInventory,
        };

        foreach (var part in optionalParts.Where(part => part != null))
        {
            yield return part.transform;
        }
    }

    // Highest ancestor that does not also hold the game systems.
    private static Transform RigRootOf(Transform part)
    {
        var root = part;
        while (root.parent != null && !ContainsGameManager(root.parent))
        {
            root = root.parent;
        }

        return root;
    }

    private static bool ContainsGameManager(Transform root) =>
        GameManager.Instance != null && GameManager.Instance.transform.IsChildOf(root);

    private static void PrepareCloneForActivation(Transform cloneTemplate)
    {
        CloneParts.DisableAll<AudioListener>(cloneTemplate);
        RemoveOutlineEffects(cloneTemplate);
        RenameCameraShakers(cloneTemplate);
    }

    // A clone's OnDestroy would free player 1's shared textures.
    private static void RemoveOutlineEffects(Transform root)
    {
        foreach (var outlineEffect in root.GetComponentsInChildren<cakeslice.OutlineEffect>(true))
        {
            UnityObject.DestroyImmediate(outlineEffect);
        }
    }

    // CameraShaker registers itself in a dictionary by object name.
    private static void RenameCameraShakers(Transform root)
    {
        foreach (var shaker in root.GetComponentsInChildren<EZCameraShake.CameraShaker>(true))
        {
            shaker.gameObject.name = CloneParts.PlayerTwoName(shaker.gameObject.name);
        }
    }
}
