namespace CanEmAll.SplitScreenMod.PlayerTwo;

// Swaps the game's singletons while player 2 code runs.
internal static class PlayerContext
{
    private static int _depth;
    private static SingletonSnapshot _savedSingletons;
    private static bool _activationLogged;

    public static bool IsPlayerTwo => _depth > 0;

    public static void Enter(PlayerTwoRig rig)
    {
        if (_depth++ > 0)
        {
            return;
        }

        if (!_activationLogged)
        {
            _activationLogged = true;
            Plugin.Logger.LogInfo("Player 2 context is active; gamepad input is routed to player 2.");
        }

        _savedSingletons = SingletonSnapshot.Capture();
        SingletonSnapshot.Of(rig).Apply();
    }

    public static void Exit()
    {
        if (_depth == 0 || --_depth > 0)
        {
            return;
        }

        _savedSingletons.Apply();
        _savedSingletons = default;
    }

    // An exception skips the postfix and leaks the context.
    public static void ResetIfLeaked()
    {
        if (_depth == 0)
        {
            return;
        }

        Plugin.Logger.LogWarning("Player 2 context leaked after an exception; restoring player 1.");
        _depth = 1;
        Exit();
    }
}
