using UnityEngine;

namespace CanEmAll.SplitScreenMod;

internal static class CanvasHierarchy
{
    // Canvas.rootCanvas is unreliable on inactive canvases.
    public static Canvas RootCanvasOf(Transform transform)
    {
        var canvases = transform.GetComponentsInParent<Canvas>(true);
        return canvases.Length == 0 ? null : canvases[canvases.Length - 1];
    }

    public static Transform TopLevelChildOf(Transform root, Transform descendant)
    {
        var current = descendant;
        while (current != null && current.parent != root)
        {
            current = current.parent;
        }

        return current;
    }

    public static List<int> SiblingIndicesBelow(Transform root, Transform descendant)
    {
        var indices = new List<int>();
        var current = descendant;
        while (current != null && current != root)
        {
            indices.Insert(0, current.GetSiblingIndex());
            current = current.parent;
        }

        return current == root ? indices : null;
    }

    public static Transform ChildAt(Transform root, IEnumerable<int> siblingIndices)
    {
        var current = root;
        foreach (var index in siblingIndices)
        {
            if (index < 0 || index >= current.childCount)
            {
                return null;
            }

            current = current.GetChild(index);
        }

        return current;
    }
}
