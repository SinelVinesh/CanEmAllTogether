using System.Text;
using CanEmAll.SplitScreenMod.PlayerTwo;
using UnityEngine;
using UnityEngine.UI;

namespace CanEmAll.SplitScreenMod.Diagnostics;

// Read-only on purpose: it must never alter game state.
internal static class HudReport
{
    private const string Missing = "<none>";
    private const string NotApplicable = "n/a";

    private static readonly string[] CrosshairNameHints = { "cross", "reticle", "aim", "dot" };

    private readonly record struct WidgetRow(string Name, Transform Widget);

    public static void Log()
    {
        var report = new StringBuilder("HUD report").AppendLine();
        AppendSection(report, "Canvases", AppendCanvases);
        AppendSection(report, "Player widgets", AppendPlayerWidgets);
        AppendSection(report, "HUD singleton widgets", AppendSingletonWidgets);
        AppendSection(report, "Crosshair candidates", AppendCrosshairCandidates);
        Plugin.Logger.LogInfo(report.ToString());
    }

    // One failing section must not discard the others.
    private static void AppendSection(StringBuilder report, string title, Action<StringBuilder> append)
    {
        try
        {
            append(report);
        }
        catch (Exception exception)
        {
            report.AppendLine($"{title}: <section failed: {exception.Message}>");
            Plugin.Logger.LogError($"HUD report section '{title}' failed: {exception}");
        }
    }

    private static void AppendCanvases(StringBuilder report)
    {
        report.AppendLine("Canvases:");
        foreach (var canvas in SceneObjects<Canvas>())
        {
            var camera = canvas.worldCamera;
            report.AppendLine(
                $"  {HierarchyPaths.Of(canvas.transform)} | active: {canvas.gameObject.activeInHierarchy} | " +
                $"enabled: {canvas.enabled} | mode: {canvas.renderMode} | display: {canvas.targetDisplay} | " +
                $"camera: {(camera == null ? Missing : camera.name)} | order: {canvas.sortingOrder} | " +
                $"nested: {CanvasHierarchy.RootCanvasOf(canvas.transform) != canvas}{PlayerTwoRigTag(canvas)}");
        }
    }

    private static void AppendPlayerWidgets(StringBuilder report)
    {
        var playerOne = Player.Instance;
        var playerOneInventory = Inventory.Instance;
        AppendOwners(report, "Player 1", playerOne, playerOneInventory);
        WarnIfOwnedByPlayerTwo(report, "Player.Instance", playerOne);
        WarnIfOwnedByPlayerTwo(report, "Inventory.Instance", playerOneInventory);

        var playerOneWidgets = PlayerWidgets(playerOne, playerOneInventory);
        AppendRows(report, "Player 1 widgets:", playerOneWidgets);

        if (!PlayerTwoSession.IsSpawned)
        {
            report.AppendLine("Player 2 widgets: player 2 not spawned");
            return;
        }

        var rig = PlayerTwoSession.Rig;
        AppendOwners(report, "Player 2", rig.Player, rig.Inventory);
        var playerTwoWidgets = PlayerWidgets(rig.Player, rig.Inventory);
        AppendRows(
            report,
            "Player 2 widgets:",
            playerTwoWidgets,
            index => $" | same object as P1: {SameObjectLabel(playerOneWidgets[index].Widget, playerTwoWidgets[index].Widget)}");
    }

    private static void AppendSingletonWidgets(StringBuilder report)
    {
        AppendRows(report, "HUD singleton widgets:", SingletonWidgets());
        AppendInstanceCount<BulletCounter>(report);
        AppendInstanceCount<VacuumBar>(report);

        if (PlayerTwoSession.IsSpawned)
        {
            var rig = PlayerTwoSession.Rig;
            AppendRows(report, "Player 2 singletons:", new[]
            {
                Row("BulletCounter", rig.BulletCounter, owner => owner),
                Row("VacuumBar", rig.VacuumBar, owner => owner),
                Row("Vacuum", rig.Vacuum, owner => owner),
            });
        }
    }

    private static void AppendCrosshairCandidates(StringBuilder report)
    {
        var candidates = SceneObjects<Graphic>()
            .Where(graphic => HasCrosshairName(graphic.name))
            .Where(graphic => CanvasHierarchy.RootCanvasOf(graphic.transform) != null)
            .Select(graphic => new WidgetRow(graphic.GetIl2CppType().Name, graphic.transform));
        AppendRows(report, "Crosshair candidates:", candidates);
    }

    private static WidgetRow[] PlayerWidgets(Player player, Inventory inventory) => new[]
    {
        Row("Player.staminaSlider", player, owner => owner.staminaSlider),
        Row("Player.grabDropUI", player, owner => owner.grabDropUI),
        Row("Player.grabDropText", player, owner => owner.grabDropText),
        Row("Player.throwUI", player, owner => owner.throwUI),
        Row("Player.rotateUI", player, owner => owner.rotateUI),
        Row("Inventory.switchIcon", inventory, owner => owner.switchIcon),
        Row("Inventory.weaponsPanel", inventory, owner => owner.weaponsPanel),
        Row("Inventory.vacuumPanel", inventory, owner => owner.vacuumPanel),
        Row("Inventory.arrow", inventory, owner => owner.arrow),
    };

    private static WidgetRow[] SingletonWidgets()
    {
        var bulletCounter = BulletCounter.Instance;
        var vacuumBar = VacuumBar.Instance;
        var moneyManager = MoneyManager.Instance;
        var gameManager = GameManager.Instance;

        return new[]
        {
            Row("BulletCounter.visual", bulletCounter, owner => owner.visual),
            Row("BulletCounter.title", bulletCounter, owner => owner.title),
            Row("BulletCounter.text", bulletCounter, owner => owner.text),
            Row("BulletCounter.icon", bulletCounter, owner => owner.icon),
            Row("VacuumBar.visual", vacuumBar, owner => owner.visual),
            Row("VacuumBar.enemyBar", vacuumBar, owner => owner.enemyBar),
            Row("VacuumBar.enemyCountText", vacuumBar, owner => owner.enemyCountText),
            Row("VacuumBar.canBar", vacuumBar, owner => owner.canBar),
            Row("VacuumBar.canCountText", vacuumBar, owner => owner.canCountText),
            Row("VacuumBar.currentCountText", vacuumBar, owner => owner.currentCountText),
            Row("VacuumBar.totalCountText", vacuumBar, owner => owner.totalCountText),
            Row("MoneyManager.moneyText", moneyManager, owner => owner.moneyText),
            Row("GameManager.timerText", gameManager, owner => owner.timerText),
            Row("GameManager.dayCountText", gameManager, owner => owner.dayCountText),
            Row("GameManager.creaturesKilledCountText", gameManager, owner => owner.creaturesKilledCountText),
            Row("GameManager.cannedCountText", gameManager, owner => owner.cannedCountText),
        };
    }

    private static WidgetRow Row<TOwner>(string name, TOwner owner, Func<TOwner, Component> widget)
        where TOwner : UnityEngine.Object
    {
        if (owner == null)
        {
            return new WidgetRow(name, null);
        }

        var component = widget(owner);
        return new WidgetRow(name, component == null ? null : component.transform);
    }

    private static WidgetRow Row<TOwner>(string name, TOwner owner, Func<TOwner, GameObject> widget)
        where TOwner : UnityEngine.Object =>
        Row(name, owner, current =>
        {
            var gameObject = widget(current);
            return gameObject == null ? null : gameObject.transform;
        });

    private static void AppendOwners(StringBuilder report, string label, Player player, Inventory inventory) =>
        report.AppendLine($"{label}: Player at {PathOrMissing(player)}, Inventory at {PathOrMissing(inventory)}");

    private static void WarnIfOwnedByPlayerTwo(StringBuilder report, string name, Component component)
    {
        if (component != null && PlayerTwoSession.Owns(component))
        {
            report.AppendLine($"  WARNING: {name} is player 2's clone, so the P1 rows are wrong.");
        }
    }

    private static void AppendInstanceCount<T>(StringBuilder report) where T : Component =>
        report.AppendLine($"  {typeof(T).Name} instances in scene: {SceneObjects<T>().Count()}");

    private static void AppendRows(
        StringBuilder report,
        string title,
        IEnumerable<WidgetRow> rows,
        Func<int, string> suffix = null)
    {
        report.AppendLine(title);
        var index = 0;
        foreach (var row in rows)
        {
            report.AppendLine($"{Describe(row)}{suffix?.Invoke(index)}");
            index++;
        }
    }

    private static string Describe(WidgetRow row)
    {
        if (row.Widget == null)
        {
            return $"  {row.Name}: {Missing}";
        }

        var rootCanvas = CanvasHierarchy.RootCanvasOf(row.Widget);
        var canvasPath = rootCanvas == null ? Missing : HierarchyPaths.Of(rootCanvas.transform);
        return $"  {row.Name}: {HierarchyPaths.Of(row.Widget)} | canvas: {canvasPath} | " +
               $"active: {row.Widget.gameObject.activeInHierarchy}{PlayerTwoRigTag(row.Widget)}";
    }

    private static string PlayerTwoRigTag(Component component) =>
        PlayerTwoSession.Owns(component) ? " | in P2 rig" : string.Empty;

    private static string PathOrMissing(Component component) =>
        component == null ? Missing : HierarchyPaths.Of(component.transform);

    private static string SameObjectLabel(Transform first, Transform second) =>
        first == null || second == null ? NotApplicable : (first.Pointer == second.Pointer).ToString();

    private static bool HasCrosshairName(string name) =>
        CrosshairNameHints.Any(hint => name.Contains(hint, StringComparison.OrdinalIgnoreCase));

    // Resources also returns inactive objects and prefabs.
    private static IEnumerable<T> SceneObjects<T>() where T : Component =>
        Resources.FindObjectsOfTypeAll<T>().Where(component => component.gameObject.scene.IsValid());
}
