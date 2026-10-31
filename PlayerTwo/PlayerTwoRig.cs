using EZCameraShake;
using UnityEngine;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

internal sealed class PlayerTwoRig
{
    public PlayerTwoRig(
        List<Transform> roots,
        Player player,
        Inventory inventory,
        CameraShaker cameraShaker,
        Vacuum vacuum,
        BulletCounter bulletCounter)
    {
        Roots = roots;
        Player = player;
        Inventory = inventory;
        CameraShaker = cameraShaker;
        Vacuum = vacuum;
        BulletCounter = bulletCounter;
    }

    public List<Transform> Roots { get; }
    public Player Player { get; }
    public Inventory Inventory { get; }
    public CameraShaker CameraShaker { get; }
    public Vacuum Vacuum { get; }

    public BulletCounter BulletCounter { get; set; }
    public VacuumBar VacuumBar { get; set; }

    public Camera Camera => Player.mainCamera;
    public bool IsAlive => Player != null;

    public bool Contains(Transform transform) =>
        Roots.Exists(root => root != null && transform.IsChildOf(root));

    public bool OwnsHudPart(Component component) =>
        IsSame(component, BulletCounter) || IsSame(component, VacuumBar);

    private static bool IsSame(Component component, Component hudPart) =>
        hudPart != null && component.Pointer == hudPart.Pointer;
}
