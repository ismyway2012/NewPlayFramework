using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DetailedViewUIBuilder : MonoBehaviour
{
    static readonly Color ColorPrimary = new Color32(0x67, 0x50, 0xA4, 0xFF);
    static readonly Color ColorOnPrimary = Color.white;
    static readonly Color ColorSurface = new Color32(0xFE, 0xF7, 0xFF, 0xFF);
    static readonly Color ColorOnSurface = new Color32(0x1D, 0x1B, 0x20, 0xFF);
    static readonly Color ColorOnSurfaceVariant = new Color32(0x49, 0x45, 0x4F, 0xFF);
    static readonly Color ColorSurfaceContainer = new Color32(0xF3, 0xED, 0xF7, 0xFF);
    static readonly Color ColorSurfaceContainerHigh = new Color32(0xEC, 0xE6, 0xF0, 0xFF);
    static readonly Color ColorSecondaryContainer = new Color32(0xE8, 0xDE, 0xF8, 0xFF);

    TMP_FontAsset _font;

    [ContextMenu("Build UI")]
    public void BuildUI()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        gameObject.layer = LayerMask.NameToLayer("UI");
        _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        var root = CreatePanel("Background", transform, ColorSurface);
        SetStretch(root);

        var vl = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        vl.childAlignment = TextAnchor.UpperLeft;

        BuildStatusBar(root);
        BuildAppBar(root);
        BuildScrollArea(root);
        BuildMediaPlayer(root);
        BuildNavigationHandle(root);
    }

    void BuildStatusBar(RectTransform parent)
    {
        var bar = CreateTransparentPanel("StatusBar", parent);
        AddLE(bar, minH: 52, prefH: 52);

        var hl = AddHLayout(bar, new RectOffset(24, 24, 14, 14));
        hl.childAlignment = TextAnchor.MiddleCenter;

        var time = CreateTMP("TimeText", bar, "9:30", 14, ColorOnSurface, FontStyles.Bold);
        SetSize(time.rectTransform, 50, 20);

        AddFlexSpacer(bar);

        var icons = CreateHGroup("StatusIcons", bar, 6);
        SetSize(icons, 60, 20);
        CreateTMPIcon("Wifi", icons, "W", 10, ColorOnSurface);
        CreateTMPIcon("Signal", icons, "S", 10, ColorOnSurface);
        CreateTMPIcon("Battery", icons, "B", 10, ColorOnSurface);
    }

    void BuildAppBar(RectTransform parent)
    {
        var bar = CreateTransparentPanel("AppBar", parent);
        AddLE(bar, minH: 64, prefH: 64);
        var hl = AddHLayout(bar, new RectOffset(4, 4, 8, 8));
        hl.childAlignment = TextAnchor.MiddleLeft;

        CreateIconButton("BackButton", bar, "<-", 48);

        var title = CreateTMP("TitleLabel", bar, "Label", 22, ColorOnSurface, FontStyles.Normal);
        SetSize(title.rectTransform, 0, 48);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        CreateIconButton("BookmarkButton", bar, "*", 48);
        CreateIconButton("MoreButton", bar, ":", 48);
    }

    void BuildScrollArea(RectTransform parent)
    {
        var scrollGO = CreateGO("ScrollArea", parent);
        AddLE(scrollGO, flexH: 1, minH: 100);

        var scrollImg = scrollGO.gameObject.AddComponent<Image>();
        scrollImg.color = ColorSurface;

        var scroll = scrollGO.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 30;

        var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGO.layer = LayerMask.NameToLayer("UI");
        var vp = vpGO.GetComponent<RectTransform>();
        vp.SetParent(scrollGO, false);
        SetStretch(vp);
        vpGO.GetComponent<Image>().color = Color.white;
        vpGO.GetComponent<Mask>().showMaskGraphic = false;

        var content = CreateGO("Content", vp);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var cl = content.gameObject.AddComponent<VerticalLayoutGroup>();
        cl.childControlWidth = true;
        cl.childControlHeight = true;
        cl.childForceExpandWidth = true;
        cl.childForceExpandHeight = false;

        scroll.viewport = vp;
        scroll.content = content;

        BuildHeader(content);
        BuildTextContent(content);
        BuildCardGrid(content);
    }

    void BuildHeader(RectTransform parent)
    {
        var header = CreatePanel("Header", parent, ColorSurface);
        AddLE(header, prefH: 152);
        var hl = AddHLayout(header, new RectOffset(16, 16, 8, 8), 24);
        hl.childAlignment = TextAnchor.UpperLeft;

        var img = CreatePanel("HeaderImage", header, ColorSurfaceContainerHigh);
        SetSize(img, 136, 136);

        var col = CreateGO("TextColumn", header);
        col.sizeDelta = new Vector2(200, 136);
        var vl = col.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 4;
        vl.childControlWidth = true;
        vl.childControlHeight = false;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        var h = CreateTMP("Headline", col, "Headline", 24, ColorOnSurface, FontStyles.Normal);
        SetSize(h.rectTransform, 0, 32);
        var s = CreateTMP("SupportingText", col, "supporting text", 16, ColorOnSurfaceVariant, FontStyles.Normal);
        SetSize(s.rectTransform, 0, 24);
        AddSpacer(col, 16);
        BuildDownloadButton(col);
    }

    void BuildDownloadButton(RectTransform parent)
    {
        var btn = new GameObject("DownloadButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.layer = LayerMask.NameToLayer("UI");
        var r = btn.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.sizeDelta = new Vector2(120, 40);
        btn.GetComponent<Image>().color = ColorPrimary;
        var le = btn.AddComponent<LayoutElement>();
        le.preferredWidth = 120;
        le.preferredHeight = 40;
        le.minHeight = 40;

        var t = CreateTMP("Label", r, "Download", 14, ColorOnPrimary, FontStyles.Bold);
        t.alignment = TextAlignmentOptions.Center;
        SetStretch(t.rectTransform);
    }

    void BuildTextContent(RectTransform parent)
    {
        var sec = CreatePanel("TextContent", parent, ColorSurface);
        var vl = sec.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(16, 16, 8, 8);
        vl.spacing = 8;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        var pd = CreateTMP("PublishedDate", sec, "Published date", 11, ColorOnSurfaceVariant, FontStyles.Bold);
        AddLE(pd.GetComponent<RectTransform>(), prefH: 16);

        var body = CreateTMP("BodyText", sec,
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
            "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.\n\n" +
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
            14, ColorOnSurface, FontStyles.Normal);
        body.enableWordWrapping = true;
        body.overflowMode = TextOverflowModes.Overflow;
        body.lineSpacing = 2;
    }

    void BuildCardGrid(RectTransform parent)
    {
        var sec = CreatePanel("CardGrid", parent, ColorSurface);
        var vl = sec.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(0, 0, 0, 32);
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        BuildSectionTitle(sec);

        var list = CreateGO("ListContainer", sec);
        var ll = list.gameObject.AddComponent<VerticalLayoutGroup>();
        ll.padding = new RectOffset(16, 16, 0, 0);
        ll.spacing = 16;
        ll.childControlWidth = true;
        ll.childControlHeight = true;
        ll.childForceExpandWidth = true;
        ll.childForceExpandHeight = false;

        for (int i = 1; i <= 3; i++)
            BuildListItem(list, $"ListItem{i:D2}", "Title",
                "Description duis aute irure dolor in reprehenderit in voluptate velit.",
                "Today", "23 min");
    }

    void BuildSectionTitle(RectTransform parent)
    {
        var bar = CreateGO("TitleHeader", parent);
        AddLE(bar, minH: 48, prefH: 48);
        var hl = AddHLayout(bar, new RectOffset(16, 4, 8, 8), 8);
        hl.childAlignment = TextAnchor.MiddleLeft;

        var t = CreateTMP("SectionTitleText", bar, "Section title", 24, ColorOnSurface, FontStyles.Normal);
        SetSize(t.rectTransform, 200, 32);
        CreateIconButton("SectionIcon", bar, "*", 32);
    }

    void BuildListItem(RectTransform parent, string name, string title, string desc, string date, string duration)
    {
        var item = CreateGO(name, parent);
        AddLE(item, minH: 120, prefH: 120);
        var hl = AddHLayout(item, spacing: 16);
        hl.childAlignment = TextAnchor.UpperLeft;

        var img = CreatePanel("Image", item, ColorSurfaceContainerHigh);
        SetSize(img, 120, 120);

        var col = CreateGO("Content", item);
        col.sizeDelta = new Vector2(220, 120);
        var vl = col.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 4;
        vl.childControlWidth = true;
        vl.childControlHeight = false;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        var tt = CreateTMP("Title", col, title, 22, ColorOnSurface, FontStyles.Normal);
        SetSize(tt.rectTransform, 0, 28);
        var dt = CreateTMP("Description", col, desc, 14, ColorOnSurfaceVariant, FontStyles.Normal);
        dt.enableWordWrapping = true;
        dt.overflowMode = TextOverflowModes.Ellipsis;
        dt.maxVisibleLines = 2;
        SetSize(dt.rectTransform, 0, 40);
        AddSpacer(col, 8);

        var meta = CreateGO("MetaRow", col);
        meta.sizeDelta = new Vector2(0, 24);
        var ml = AddHLayout(meta, spacing: 4);
        ml.childAlignment = TextAnchor.MiddleLeft;

        CreateTMPIcon("AddCircle", meta, "+", 16, ColorPrimary);
        var dateT = CreateTMP("Date", meta, date, 12, ColorOnSurfaceVariant, FontStyles.Normal);
        SetSize(dateT.rectTransform, 38, 16);
        var dot = CreateTMP("Dot", meta, ".", 12, ColorOnSurfaceVariant, FontStyles.Bold);
        SetSize(dot.rectTransform, 8, 16);
        dot.alignment = TextAlignmentOptions.Center;
        var dur = CreateTMP("Duration", meta, duration, 12, ColorOnSurfaceVariant, FontStyles.Normal);
        SetSize(dur.rectTransform, 50, 16);
        AddFlexSpacer(meta);

        CreateTMPIcon("PlayBtn", meta, ">", 16, ColorOnSurface);
    }

    void BuildMediaPlayer(RectTransform parent)
    {
        var player = CreatePanel("MediaPlayer", parent, ColorSurface);
        AddLE(player, minH: 68, prefH: 68);

        var trackBg = CreatePanel("ProgressTrack", player, ColorSecondaryContainer);
        trackBg.anchorMin = new Vector2(0, 1);
        trackBg.anchorMax = new Vector2(1, 1);
        trackBg.pivot = new Vector2(0.5f, 1);
        trackBg.anchoredPosition = Vector2.zero;
        trackBg.sizeDelta = new Vector2(0, 4);

        var fill = CreatePanel("ProgressFill", trackBg, ColorPrimary);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0.2f, 1);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        var area = CreatePanel("PlayerContent", player, ColorSurfaceContainer);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = Vector2.zero;
        area.offsetMax = new Vector2(0, -4);
        var aHL = AddHLayout(area, new RectOffset(0, 20, 0, 0), 16);
        aHL.childAlignment = TextAnchor.MiddleLeft;

        var art = CreatePanel("AlbumArt", area, ColorSurfaceContainerHigh);
        SetSize(art, 64, 64);

        var info = CreateGO("TrackInfo", area);
        info.sizeDelta = new Vector2(200, 40);
        info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var il = info.gameObject.AddComponent<VerticalLayoutGroup>();
        il.childAlignment = TextAnchor.MiddleLeft;
        il.spacing = 2;
        il.childControlWidth = true;
        il.childControlHeight = false;
        il.childForceExpandWidth = true;
        il.childForceExpandHeight = false;

        var tt = CreateTMP("TrackTitle", info, "Title", 14, ColorOnSurface, FontStyles.Normal);
        SetSize(tt.rectTransform, 0, 20);
        var ta = CreateTMP("TrackArtist", info, "Artist", 12, ColorOnSurfaceVariant, FontStyles.Normal);
        SetSize(ta.rectTransform, 0, 16);

        var ctrl = CreateHGroup("Controls", area, 12);
        ctrl.sizeDelta = new Vector2(60, 24);
        ctrl.gameObject.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

        CreateTMPIcon("PauseBtn", ctrl, "||", 16, ColorOnSurface);
        CreateTMPIcon("SkipBtn", ctrl, ">|", 12, ColorOnSurface);
    }

    void BuildNavigationHandle(RectTransform parent)
    {
        var nav = CreatePanel("NavigationBar", parent, ColorSurface);
        AddLE(nav, prefH: 24, minH: 24);

        var handle = CreatePanel("Handle", nav, ColorOnSurface);
        handle.anchorMin = new Vector2(0.5f, 0.5f);
        handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(108, 4);
    }

    #region Helpers

    RectTransform CreateGO(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        var r = obj.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        return r;
    }

    RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.layer = LayerMask.NameToLayer("UI");
        var r = obj.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        return r;
    }

    RectTransform CreateTransparentPanel(string name, Transform parent)
    {
        var r = CreateGO(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
        return r;
    }

    TextMeshProUGUI CreateTMP(string name, Transform parent, string text, float fontSize, Color color, FontStyles style)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.GetComponent<RectTransform>().SetParent(parent, false);
        var tmp = obj.GetComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    void CreateTMPIcon(string name, Transform parent, string text, float fontSize, Color color)
    {
        var obj = CreateGO(name, parent);
        obj.sizeDelta = new Vector2(24, 24);
        var tmp = CreateTMP("Icon", obj, text, fontSize, color, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.Center;
        SetStretch(tmp.rectTransform);
    }

    RectTransform CreateIconButton(string name, Transform parent, string iconChar, float size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.layer = LayerMask.NameToLayer("UI");
        var r = obj.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.sizeDelta = new Vector2(size, size);
        var img = obj.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0);
        obj.GetComponent<Button>().targetGraphic = img;
        var tmp = CreateTMP("Icon", r, iconChar, size * 0.4f, ColorOnSurface, FontStyles.Normal);
        tmp.alignment = TextAlignmentOptions.Center;
        SetStretch(tmp.rectTransform);
        return r;
    }

    RectTransform CreateHGroup(string name, Transform parent, float spacing)
    {
        var r = CreateGO(name, parent);
        var hl = r.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = spacing;
        hl.childControlWidth = false;
        hl.childControlHeight = false;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        return r;
    }

    HorizontalLayoutGroup AddHLayout(RectTransform r, RectOffset padding = null, float spacing = 0)
    {
        var hl = r.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.padding = padding ?? new RectOffset();
        hl.spacing = spacing;
        hl.childControlWidth = false;
        hl.childControlHeight = false;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        return hl;
    }

    LayoutElement AddLE(RectTransform r, float minH = -1, float prefH = -1, float flexH = -1)
    {
        var le = r.gameObject.AddComponent<LayoutElement>();
        if (minH >= 0) le.minHeight = minH;
        if (prefH >= 0) le.preferredHeight = prefH;
        if (flexH >= 0) le.flexibleHeight = flexH;
        return le;
    }

    void AddFlexSpacer(Transform parent)
    {
        var obj = CreateGO("Spacer", parent);
        obj.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
    }

    void AddSpacer(Transform parent, float height)
    {
        var obj = CreateGO("Spacer", parent);
        obj.sizeDelta = new Vector2(0, height);
    }

    void SetStretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    void SetSize(RectTransform r, float w, float h)
    {
        r.sizeDelta = new Vector2(w, h);
    }

    #endregion
}
