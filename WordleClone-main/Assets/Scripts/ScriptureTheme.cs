using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Attach to the Canvas.  Applies the SCRIPTURE visual theme at runtime on Awake().
/// Works alongside GameManager — does NOT change game logic.
/// </summary>
[ExecuteAlways]                 // also runs in Edit mode for instant preview
[RequireComponent(typeof(Canvas))]
public class ScriptureTheme : MonoBehaviour
{
    // ── Inspector-assignable sprites (drag in from Assets/) ──────────────────

    [Header("Sprites  (assign via Inspector)")]
    public Sprite tileSprite;   // tile_rounded  (white 9-slice)
    public Sprite keySprite;    // key_rounded   (tan 9-slice)

    // ── Palette ───────────────────────────────────────────────────────────────

    [Header("Palette")]
    [ColorUsage(false)] public Color bgColor         = Hex("F2EBDA");
    [ColorUsage(false)] public Color tileEmptyColor  = Hex("FFFFFF");
    [ColorUsage(false)] public Color tileGreyColor   = Hex("706B63");
    [ColorUsage(false)] public Color tileYellowColor = Hex("B59F3B");
    [ColorUsage(false)] public Color tileGreenColor  = Hex("4E7A3A");
    [ColorUsage(false)] public Color keyDefaultColor = Hex("CFC9BC");
    [ColorUsage(false)] public Color keyGreyColor    = Hex("7D7469");
    [ColorUsage(false)] public Color keyYellowColor  = Hex("B59F3B");
    [ColorUsage(false)] public Color keyGreenColor   = Hex("4E7A3A");
    [ColorUsage(false)] public Color textDark        = Hex("2C2924");
    [ColorUsage(false)] public Color textGold        = Hex("C99A33");
    [ColorUsage(false)] public Color textMid         = Hex("6B6355");

    // ── Key name set ──────────────────────────────────────────────────────────

    static readonly HashSet<string> KEY_NAMES = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "q","w","e","r","t","y","u","i","o","p",
        "a","s","d","f","g","h","j","k","l",
        "z","x","c","v","b","n","m",
        "enter","del","backspace"
    };

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Application.isPlaying) ApplyTheme();
    }

#if UNITY_EDITOR
    void OnValidate() => ApplyTheme();
#endif

    public void ApplyTheme()
    {
        // 1. Canvas / root background
        if (TryGetComponent(out Image rootImg))
        {
            rootImg.color = bgColor;
            rootImg.raycastTarget = false;
        }

        // 2. Walk children
        foreach (Image img in GetComponentsInChildren<Image>(true))
            StyleImage(img);

        foreach (TextMeshProUGUI tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            StyleText(tmp);
    }

    // ── Image styling ─────────────────────────────────────────────────────────

    void StyleImage(Image img)
    {
        string n  = img.gameObject.name;
        string pn = img.transform.parent?.name ?? "";

        // Background / outer panels
        if (n is "Canvas" or "Panel" or "GamePanel" or "Panel (2)" or "Background" or "BG")
        {
            img.color = bgColor; return;
        }

        // Keyboard keys  (Image is the direct component on the key GO)
        if (KEY_NAMES.Contains(n) && img.transform == img.gameObject.transform)
        {
            if (keySprite) { img.sprite = keySprite; img.type = Image.Type.Sliced; }
            img.color = keyDefaultColor;
            return;
        }

        // Tile images (identified by approx size ~50-70 px)
        if (IsTileImage(img))
        {
            if (tileSprite) { img.sprite = tileSprite; img.type = Image.Type.Sliced; }
            img.color = tileEmptyColor;
            return;
        }
    }

    bool IsTileImage(Image img)
    {
        // Tile is the Image on a tile GameObject inside a word row
        if (!IsInsideWordRow(img.transform)) return false;
        RectTransform rt = img.GetComponent<RectTransform>();
        if (rt == null) return false;
        float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
        return w >= 40 && w <= 80 && h >= 40 && h <= 80;
    }

    static bool IsInsideWordRow(Transform t)
    {
        Transform p = t;
        while (p != null)
        {
            string n = p.name;
            if (n.StartsWith("WordContainer") || n.StartsWith("Word") || n.StartsWith("Row"))
                return true;
            p = p.parent;
        }
        return false;
    }

    // ── Text styling ──────────────────────────────────────────────────────────

    void StyleText(TextMeshProUGUI tmp)
    {
        string n  = tmp.gameObject.name;
        string pn = tmp.transform.parent?.name ?? "";

        // Title → two-tone rich text
        if (n is "Title" or "TITLE" || pn is "Title" or "Header" or "HeaderBar")
        {
            tmp.enableRichText = true;
            tmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(textDark)}>SCRIP</color>" +
                       $"<color=#{ColorUtility.ToHtmlStringRGB(textGold)}>TURE</color>";
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textDark;
            return;
        }

        // Key labels
        if (KEY_NAMES.Contains(pn))
        {
            tmp.color = textDark;
            tmp.fontStyle = FontStyles.Bold;
            return;
        }

        // Tile letters (inside word rows)
        if (IsInsideWordRow(tmp.transform))
        {
            tmp.color = textDark;
            tmp.fontStyle = FontStyles.Bold;
            return;
        }

        // Default
        tmp.color = textDark;
    }

    // ── Static colour helper ──────────────────────────────────────────────────

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
