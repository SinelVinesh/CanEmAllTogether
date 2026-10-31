using EZCameraShake;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

// The game's single-player singletons that each player owns.
internal readonly struct SingletonSnapshot
{
    private readonly Player _player;
    private readonly Inventory _inventory;
    private readonly CameraShaker _cameraShaker;
    private readonly Vacuum _vacuum;
    private readonly BulletCounter _bulletCounter;
    private readonly VacuumBar _vacuumBar;

    private SingletonSnapshot(
        Player player,
        Inventory inventory,
        CameraShaker cameraShaker,
        Vacuum vacuum,
        BulletCounter bulletCounter,
        VacuumBar vacuumBar)
    {
        _player = player;
        _inventory = inventory;
        _cameraShaker = cameraShaker;
        _vacuum = vacuum;
        _bulletCounter = bulletCounter;
        _vacuumBar = vacuumBar;
    }

    public Inventory Inventory => _inventory;
    public BulletCounter BulletCounter => _bulletCounter;

    public static SingletonSnapshot Capture() =>
        new(Player.Instance, Inventory.Instance, CameraShaker.Instance, Vacuum.Instance, BulletCounter.Instance, VacuumBar.Instance);

    // Missing parts keep player 1's, so code never sees null.
    public static SingletonSnapshot Of(PlayerTwoRig rig) =>
        new(
            rig.Player,
            OrCurrent(rig.Inventory, Inventory.Instance),
            OrCurrent(rig.CameraShaker, CameraShaker.Instance),
            OrCurrent(rig.Vacuum, Vacuum.Instance),
            OrCurrent(rig.BulletCounter, BulletCounter.Instance),
            OrCurrent(rig.VacuumBar, VacuumBar.Instance));

    public void Apply()
    {
        Player.Instance = _player;
        Inventory.Instance = _inventory;
        CameraShaker.Instance = _cameraShaker;
        Vacuum.Instance = _vacuum;
        BulletCounter.Instance = _bulletCounter;
        VacuumBar.Instance = _vacuumBar;
    }

    private static T OrCurrent<T>(T playerTwoPart, T current) where T : UnityEngine.Object =>
        playerTwoPart == null ? current : playerTwoPart;
}
