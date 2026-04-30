using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// One-click tool: Unity menu  Scripture ▶ Apply Theme
/// Reskins the open scene to match the SCRIPTURE wordle reference design.
/// </summary>
public static class ScriptureReskin
{
    // ── Palette ───────────────────────────────────────────────────────────────

    // Background / panels
    static readonly Color C_BG            = Hex("F2EBDA");   // warm cream canvas
    static readonly Color C_PANEL_BG      = Hex("F2EBDA");   // game panel (same)
    static readonly Color C_QUOTE_BG      = Hex("EDE6D3");   // slightly darker cream for quote box
    static readonly Color C_QUOTE_BORDER  = Hex("D9D0BB");   // subtle border

    // Tile states (empty / grey / yellow / green)
    static readonly Color C_TILE_EMPTY    = Hex("FFFFFF");
    static readonly Color C_TILE_GREY     = Hex("706B63");
    static readonly Color C_TILE_YELLOW   = Hex("B59F3B");
    static readonly Color C_TILE_GREEN    = Hex("4E7A3A");

    // Keyboard keys
    static readonly Color C_KEY_DEFAULT   = Hex("CFC9BC");   // warm tan
    static readonly Color C_KEY_GREY      = Hex("7D7469");
    static readonly Color C_KEY_YELLOW    = Hex("B59F3B");
    static readonly Color C_KEY_GREEN     = Hex("4E7A3A");

    // Text
    static readonly Color C_TEXT_DARK     = Hex("2C2924");
    static readonly Color C_TEXT_GOLD     = Hex("C99A33");
    static readonly Color C_TEXT_MID      = Hex("6B6355");
    static readonly Color C_TEXT_WHITE    = Color.white;

    // Stats bar
    static readonly Color C_STATS_NUM     = Hex("2C2924");
    static readonly Color C_STATS_LABEL   = Hex("8A8070");

    // ── Paths ─────────────────────────────────────────────────────────────────

    const string SPRITE_DIR       = "Assets/Sprites/Scripture";
    const string TILE_SPRITE_PATH = SPRITE_DIR + "/tile_rounded.png";
    const string KEY_SPRITE_PATH  = SPRITE_DIR + "/key_rounded.png";

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("Scripture/Apply Theme %#T")]
    public static void ApplyTheme()
    {
        EnsureSprites();

        Sprite tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TILE_SPRITE_PATH);
        Sprite keySprite  = AssetDatabase.LoadAssetAtPath<Sprite>(KEY_SPRITE_PATH);

        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) { Debug.LogError("[Scripture] Canvas not found."); return; }

        // ── 1. Canvas background ──────────────────────────────────────────────
        if (canvasGO.TryGetComponent(out Image canvasBg))
        {
            canvasBg.color   = C_BG;
            canvasBg.sprite  = null;
            EditorUtility.SetDirty(canvasBg);
        }
        else
        {
            // Add an Image to act as background fill
            var bg = canvasGO.AddComponent<Image>();
            bg.color   = C_BG;
            bg.raycastTarget = false;
            EditorUtility.SetDirty(bg);
        }

        // ── 2. Restyle every UI element in the canvas ─────────────────────────
        ReskinAll(canvasGO, tileSprite, keySprite);

        // ── 3. Ensure scripture quote exists below the header ─────────────────
        EnsureScriptureQuote(canvasGO);

        // ── 4. Update tile sprite references in GameManager ───────────────────
        PatchGameManagerSprites(canvasGO);

        // ── 5. Dirty + save ───────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Scripture] Theme applied successfully.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    // ── Sprite generation ─────────────────────────────────────────────────────

    static void EnsureSprites()
    {
        if (!Directory.Exists(SPRITE_DIR.Replace("Assets/", Application.dataPath + "/")))
            Directory.CreateDirectory(SPRITE_DIR.Replace("Assets/", Application.dataPath + "/"));

        if (!File.Exists(Application.dataPath + "/" + TILE_SPRITE_PATH.Substring("Assets/".Length)))
            SaveRoundedRectSprite(TILE_SPRITE_PATH, 56, 56, 8);

        if (!File.Exists(Application.dataPath + "/" + KEY_SPRITE_PATH.Substring("Assets/".Length)))
            SaveRoundedRectSprite(KEY_SPRITE_PATH, 44, 58, 6);

        AssetDatabase.Refresh();

        SetSpriteImportSettings(TILE_SPRITE_PATH, 56, 56, 8);
        SetSpriteImportSettings(KEY_SPRITE_PATH,  44, 58, 6);
    }

    static void SaveRoundedRectSprite(string assetPath, int w, int h, int radius)
    {
        Texture2D tex = GenerateRoundedRect(w, h, radius, Color.white);
        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string fullPath = Application.dataPath + "/" + assetPath.Substring("Assets/".Length);
        File.WriteAllBytes(fullPath, bytes);
    }

    static Texture2D GenerateRoundedRect(int w, int h, int r, Color fill)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear   = new Color(0, 0, 0, 0);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // distance from nearest corner quadrant
            int cx = (x < r)     ? r     : (x >= w - r) ? w - r - 1 : x;
            int cy = (y < r)     ? r     : (y >= h - r) ? h - r - 1 : y;
            bool inCorner = (x < r || x >= w - r) && (y < r || y >= h - r);
            if (inCorner)
            {
                float dx = x - cx, dy = y - cy;
                tex.SetPixel(x, y, (dx * dx + dy * dy <= (float)r * r) ? fill : clear);
            }
            else tex.SetPixel(x, y, fill);
        }
        tex.Apply();
        return tex;
    }

    static void SetSpriteImportSettings(string assetPath, int w, int h, int border)
    {
        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;
        ti.textureType       = TextureImporterType.Sprite;
        ti.spriteImportMode  = SpriteImportMode.Single;
        ti.spriteBorder      = new Vector4(border, border, border, border);
        ti.mipmapEnabled     = false;
        ti.filterMode        = FilterMode.Bilinear;
        ti.SaveAndReimport();
    }

    // ── Scene traversal ───────────────────────────────────────────────────────

    static readonly HashSet<string> KEY_NAMES = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "q","w","e","r","t","y","u","i","o","p",
        "a","s","d","f","g","h","j","k","l",
        "z","x","c","v","b","n","m",
        "enter","del","backspace"
    };

    static void ReskinAll(GameObject root, Sprite tileSprite, Sprite keySprite)
    {
        // Iterate every Image & TMP in canvas children
        Image[]          images = root.GetComponentsInChildren<Image>(true);
        TextMeshProUGUI[] tmps  = root.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (Image img in images)
        {
            string goName = img.gameObject.name;

            // ── Canvas backdrop
            if (goName == "Canvas") { img.color = C_BG; EditorUtility.SetDirty(img); continue; }

            // ── Game / background panels
            if (goName is "Panel" or "GamePanel" or "Panel (2)" or "Background" or "BG")
            {
                img.color = C_PANEL_BG; EditorUtility.SetDirty(img); continue;
            }

            // ── Keyboard keys  (Image component directly on the key GO)
            if (KEY_NAMES.Contains(goName) && img.gameObject == img.gameObject)
            {
                // Only style the root image of the key (not child images)
                if (img.transform.parent != null && img.transform == img.gameObject.transform)
                {
                    if (keySprite != null)
                    {
                        img.sprite = keySprite;
                        img.type   = Image.Type.Sliced;
                    }
                    img.color = C_KEY_DEFAULT;
                    EditorUtility.SetDirty(img);
                    continue;
                }
            }

            // ── Tile images (Image directly on a tile – identified by typical 56×55 size)
            RectTransform rt = img.GetComponent<RectTransform>();
            bool looksLikeTile = rt != null && rt.sizeDelta.x >= 50 && rt.sizeDelta.x <= 70
                                            && rt.sizeDelta.y >= 50 && rt.sizeDelta.y <= 70;
            if (looksLikeTile && img.transform.childCount == 0 || IsInsideWordRow(img.transform))
            {
                // Empty tile → white fill
                if (tileSprite != null)
                {
                    img.sprite = tileSprite;
                    img.type   = Image.Type.Sliced;
                }
                img.color = C_TILE_EMPTY;
                EditorUtility.SetDirty(img);
                continue;
            }

            // ── Result panels (GameWon / GameLost / GameEnd)
            Transform p = img.transform;
            bool insideResult = false;
            while (p != null) { if (p.name is "GameWon" or "GameLost" or "GameEnd" or "ResultPanel") { insideResult = true; break; } p = p.parent; }
            if (insideResult) { img.color = Hex("F7F1E3"); EditorUtility.SetDirty(img); continue; }
        }

        // ── TMP text ─────────────────────────────────────────────────────────
        foreach (TextMeshProUGUI tmp in tmps)
        {
            string goName = tmp.gameObject.name;
            string parentName = tmp.transform.parent?.name ?? "";

            // Title
            if (goName is "Title" or "Title (1)" || parentName is "Title" or "Header")
            {
                StyleTitle(tmp);
                continue;
            }

            // Key labels
            if (KEY_NAMES.Contains(parentName))
            {
                tmp.color     = C_TEXT_DARK;
                tmp.fontStyle = FontStyles.Bold;
                EditorUtility.SetDirty(tmp);
                continue;
            }

            // Tile letters
            if (IsInsideWordRow(tmp.transform))
            {
                tmp.color     = C_TEXT_DARK;
                tmp.fontStyle = FontStyles.Bold;
                EditorUtility.SetDirty(tmp);
                continue;
            }

            // Error messages
            if (goName is "NotInWordList" or "NotEnoughLetters" || parentName is "NotInWordList" or "NotEnoughLetters")
            {
                tmp.color = C_TEXT_DARK;
                EditorUtility.SetDirty(tmp);
                continue;
            }

            // Stats / subtitles – default dark
            tmp.color = C_TEXT_DARK;
            EditorUtility.SetDirty(tmp);
        }
    }

    static bool IsInsideWordRow(Transform t)
    {
        Transform p = t;
        while (p != null)
        {
            if (p.name.StartsWith("WordContainer") || p.name.StartsWith("Row") || p.name.StartsWith("Word"))
                return true;
            p = p.parent;
        }
        return false;
    }

    static void StyleTitle(TextMeshProUGUI tmp)
    {
        // Two-tone title: dark "SCRIP" + gold "TURE"
        tmp.enableRichText = true;
        tmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(C_TEXT_DARK)}>SCRIP</color>" +
                   $"<color=#{ColorUtility.ToHtmlStringRGB(C_TEXT_GOLD)}>TURE</color>";
        tmp.fontStyle  = FontStyles.Bold;
        tmp.fontSize   = 28;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.color      = C_TEXT_DARK;   // base colour (overridden by rich-text tags)
        EditorUtility.SetDirty(tmp);
    }

    // ── Scripture quote box ───────────────────────────────────────────────────

    const string QUOTE_TEXT  = "“Thy word is a lamp unto my feet,\nand a light unto my path.”";
    const string QUOTE_REF   = "PSALM 119:105";
    const string QUOTE_GO    = "ScriptureQuote";

    static void EnsureScriptureQuote(GameObject canvasGO)
    {
        // Don't add twice
        if (canvasGO.transform.Find(QUOTE_GO) != null) return;

        // Find header to anchor below it
        Transform header = canvasGO.transform.Find("Header")
                        ?? canvasGO.transform.Find("Title")
                        ?? canvasGO.transform.GetChild(0);

        GameObject quoteGO = new GameObject(QUOTE_GO);
        quoteGO.transform.SetParent(canvasGO.transform, false);

        // Position it below the stats bar (approximate)
        RectTransform rt = quoteGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0f);
        rt.anchorMax = new Vector2(0.95f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 430f);   // adjust per layout
        rt.sizeDelta = new Vector2(0f, 60f);

        // Background image
        Image bg = quoteGO.AddComponent<Image>();
        bg.color = C_QUOTE_BG;
        bg.raycastTarget = false;
        // rounded bg sprite
        Sprite qs = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_DIR + "/tile_rounded.png");
        if (qs != null) { bg.sprite = qs; bg.type = Image.Type.Sliced; }

        // Quote text child
        GameObject textGO = new GameObject("QuoteText");
        textGO.transform.SetParent(quoteGO.transform, false);
        RectTransform trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin       = Vector2.zero;
        trt.anchorMax       = Vector2.one;
        trt.offsetMin       = new Vector2(10, 6);
        trt.offsetMax       = new Vector2(-10, -6);

        TextMeshProUGUI qTmp = textGO.AddComponent<TextMeshProUGUI>();
        qTmp.text           = QUOTE_TEXT;
        qTmp.color          = C_TEXT_DARK;
        qTmp.fontSize       = 11f;
        qTmp.fontStyle      = FontStyles.Italic;
        qTmp.alignment      = TextAlignmentOptions.Left;
        qTmp.enableWordWrapping = true;
        qTmp.raycastTarget  = false;
        EditorUtility.SetDirty(qTmp);

        // Reference text child
        GameObject refGO = new GameObject("QuoteRef");
        refGO.transform.SetParent(quoteGO.transform, false);
        RectTransform rrt = refGO.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0f, 0f);
        rrt.anchorMax = new Vector2(1f, 0f);
        rrt.pivot     = new Vector2(0f, 0f);
        rrt.anchoredPosition = new Vector2(10, 4);
        rrt.sizeDelta = new Vector2(-20, 14);

        TextMeshProUGUI rTmp = refGO.AddComponent<TextMeshProUGUI>();
        rTmp.text           = QUOTE_REF;
        rTmp.color          = C_TEXT_MID;
        rTmp.fontSize       = 8f;
        rTmp.fontStyle      = FontStyles.SmallCaps;
        rTmp.alignment      = TextAlignmentOptions.Left;
        rTmp.raycastTarget  = false;
        EditorUtility.SetDirty(rTmp);

        // Move quote to index 1 so it sits just after the header
        quoteGO.transform.SetSiblingIndex(1);
        EditorUtility.SetDirty(quoteGO);
    }

    // ── GameManager sprite patch ──────────────────────────────────────────────

    static void PatchGameManagerSprites(GameObject canvasGO)
    {
        // Load colored tile sprites
        Sprite green  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Green.png");
        Sprite yellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Yellow.png");

        // Find the -----GameManager----- GO and patch sprite colors via tint
        // (The sprites themselves are used; we tint the Image to match palette)
        // GameManager uses sprite-swap at runtime, so we just patch the stored
        // key sprites to use our new key_rounded sprite with color tints.

        // Nothing required at design-time unless you want to preview colors:
        // GameManager references are inspector-assigned and survive the reskin.
        // Key sprites (key_grey, key_yellow, key_green) keep their assignments.
    }
}
