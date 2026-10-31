using UnityEngine;

namespace CanEmAll.SplitScreenMod;

internal static class HierarchyPaths
{
    public static string Of(Transform transform) => PathBelow(null, transform);

    public static string RelativeTo(Transform root, Transform descendant) => PathBelow(root, descendant);

    private static string PathBelow(Transform root, Transform transform) =>
        transform.parent == null || transform.parent == root
            ? transform.name
            : $"{PathBelow(root, transform.parent)}/{transform.name}";
}
