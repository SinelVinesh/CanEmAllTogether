using UnityEngine;

namespace CanEmAll.SplitScreenMod;

internal enum SplitScreenLayout
{
    Vertical,
    Horizontal,
    SeparateDisplays,
}

internal static class SplitScreenLayoutExtensions
{
    private static readonly Rect FullScreen = new(0f, 0f, 1f, 1f);
    private static readonly Rect LeftHalf = new(0f, 0f, 0.5f, 1f);
    private static readonly Rect RightHalf = new(0.5f, 0f, 0.5f, 1f);
    private static readonly Rect TopHalf = new(0f, 0.5f, 1f, 0.5f);
    private static readonly Rect BottomHalf = new(0f, 0f, 1f, 0.5f);

    public static Rect PlayerOneViewport(this SplitScreenLayout layout) => layout switch
    {
        SplitScreenLayout.Vertical => LeftHalf,
        SplitScreenLayout.Horizontal => TopHalf,
        _ => FullScreen,
    };

    public static Rect PlayerTwoViewport(this SplitScreenLayout layout) => layout switch
    {
        SplitScreenLayout.Vertical => RightHalf,
        SplitScreenLayout.Horizontal => BottomHalf,
        _ => FullScreen,
    };

    public static bool SharesOneScreen(this SplitScreenLayout layout) =>
        layout != SplitScreenLayout.SeparateDisplays;
}
