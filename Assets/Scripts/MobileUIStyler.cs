using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Restyles the existing menus + HUD for phones at runtime (no scene changes needed):
//  - canvas scaling that follows portrait / landscape
//  - safe area (notch, rounded corners, gesture bar)
//  - big touch targets, readable text, rounded "neon party" look
// It keeps all your references and onClick events, it only moves / restyles things.
// Optional: put a TMP font asset named "UIFont" in any Resources folder to use it everywhere.
public class MobileUIStyler : MonoBehaviour
{
    // ---------- palette ----------
    static readonly Color Red = Hex("FF3D57");
    static readonly Color RedDeep = Hex("A3122F");
    static readonly Color Amber = Hex("FFC23C");
    static readonly Color Cyan = Hex("3DE0FF");
    static readonly Color Ink = Hex("0A0D22");
    static readonly Color TextMain = Hex("F6F7FF");

    static Color Hex(string h)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + h, out c);
        return c;
    }

    // ---------- generated sprites ----------
    const int TexSize = 256;
    const float SpriteRadius = 112f;
    static Sprite sprPill, sprPillRing, sprCircle, sprCircleRing, sprGlow, sprBg;

    static void BuildSprites()
    {
        if (sprPill != null) return;
        sprPill = MakeRounded(TexSize, SpriteRadius, 0f, true, true);
        sprPillRing = MakeRounded(TexSize, SpriteRadius, 7f, false, true);
        sprCircle = MakeRounded(128, 64f, 0f, false, false);
        sprCircleRing = MakeRounded(128, 64f, 6f, false, false);

        var glow = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        glow.wrapMode = TextureWrapMode.Clamp;
        var gp = new Color32[128 * 128];
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float r = new Vector2(x + 0.5f - 64f, y + 0.5f - 64f).magnitude / 64f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
                gp[y * 128 + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        glow.SetPixels32(gp);
        glow.Apply(false, true);
        sprGlow = Sprite.Create(glow, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);

        var bg = new Texture2D(4, 256, TextureFormat.RGBA32, false);
        bg.wrapMode = TextureWrapMode.Clamp;
        bg.filterMode = FilterMode.Bilinear;
        var bp = new Color32[4 * 256];
        Color bottom = new Color(0.02f, 0.03f, 0.07f, 0.96f);
        Color mid = new Color(0.05f, 0.05f, 0.15f, 0.92f);
        Color top = new Color(0.11f, 0.07f, 0.30f, 0.90f);
        for (int y = 0; y < 256; y++)
        {
            float v = y / 255f;
            Color c = v < 0.5f ? Color.Lerp(bottom, mid, v * 2f) : Color.Lerp(mid, top, (v - 0.5f) * 2f);
            Color32 c32 = c;
            for (int x = 0; x < 4; x++) bp[y * 4 + x] = c32;
        }
        bg.SetPixels32(bp);
        bg.Apply(false, true);
        sprBg = Sprite.Create(bg, new Rect(0, 0, 4, 256), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite MakeRounded(int size, float radius, float ring, bool shade, bool sliced)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[size * size];
        float half = size * 0.5f;
        float inner = half - radius;
        for (int y = 0; y < size; y++)
        {
            float v = (y + 0.5f) / size;
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - half) - inner;
                float dy = Mathf.Abs(y + 0.5f - half) - inner;
                float d = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude
                          + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
                float a = Mathf.Clamp01(0.5f - d);
                if (ring > 0f) a *= Mathf.Clamp01(d + ring + 0.5f);
                float s = shade ? Mathf.Lerp(0.74f, 1f, v) : 1f;
                byte c = (byte)Mathf.RoundToInt(s * 255f);
                px[y * size + x] = new Color32(c, c, c, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        Vector4 border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
    }

    // ---------- references ----------
    MainMenu menu;
    BallThrower thrower;
    TablePlacer placer;
    Canvas canvas;
    CanvasScaler scaler;
    TMP_FontAsset uiFont;

    RectTransform menuSafe, settingsSafe, hudSafe;
    RectTransform menuHeader, menuButtons;
    Image glowA, glowB, setGlow;

    RectTransform settingsTitle, settingsBar, settingsCard, settingsBack;
    GridLayoutGroup grid;

    HudPill throwsPill, infoPill;
    RectTransform undoRT;

    int lastW, lastH;
    Rect lastSafe;

    enum Look { Primary, Glass, Quiet, Chip }

    // ---------- boot ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include) == null) return;
        if (FindFirstObjectByType<MobileUIStyler>(FindObjectsInactive.Include) != null) return;
        new GameObject("MobileUIStyler").AddComponent<MobileUIStyler>();
    }

    void Awake()
    {
        menu = FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (menu == null || menu.mainMenuPanel == null || menu.settingsPanel == null || menu.arHud == null)
        {
            Debug.LogWarning("MobileUIStyler: MainMenu references missing, skipping.");
            enabled = false;
            return;
        }

        thrower = FindFirstObjectByType<BallThrower>(FindObjectsInactive.Include);
        placer = menu.tablePlacer;
        canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>(true);
        if (canvas == null) { enabled = false; return; }

        BuildSprites();
        uiFont = Resources.Load<TMP_FontAsset>("UIFont");

        // taps on high dpi screens: 10px is way too sensitive and cancels clicks
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es != null)
            es.pixelDragThreshold = Mathf.Max(10, Mathf.RoundToInt(Screen.dpi > 0f ? Screen.dpi * 0.08f : 30f));

        scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        BuildMainMenu();
        BuildSettings();
        BuildHud();
        ApplyLayout();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH || Screen.safeArea != lastSafe)
            ApplyLayout();

        if (glowA != null)
        {
            Color c = glowA.color;
            c.a = 0.30f * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 1.6f));
            glowA.color = c;
        }
    }

    // ---------- helpers ----------
    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform c in root)
        {
            if (c.name == name) return c;
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static Image NewImage(string name, Transform parent, Sprite sp, Color c, Image.Type type = Image.Type.Simple)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sp;
        img.color = c;
        img.type = type;
        img.raycastTarget = false;
        return img;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    static void Round(Image img, float radius)
    {
        img.pixelsPerUnitMultiplier = SpriteRadius / Mathf.Max(1f, radius);
    }

    static void SetHeight(GameObject go, float h)
    {
        LayoutElement le = GetOrAdd<LayoutElement>(go);
        le.minHeight = h;
        le.preferredHeight = h;
    }

    void StyleText(TMP_Text t, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        if (uiFont != null) t.font = uiFont;
        t.fontSize = size;
        t.enableAutoSizing = false;
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.richText = true;
    }

    TextMeshProUGUI NewText(string name, Transform parent, string s, float size, Color color,
        FontStyles style, TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = s;
        StyleText(t, size, color, style, align);
        return t;
    }

    static void Underlay(TextMeshProUGUI t, Color c, float offset, float dilate, float softness)
    {
        Material m = t.fontMaterial;
        m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        m.SetColor(ShaderUtilities.ID_UnderlayColor, c);
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, offset);
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -offset);
        m.SetFloat(ShaderUtilities.ID_UnderlayDilate, dilate);
        m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
        t.UpdateMeshPadding();
    }

    RectTransform MakeSafeArea(Transform parent)
    {
        RectTransform rt = NewRect("SafeArea", parent);
        Stretch(rt);
        return rt;
    }

    static void FitSafe(RectTransform rt)
    {
        if (rt == null) return;
        Rect sa = Screen.safeArea;
        rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void StyleBackground(Image bg)
    {
        bg.sprite = sprBg;
        bg.type = Image.Type.Simple;
        bg.color = Color.white;
        bg.raycastTarget = true;
    }

    void AddIntro(RectTransform panel, RectTransform content)
    {
        GetOrAdd<CanvasGroup>(panel.gameObject);
        PanelIntro p = panel.gameObject.AddComponent<PanelIntro>();
        p.content = content;
    }

    // ---------- buttons ----------
    void StyleButton(Button b, Look look, float height, float fontSize)
    {
        if (b == null) return;
        Image img = GetOrAdd<Image>(b.gameObject);
        img.sprite = sprPill;
        img.type = Image.Type.Sliced;
        img.raycastTarget = true;
        Round(img, height * 0.5f);
        b.targetGraphic = img;

        Color labelColor = TextMain;
        Color ring = Color.clear;
        switch (look)
        {
            case Look.Primary:
                img.color = Red;
                Shadow lip = GetOrAdd<Shadow>(b.gameObject);
                lip.effectColor = new Color(RedDeep.r, RedDeep.g, RedDeep.b, 1f);
                lip.effectDistance = new Vector2(0f, -10f);
                lip.useGraphicAlpha = false;
                Image shine = NewImage("Shine", b.transform, sprPill, new Color(1f, 1f, 1f, 0.16f), Image.Type.Sliced);
                Place(shine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 0f));
                shine.rectTransform.anchorMin = new Vector2(0f, 0.52f);
                shine.rectTransform.anchorMax = new Vector2(1f, 1f);
                shine.rectTransform.offsetMin = new Vector2(26f, 0f);
                shine.rectTransform.offsetMax = new Vector2(-26f, -10f);
                Round(shine, height * 0.2f);
                shine.rectTransform.SetAsFirstSibling();
                break;
            case Look.Glass:
                img.color = new Color(1f, 1f, 1f, 0.10f);
                ring = new Color(1f, 1f, 1f, 0.28f);
                break;
            case Look.Quiet:
                img.color = new Color(1f, 1f, 1f, 0.04f);
                ring = new Color(1f, 0.35f, 0.45f, 0.55f);
                labelColor = new Color(1f, 0.80f, 0.84f);
                break;
            case Look.Chip:
                img.color = new Color(Ink.r, Ink.g, Ink.b, 0.62f);
                ring = new Color(1f, 1f, 1f, 0.25f);
                break;
        }

        if (ring.a > 0f)
        {
            Image r = NewImage("Ring", b.transform, sprPillRing, ring, Image.Type.Sliced);
            Stretch(r.rectTransform);
            Round(r, height * 0.5f);
            r.rectTransform.SetAsFirstSibling();
        }

        TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            StyleText(label, fontSize, labelColor, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Center);
            label.characterSpacing = look == Look.Chip ? 4f : 8f;
            label.enableAutoSizing = true;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = fontSize * 0.55f;
            Stretch(label.rectTransform, 28f, 0f, 28f, 0f);
            if (look == Look.Primary) Underlay(label, new Color(0.4f, 0f, 0.1f, 0.6f), 0.5f, 0.1f, 0.3f);
        }

        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white;
        cb.selectedColor = Color.white;
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.06f;
        b.colors = cb;

        GetOrAdd<PressScale>(b.gameObject);
    }

    // ---------- main menu ----------
    void BuildMainMenu()
    {
        RectTransform panel = (RectTransform)menu.mainMenuPanel.transform;
        StyleBackground(GetOrAdd<Image>(panel.gameObject));

        glowA = NewImage("GlowA", panel, sprGlow, new Color(Red.r, Red.g, Red.b, 0.30f));
        glowB = NewImage("GlowB", panel, sprGlow, new Color(Cyan.r, Cyan.g, Cyan.b, 0.16f));
        glowB.rectTransform.SetAsFirstSibling();
        glowA.rectTransform.SetAsFirstSibling();

        menuSafe = MakeSafeArea(panel);
        AddIntro(panel, menuSafe);

        menuHeader = NewRect("Header", menuSafe);
        menuButtons = NewRect("Buttons", menuSafe);

        // title
        Transform title = FindDeep(panel, "Tittle");
        TextMeshProUGUI titleTxt = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
        if (titleTxt == null)
            titleTxt = NewText("Title", menuHeader, "", 190, TextMain, FontStyles.Bold, TextAlignmentOptions.Center);
        titleTxt.transform.SetParent(menuHeader, false);
        StyleText(titleTxt, 190f, TextMain, FontStyles.Bold, TextAlignmentOptions.Center);
        titleTxt.text = "BEER\n<color=#FFC23C>PONG</color>";
        titleTxt.lineSpacing = -28f;
        titleTxt.characterSpacing = 4f;
        Place(titleTxt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 420f));
        Underlay(titleTxt, new Color(0f, 0f, 0f, 0.65f), 0.7f, 0.25f, 0.35f);

        // subtitle
        TextMeshProUGUI sub = NewText("Subtitle", menuHeader, "PARTY GAME", 42, Cyan, FontStyles.Bold, TextAlignmentOptions.Center);
        sub.characterSpacing = 16f;
        Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -405f), new Vector2(900f, 60f));
        for (int i = 0; i < 2; i++)
        {
            Image line = NewImage(i == 0 ? "LineL" : "LineR", menuHeader, sprPill,
                new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f), Image.Type.Sliced);
            Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(i == 0 ? -350f : 350f, -435f), new Vector2(140f, 6f));
            Round(line, 3f);
        }

        BuildRack(menuHeader);

        // buttons
        VerticalLayoutGroup v = menuButtons.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 28f;
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        AddMenuButton(panel, "PlayButton", Look.Primary, 160f, 68f);
        AddMenuButton(panel, "SettingsButton", Look.Glass, 128f, 52f);
        AddMenuButton(panel, "ExitButton", Look.Quiet, 128f, 52f);
    }

    void AddMenuButton(Transform panel, string name, Look look, float h, float fs)
    {
        Transform t = FindDeep(panel, name);
        if (t == null) return;
        t.SetParent(menuButtons, false);
        SetHeight(t.gameObject, h);
        StyleButton(t.GetComponent<Button>(), look, h, fs);
    }

    void BuildRack(Transform parent)
    {
        RectTransform rack = NewRect("CupRack", parent);
        Place(rack, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(400f, 360f));
        Vector2 top = new Vector2(0.5f, 1f);
        const float d = 92f, pitchX = 100f, pitchY = 88f;
        for (int row = 0; row < 4; row++)
            for (int i = 0; i <= row; i++)
            {
                Vector2 p = new Vector2((i - row * 0.5f) * pitchX, -d * 0.5f - row * pitchY);
                Circle(rack, "Shadow", p + new Vector2(0f, -7f), d, new Color(0f, 0f, 0f, 0.4f), sprCircle, top);
                Circle(rack, "Cup", p, d, Red, sprCircle, top);
                Circle(rack, "Rim", p, d, new Color(1f, 1f, 1f, 0.35f), sprCircleRing, top);
                Circle(rack, "Mouth", p, d * 0.78f, RedDeep, sprCircle, top);
                Circle(rack, "Beer", p, d * 0.60f, Amber, sprCircle, top);
                Circle(rack, "Glint", p + new Vector2(-d * 0.12f, d * 0.12f), d * 0.16f, new Color(1f, 1f, 1f, 0.5f), sprCircle, top);
            }

        Vector2 bp = new Vector2(150f, -30f);
        Circle(rack, "BallShadow", bp + new Vector2(0f, -8f), 46f, new Color(0f, 0f, 0f, 0.35f), sprCircle, top);
        Image ball = Circle(rack, "Ball", bp, 46f, Color.white, sprCircle, top);
        Circle(ball.transform, "BallEdge", Vector2.zero, 46f, new Color(0f, 0f, 0f, 0.18f), sprCircleRing, new Vector2(0.5f, 0.5f));
        ball.gameObject.AddComponent<UIBob>();
    }

    static Image Circle(Transform parent, string name, Vector2 pos, float size, Color c, Sprite sp, Vector2 anchor)
    {
        Image img = NewImage(name, parent, sp, c);
        Place(img.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
        return img;
    }

    // ---------- settings ----------
    void BuildSettings()
    {
        RectTransform panel = (RectTransform)menu.settingsPanel.transform;
        StyleBackground(GetOrAdd<Image>(panel.gameObject));

        setGlow = NewImage("Glow", panel, sprGlow, new Color(Cyan.r, Cyan.g, Cyan.b, 0.14f));
        setGlow.rectTransform.SetAsFirstSibling();

        settingsSafe = MakeSafeArea(panel);
        AddIntro(panel, settingsSafe);

        SettingsAudioUI audio = menu.settingsPanel.GetComponent<SettingsAudioUI>();

        // title
        Transform tt = FindDeep(panel, "SettingsTittle");
        TextMeshProUGUI titleTxt = tt != null ? tt.GetComponent<TextMeshProUGUI>() : null;
        if (titleTxt == null)
            titleTxt = NewText("SettingsTitle", settingsSafe, "SETTINGS", 100, TextMain, FontStyles.Bold, TextAlignmentOptions.Center);
        titleTxt.transform.SetParent(settingsSafe, false);
        StyleText(titleTxt, 104f, TextMain, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Center);
        titleTxt.characterSpacing = 6f;
        Underlay(titleTxt, new Color(0f, 0f, 0f, 0.6f), 0.6f, 0.2f, 0.3f);
        settingsTitle = titleTxt.rectTransform;

        Image bar = NewImage("TitleBar", settingsSafe, sprPill, Amber, Image.Type.Sliced);
        Round(bar, 5f);
        settingsBar = bar.rectTransform;

        // card with the two audio sections
        Image card = NewImage("Card", settingsSafe, sprPill, new Color(Ink.r, Ink.g, Ink.b, 0.78f), Image.Type.Sliced);
        Round(card, 56f);
        Image cardRing = NewImage("Ring", card.transform, sprPillRing, new Color(1f, 1f, 1f, 0.14f), Image.Type.Sliced);
        Stretch(cardRing.rectTransform);
        Round(cardRing, 56f);
        GetOrAdd<LayoutElement>(cardRing.gameObject).ignoreLayout = true;
        settingsCard = card.rectTransform;
        grid = card.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(56, 56, 56, 56);
        grid.spacing = new Vector2(64f, 56f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        ContentSizeFitter fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Toggle mt = audio != null ? audio.musicToggle : null;
        Slider ms = audio != null ? audio.musicSlider : null;
        Toggle st = audio != null ? audio.sfxToggle : null;
        Slider ss = audio != null ? audio.sfxSlider : null;
        if (mt != null && ms != null) BuildAudioSection(card.transform, "MUSIC", mt, ms, Red);
        if (st != null && ss != null) BuildAudioSection(card.transform, "SOUND FX", st, ss, Cyan);

        // back button
        Transform back = FindDeep(panel, "BackButton");
        if (back != null)
        {
            back.SetParent(settingsSafe, false);
            settingsBack = (RectTransform)back;
            StyleButton(back.GetComponent<Button>(), Look.Glass, 128f, 52f);
        }
    }

    void BuildAudioSection(Transform card, string label, Toggle toggle, Slider slider, Color accent)
    {
        RectTransform section = NewRect(label + "Section", card);
        VerticalLayoutGroup v = section.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 10f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        toggle.transform.SetParent(section, false);
        slider.transform.SetParent(section, false);
        SetHeight(toggle.gameObject, 96f);
        SetHeight(slider.gameObject, 88f);

        CanvasGroup dim = GetOrAdd<CanvasGroup>(slider.gameObject);
        StyleSwitch(toggle, label, accent, dim);
        StyleSlider(slider, accent);
    }

    void StyleSwitch(Toggle t, string label, Color accent, CanvasGroup dim)
    {
        Image hit = GetOrAdd<Image>(t.gameObject);
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.raycastTarget = true;

        Transform oldLabel = t.transform.Find("Label");
        if (oldLabel != null) oldLabel.gameObject.SetActive(false);
        if (t.graphic != null) t.graphic.gameObject.SetActive(false);
        t.graphic = null;

        Transform bgT = t.transform.Find("Background");
        Image track = bgT != null ? bgT.GetComponent<Image>() : null;
        if (track == null) track = NewImage("Background", t.transform, sprPill, Color.white, Image.Type.Sliced);
        Place(track.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(140f, 76f));
        track.sprite = sprPill;
        track.type = Image.Type.Sliced;
        track.raycastTarget = false;
        Round(track, 38f);
        t.targetGraphic = track;
        t.transition = Selectable.Transition.None;

        Image knob = NewImage("Knob", track.transform, sprCircle, Color.white);
        Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
        Shadow ks = knob.gameObject.AddComponent<Shadow>();
        ks.effectColor = new Color(0f, 0f, 0f, 0.35f);
        ks.effectDistance = new Vector2(0f, -4f);

        Image dot = NewImage("Dot", t.transform, sprCircle, accent);
        Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 24f));

        TextMeshProUGUI txt = NewText("LabelTMP", t.transform, label, 50, TextMain,
            FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft);
        txt.characterSpacing = 6f;
        Stretch(txt.rectTransform, 50f, 0f, 170f, 0f);

        SwitchVisual sv = t.gameObject.AddComponent<SwitchVisual>();
        sv.toggle = t;
        sv.track = track;
        sv.knob = knob.rectTransform;
        sv.onColor = accent;
        sv.offColor = new Color(1f, 1f, 1f, 0.18f);
        sv.dimTarget = dim;
        sv.travel = 32f;
    }

    void StyleSlider(Slider s, Color accent)
    {
        const float handle = 64f, track = 20f;

        Image hit = GetOrAdd<Image>(s.gameObject);
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.raycastTarget = true;

        Transform bgT = s.transform.Find("Background");
        if (bgT != null)
        {
            RectTransform rt = (RectTransform)bgT;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(-handle, track);
            Image im = bgT.GetComponent<Image>();
            if (im != null)
            {
                im.sprite = sprPill;
                im.type = Image.Type.Sliced;
                im.color = new Color(1f, 1f, 1f, 0.16f);
                im.raycastTarget = false;
                Round(im, track * 0.5f);
            }
        }

        RectTransform fillArea = s.fillRect != null ? s.fillRect.parent as RectTransform : null;
        if (fillArea != null)
        {
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.pivot = new Vector2(0.5f, 0.5f);
            fillArea.anchoredPosition = Vector2.zero;
            fillArea.sizeDelta = new Vector2(-handle, track);
        }
        if (s.fillRect != null)
        {
            s.fillRect.anchorMin = new Vector2(0f, 0f);
            s.fillRect.anchorMax = new Vector2(0f, 1f);
            s.fillRect.anchoredPosition = Vector2.zero;
            s.fillRect.sizeDelta = Vector2.zero;
            Image fi = s.fillRect.GetComponent<Image>();
            if (fi != null)
            {
                fi.sprite = sprPill;
                fi.type = Image.Type.Sliced;
                fi.color = accent;
                fi.raycastTarget = false;
                Round(fi, track * 0.5f);
            }
        }

        RectTransform handleArea = s.handleRect != null ? s.handleRect.parent as RectTransform : null;
        if (handleArea != null)
        {
            handleArea.anchorMin = new Vector2(0f, 0.5f);
            handleArea.anchorMax = new Vector2(1f, 0.5f);
            handleArea.pivot = new Vector2(0.5f, 0.5f);
            handleArea.anchoredPosition = Vector2.zero;
            handleArea.sizeDelta = new Vector2(-handle, handle);
        }
        if (s.handleRect != null)
        {
            RectTransform h = s.handleRect;
            h.anchorMin = new Vector2(0f, 0f);
            h.anchorMax = new Vector2(0f, 1f);
            h.pivot = new Vector2(0.5f, 0.5f);
            h.sizeDelta = new Vector2(handle, 0f);
            h.anchoredPosition = Vector2.zero;
            Image him = h.GetComponent<Image>();
            if (him != null)
            {
                him.sprite = sprCircle;
                him.type = Image.Type.Simple;
                him.color = Color.white;
                him.raycastTarget = false;
                Shadow sh = GetOrAdd<Shadow>(h.gameObject);
                sh.effectColor = new Color(0f, 0f, 0f, 0.4f);
                sh.effectDistance = new Vector2(0f, -5f);
                Image core = NewImage("Core", h, sprCircle, accent);
                Place(core.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
                s.targetGraphic = him;
            }
        }
        s.transition = Selectable.Transition.None;
        s.SetValueWithoutNotify(s.value);
    }

    // ---------- AR HUD ----------
    void BuildHud()
    {
        RectTransform hud = (RectTransform)menu.arHud.transform;
        Stretch(hud);
        hudSafe = MakeSafeArea(hud);

        TextMeshProUGUI throwsTxt = thrower != null ? thrower.throwsText : null;
        TextMeshProUGUI infoTxt = thrower != null && thrower.infoText != null ? thrower.infoText
            : (placer != null ? placer.infoText : null);

        if (throwsTxt != null)
        {
            StyleText(throwsTxt, 42f, TextMain, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Center);
            throwsTxt.characterSpacing = 3f;
            throwsPill = MakePill("ThrowsChip", throwsTxt, 700f);
        }
        if (infoTxt != null)
        {
            StyleText(infoTxt, 44f, TextMain, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Center);
            infoTxt.characterSpacing = 3f;
            infoTxt.textWrappingMode = TextWrappingModes.Normal;
            infoPill = MakePill("InfoToast", infoTxt, 880f);
        }

        if (placer != null && placer.undoButton != null)
        {
            undoRT = (RectTransform)placer.undoButton.transform;
            undoRT.SetParent(hudSafe, false);
            StyleButton(placer.undoButton.GetComponent<Button>(), Look.Chip, 96f, 40f);
        }
    }

    HudPill MakePill(string name, TextMeshProUGUI text, float maxWidth)
    {
        Image img = NewImage(name, hudSafe, sprPill, new Color(Ink.r, Ink.g, Ink.b, 0.62f), Image.Type.Sliced);
        Round(img, 48f);
        Image ring = NewImage("Ring", img.transform, sprPillRing, new Color(1f, 1f, 1f, 0.22f), Image.Type.Sliced);
        Stretch(ring.rectTransform);
        Round(ring, 48f);

        CanvasGroup cg = img.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        text.transform.SetParent(img.transform, false);
        Stretch(text.rectTransform, 40f, 20f, 40f, 20f);

        HudPill p = img.gameObject.AddComponent<HudPill>();
        p.text = text;
        p.root = img.rectTransform;
        p.group = cg;
        p.maxTextWidth = maxWidth;
        return p;
    }

    // ---------- layout (portrait / landscape / safe area) ----------
    void ApplyLayout()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        lastSafe = Screen.safeArea;

        bool land = Screen.width > Screen.height;
        scaler.referenceResolution = land ? new Vector2(1920f, 1080f) : new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = land ? 1f : 0f;

        float scale = land ? Screen.height / 1080f : Screen.width / 1080f;
        float safeW = lastSafe.width / scale;
        float safeH = lastSafe.height / scale;

        FitSafe(menuSafe);
        FitSafe(settingsSafe);
        FitSafe(hudSafe);

        LayoutMainMenu(land, safeW, safeH);
        LayoutSettings(land, safeW, safeH);
        LayoutHud(land, safeW);
    }

    void LayoutMainMenu(bool land, float safeW, float safeH)
    {
        const float buttonsH = 160f + 128f + 128f + 28f * 2f;
        Vector2 half = new Vector2(0.5f, 0.5f);

        if (!land)
        {
            float bw = Mathf.Min(safeW - 120f, 780f);
            Place(menuButtons, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(bw, buttonsH));

            float availH = safeH - (100f + buttonsH + 40f) - 40f;
            float s = Mathf.Clamp(Mathf.Min(availH / 860f, (safeW - 40f) / 900f), 0.5f, 1f);
            float y = -(40f + Mathf.Max(0f, (availH - 860f * s) * 0.5f));
            Place(menuHeader, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(900f, 860f));
            menuHeader.localScale = Vector3.one * s;

            Place(glowA.rectTransform, new Vector2(0.5f, 1f), half, new Vector2(0f, -560f), new Vector2(1500f, 1500f));
            Place(glowB.rectTransform, new Vector2(0.5f, 0f), half, new Vector2(0f, 280f), new Vector2(1300f, 1300f));
        }
        else
        {
            float s = Mathf.Clamp(Mathf.Min((safeH - 40f) / 860f, (safeW * 0.5f - 40f) / 900f), 0.5f, 1f);
            Place(menuHeader, new Vector2(0.27f, 0.5f), half, Vector2.zero, new Vector2(900f, 860f));
            menuHeader.localScale = Vector3.one * s;

            float bw = Mathf.Min(safeW * 0.40f, 720f);
            Place(menuButtons, new Vector2(0.74f, 0.5f), half, Vector2.zero, new Vector2(bw, buttonsH));

            Place(glowA.rectTransform, new Vector2(0.27f, 0.5f), half, Vector2.zero, new Vector2(1500f, 1500f));
            Place(glowB.rectTransform, new Vector2(0.74f, 0.5f), half, Vector2.zero, new Vector2(1200f, 1200f));
        }
    }

    void LayoutSettings(bool land, float safeW, float safeH)
    {
        float topY = land ? 30f : 80f;
        if (settingsTitle != null)
            Place(settingsTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -topY), new Vector2(900f, 150f));
        if (settingsBar != null)
            Place(settingsBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(topY + 150f)), new Vector2(140f, 10f));

        if (settingsCard != null && grid != null)
        {
            int cols = land ? 2 : 1;
            float cardW = land ? Mathf.Min(safeW - 160f, 1500f) : Mathf.Min(safeW - 80f, 900f);
            float cellW = (cardW - grid.padding.left - grid.padding.right - grid.spacing.x * (cols - 1)) / cols;
            grid.constraintCount = cols;
            grid.cellSize = new Vector2(cellW, 196f);
            Place(settingsCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cardW, 300f));
        }

        if (settingsBack != null)
            Place(settingsBack, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, land ? 30f : 100f), new Vector2(Mathf.Min(safeW - 160f, 640f), 128f));

        if (setGlow != null)
            Place(setGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 1600f));
    }

    void LayoutHud(bool land, float safeW)
    {
        const float margin = 28f;
        Vector2 tl = new Vector2(0f, 1f), tr = new Vector2(1f, 1f), tc = new Vector2(0.5f, 1f);

        if (throwsPill != null)
        {
            Place(throwsPill.root, tl, tl, new Vector2(margin, -margin), throwsPill.root.sizeDelta);
            throwsPill.Refresh();
        }
        if (undoRT != null)
            Place(undoRT, tr, tr, new Vector2(-margin, -margin), new Vector2(230f, 96f));
        if (infoPill != null)
        {
            bool oneRow = safeW > 1500f;
            infoPill.maxTextWidth = Mathf.Min(safeW - 2f * margin - 80f, oneRow ? 900f : 880f);
            Place(infoPill.root, tc, tc, new Vector2(0f, oneRow ? -margin : -(margin + 96f + 20f)), infoPill.root.sizeDelta);
            infoPill.Refresh();
        }
    }
}

// ================= small helper components =================

// fits a pill to its text (and hides it when the text is empty)
public class HudPill : MonoBehaviour
{
    public TextMeshProUGUI text;
    public RectTransform root;
    public CanvasGroup group;
    public float padX = 40f, padY = 20f, minHeight = 96f, maxTextWidth = 900f;
    string last;

    public void Refresh() { last = null; }

    void LateUpdate()
    {
        if (text == null || root == null || group == null) return;
        string s = text.text ?? "";
        if (s != last)
        {
            last = s;
            if (s.Length > 0)
            {
                Vector2 p = text.GetPreferredValues(s, maxTextWidth, 0f);
                root.sizeDelta = new Vector2(Mathf.Ceil(p.x) + padX * 2f + 2f,
                    Mathf.Max(minHeight, Mathf.Ceil(p.y) + padY * 2f));
            }
        }
        group.alpha = Mathf.MoveTowards(group.alpha, s.Length > 0 ? 1f : 0f, Time.unscaledDeltaTime * 6f);
    }
}

// animated on/off switch driven by a Toggle (polls isOn, so SetIsOnWithoutNotify works too)
public class SwitchVisual : MonoBehaviour
{
    public Toggle toggle;
    public Image track;
    public RectTransform knob;
    public Color onColor, offColor;
    public CanvasGroup dimTarget;
    public float travel = 32f;
    float t;
    int snap;

    void OnEnable() { snap = 2; }

    void Update()
    {
        if (toggle == null || knob == null || track == null) return;
        float target = toggle.isOn ? 1f : 0f;
        if (snap > 0) { t = target; snap--; }
        else t = Mathf.MoveTowards(t, target, Time.unscaledDeltaTime * 9f);
        float e = t * t * (3f - 2f * t);
        knob.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, e), 0f);
        track.color = Color.Lerp(offColor, onColor, e);
        if (dimTarget != null) dimTarget.alpha = Mathf.Lerp(0.4f, 1f, e);
    }
}

// little press feedback
public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    float target = 1f;

    void OnEnable() { target = 1f; transform.localScale = Vector3.one; }
    public void OnPointerDown(PointerEventData e) { target = 0.95f; }
    public void OnPointerUp(PointerEventData e) { target = 1f; }
    public void OnPointerExit(PointerEventData e) { target = 1f; }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, Time.unscaledDeltaTime * 22f);
    }
}

// fade + tiny zoom when a panel is shown
public class PanelIntro : MonoBehaviour
{
    public RectTransform content;
    CanvasGroup group;
    float t = 1f;

    void Awake() { group = GetComponent<CanvasGroup>(); }
    void OnEnable() { t = 0f; Apply(0f); }

    void Update()
    {
        if (t >= 1f) return;
        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.3f);
        Apply(1f - Mathf.Pow(1f - t, 3f));
    }

    void Apply(float e)
    {
        if (group != null) group.alpha = e;
        if (content != null) content.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
    }
}

// gentle floating motion (the ping pong ball)
public class UIBob : MonoBehaviour
{
    public float amplitude = 10f, speed = 2.2f;
    RectTransform rt;
    Vector2 basePos;

    void Awake()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
    }

    void Update()
    {
        rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(Time.unscaledTime * speed) * amplitude);
    }
}
