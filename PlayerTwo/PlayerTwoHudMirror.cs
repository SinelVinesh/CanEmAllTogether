using TMPro;
using UnityEngine;

namespace CanEmAll.SplitScreenMod.PlayerTwo;

// Money and timer are shared, so player 2's copy follows player 1's.
internal sealed class PlayerTwoHudMirror
{
    private readonly List<(TMP_Text Source, TMP_Text Copy)> _pairs = new();

    public int Count => _pairs.Count;

    public void Add(TMP_Text source, TMP_Text copy)
    {
        if (source != null && copy != null)
        {
            _pairs.Add((source, copy));
        }
    }

    public void Refresh()
    {
        foreach (var (source, copy) in _pairs)
        {
            if (source != null && copy != null)
            {
                Sync(source, copy);
            }
        }
    }

    private static void Sync(TMP_Text source, TMP_Text copy)
    {
        var sourceObject = source.gameObject;
        if (copy.gameObject.activeSelf != sourceObject.activeSelf)
        {
            copy.gameObject.SetActive(sourceObject.activeSelf);
        }

        var sourceText = source.text;
        if (copy.text != sourceText)
        {
            copy.text = sourceText;
        }

        copy.color = source.color;
        copy.transform.localScale = source.transform.localScale;
    }
}
