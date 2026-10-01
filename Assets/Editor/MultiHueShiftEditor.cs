using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MultiColorLabEditor : EditorWindow
{
    // ====== Data ======
    private List<Texture2D> sourceTextures = new List<Texture2D>();
    private Dictionary<Texture2D, Texture2D> previewMap = new Dictionary<Texture2D, Texture2D>();

    // ====== Controls ======
    // --- Group 1: Basic (Exposure/Gamma/Brightness/Contrast)
    private float exposureEV = 0f;            // -5..+5 EV
    private float gammaVal   = 1.0f;          // 0.1..3
    private float brightness = 0f;            // -1..+1 (add)
    private float contrast   = 0f;            // -1..+1 (around 0.5)

    // --- Group 2: Color (Temp/Tint/Hue/Saturation/Vibrance)
    private float temperature = 0f;           // -100..+100 (blue<->yellow)
    private float tint        = 0f;           // -100..+100 (green<->magenta)
    private float hueShiftDeg = 0f;           // -180..+180
    private float saturation  = 0f;           // -1..+1 (delta)
    private float vibrance    = 0f;           // -1..+1 (more on low-sat)

    // --- Group 3: Advanced (Shadows/Highlights/Clarity/Sharpen)
    private float shadowsAdj   = 0f;          // -1..+1  (<0 tối đi, >0 sáng lên phần shadow)
    private float highlightsAdj = 0f;         // -1..+1  (<0 giảm cháy sáng, >0 tăng sáng highlight)
    private float clarity      = 0f;          // -1..+1 (midtone contrast)
    private float sharpen      = 0f;          // 0..1

    // --- Group 4: Color Filters (3-way Color Balance / Levels)
    private Color shadowsTint    = new Color(0,0,0,0);
    private Color midtonesTint   = new Color(0,0,0,0);
    private Color highlightsTint = new Color(0,0,0,0);

    private float levelBlack    = 0f;         // 0..0.5
    private float levelWhite    = 1f;         // 0.5..1
    private float levelMidGamma = 1.0f;       // 0.1..3

    // ====== UI ======
    private Vector2 scrollPreview;
    private Vector2 scrollControls;
    private const float previewAreaMaxHeight = 520f;

    // Preview perf
    private int  previewMaxSize     = 128;    // cạnh dài tối đa của preview
    private bool useLowResPreview   = true;   // bật/tắt preview low-res

    // Foldouts
    private bool foldBasic    = true;
    private bool foldColor    = true;
    private bool foldAdvanced = true;
    private bool foldFilters  = true;

    [MenuItem("Tools/Multi Sprite Color Lab")]
    public static void ShowWindow()
    {
        GetWindow<MultiColorLabEditor>("Multi Color Lab");
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(); // LEFT controls - RIGHT preview

        // ===== LEFT: Controls =====
        EditorGUILayout.BeginVertical(GUILayout.Width(420));
        GUILayout.Label("🎛️ Bảng điều khiển", EditorStyles.boldLabel);
        DrawDropArea();

        scrollControls = EditorGUILayout.BeginScrollView(scrollControls);

        // ===== Group 1: Basic =====
        GUILayout.Space(6);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        foldBasic = EditorGUILayout.Foldout(foldBasic, "🌓  Basic (Exposure / Tone)", true, EditorStyles.foldoutHeader);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↺ Reset nhóm", GUILayout.Height(20), GUILayout.Width(110)))
        {
            ResetBasic();
            UpdatePreviews();
        }
        EditorGUILayout.EndHorizontal();

        bool changed = false;
        EditorGUI.BeginChangeCheck();
        if (foldBasic)
        {
            exposureEV = EditorGUILayout.Slider(new GUIContent("Exposure (EV)"), exposureEV, -5f, 5f);
            gammaVal   = EditorGUILayout.Slider(new GUIContent("Gamma"),        gammaVal,   0.1f, 3f);
            brightness = EditorGUILayout.Slider(new GUIContent("Brightness"),   brightness, -1f, 1f);
            contrast   = EditorGUILayout.Slider(new GUIContent("Contrast"),     contrast,   -1f, 1f);
        }
        if (EditorGUI.EndChangeCheck()) changed = true;
        EditorGUILayout.EndVertical();

        // ===== Group 2: Color =====
        GUILayout.Space(6);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        foldColor = EditorGUILayout.Foldout(foldColor, "🎨  Color (WB / Hue / Sat / Vibrance)", true, EditorStyles.foldoutHeader);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↺ Reset nhóm", GUILayout.Height(20), GUILayout.Width(110)))
        {
            ResetColor();
            UpdatePreviews();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        if (foldColor)
        {
            GUILayout.Label("🌡️ White Balance", EditorStyles.miniBoldLabel);
            temperature = EditorGUILayout.Slider(new GUIContent("Temperature"), temperature, -100f, 100f);
            tint        = EditorGUILayout.Slider(new GUIContent("Tint"),        tint,        -100f, 100f);

            GUILayout.Space(4);
            GUILayout.Label("🎛 Hue/Saturation/Vibrance", EditorStyles.miniBoldLabel);
            hueShiftDeg = EditorGUILayout.Slider(new GUIContent("Hue (°)"),         hueShiftDeg, -180f, 180f);
            saturation  = EditorGUILayout.Slider(new GUIContent("Saturation Δ"),    saturation,  -1f,   1f);
            vibrance    = EditorGUILayout.Slider(new GUIContent("Vibrance"),        vibrance,    -1f,   1f);
        }
        if (EditorGUI.EndChangeCheck()) changed = true;
        EditorGUILayout.EndVertical();

        // ===== Group 3: Advanced =====
        GUILayout.Space(6);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        foldAdvanced = EditorGUILayout.Foldout(foldAdvanced, "✨  Advanced (Shadows / Highlights / Clarity / Sharpen)", true, EditorStyles.foldoutHeader);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↺ Reset nhóm", GUILayout.Height(20), GUILayout.Width(110)))
        {
            ResetAdvanced();
            UpdatePreviews();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        if (foldAdvanced)
        {
            shadowsAdj    = EditorGUILayout.Slider(new GUIContent("Shadows"),   shadowsAdj,   -1f, 1f);
            highlightsAdj = EditorGUILayout.Slider(new GUIContent("Highlights"),highlightsAdj,-1f, 1f);
            clarity       = EditorGUILayout.Slider(new GUIContent("Clarity (mid-contrast)"), clarity, -1f, 1f);
            sharpen       = EditorGUILayout.Slider(new GUIContent("Sharpen"), sharpen, 0f, 1f);
        }
        if (EditorGUI.EndChangeCheck()) changed = true;
        EditorGUILayout.EndVertical();

        // ===== Group 4: Color Filters =====
        GUILayout.Space(6);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        foldFilters = EditorGUILayout.Foldout(foldFilters, "📦  Color Filters (3‑Way Color Balance / Levels)", true, EditorStyles.foldoutHeader);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↺ Reset nhóm", GUILayout.Height(20), GUILayout.Width(110)))
        {
            ResetFilters();
            UpdatePreviews();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        if (foldFilters)
        {
            GUILayout.Label("🌗 3-Way Color Balance (Additive tint)", EditorStyles.miniBoldLabel);
            shadowsTint    = EditorGUILayout.ColorField(new GUIContent("Shadows Tint"),    shadowsTint,   true, false, false);
            midtonesTint   = EditorGUILayout.ColorField(new GUIContent("Midtones Tint"),   midtonesTint,  true, false, false);
            highlightsTint = EditorGUILayout.ColorField(new GUIContent("Highlights Tint"), highlightsTint, true, false, false);

            GUILayout.Space(4);
            GUILayout.Label("📈 Levels", EditorStyles.miniBoldLabel);
            levelBlack    = EditorGUILayout.Slider(new GUIContent("Black Point"), levelBlack, 0f, 0.5f);
            levelWhite    = EditorGUILayout.Slider(new GUIContent("White Point"), levelWhite, 0.5f, 1f);
            levelMidGamma = EditorGUILayout.Slider(new GUIContent("Mid (Gamma)"), levelMidGamma, 0.1f, 3f);
        }
        if (EditorGUI.EndChangeCheck()) changed = true;
        EditorGUILayout.EndVertical();

        GUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("↺ Reset TẤT CẢ", GUILayout.Height(24)))
        {
            ResetAll();
            changed = true;
        }
        if (GUILayout.Button("Cập nhật Preview", GUILayout.Height(24)))
        {
            changed = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView(); // end left scroll
        EditorGUILayout.EndVertical();   // end left

        // ===== RIGHT: Preview =====
        EditorGUILayout.BeginVertical();
        GUILayout.Label("👁️ Preview", EditorStyles.boldLabel);

        // Tùy chọn preview perf
        useLowResPreview = EditorGUILayout.Toggle(new GUIContent("Preview low-res"), useLowResPreview);
        previewMaxSize   = EditorGUILayout.IntSlider(new GUIContent("Kích thước preview tối đa"), previewMaxSize, 64, 1024);

        if (changed)
            UpdatePreviews();

        if (previewMap.Count > 0)
        {
            scrollPreview = EditorGUILayout.BeginScrollView(scrollPreview, GUILayout.Height(previewAreaMaxHeight));
            foreach (var tex in sourceTextures.ToArray())
            {
                if (!previewMap.ContainsKey(tex)) continue;

                GUILayout.BeginHorizontal("box");

                GUILayout.BeginVertical(GUILayout.Width(90));
                GUILayout.Label("Gốc", EditorStyles.miniBoldLabel);
                GUILayout.Label(tex, GUILayout.Width(80), GUILayout.Height(80));
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(90));
                GUILayout.Label("Sau chỉnh", EditorStyles.miniBoldLabel);
                GUILayout.Label(previewMap[tex], GUILayout.Width(80), GUILayout.Height(80));
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical(GUILayout.Width(200));
                GUILayout.Label(tex.name, EditorStyles.label);
                if (GUILayout.Button("❌ Xoá khỏi danh sách"))
                {
                    sourceTextures.Remove(tex);
                    previewMap.Remove(tex);
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button($"💾 Ghi đè tất cả ({previewMap.Count})", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Xác nhận", $"Ghi đè {previewMap.Count} texture đã chỉnh ở full-res?", "Ghi đè", "Hủy"))
                {
                    foreach (var tex in sourceTextures)
                    {
                        var full = ApplyAllAdjustmentsFull(tex); // xử lý lại full-res
                        OverwriteTexture(tex, full);
                    }
                    AssetDatabase.Refresh();
                    Debug.Log("✅ Đã ghi đè tất cả texture (full-res).");
                }
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            EditorGUILayout.HelpBox("Kéo sprite/texture vào ô bên trái để bắt đầu.", MessageType.Info);
        }

        if (GUILayout.Button("🗑 Xoá toàn bộ danh sách"))
        {
            sourceTextures.Clear();
            previewMap.Clear();
        }

        EditorGUILayout.EndVertical(); // right
        EditorGUILayout.EndHorizontal(); // split
    }

    // ===== Drag & Drop =====
    void DrawDropArea()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 70.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "KÉO SPRITE HOẶC TEXTURE VÀO ĐÂY", EditorStyles.helpBox);

        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                bool anyAdded = false;
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    Texture2D tex = null;
                    if (obj is Texture2D t) tex = t;
                    else if (obj is Sprite s && s.texture != null) tex = s.texture;

                    if (tex != null && !sourceTextures.Contains(tex))
                    {
                        sourceTextures.Add(tex);
                        anyAdded = true;
                    }
                }
                if (anyAdded) UpdatePreviews();
                evt.Use();
            }
        }
    }

    // ===== Preview/Processing pipeline =====
    void UpdatePreviews()
    {
        previewMap.Clear();
        foreach (var tex in sourceTextures)
        {
            Texture2D prev = useLowResPreview
                ? ApplyAllAdjustmentsPreview(tex, previewMaxSize)  // xử lý bản thu nhỏ
                : ApplyAllAdjustmentsFull(tex);                    // xử lý full-res
            previewMap[tex] = prev;
        }
    }

    Texture2D ApplyAllAdjustmentsPreview(Texture2D original, int maxSize)
    {
        MakeTextureReadable(original);

        float scale = Mathf.Min(1f, (float)maxSize / Mathf.Max(original.width, original.height));
        int w = Mathf.Max(1, Mathf.RoundToInt(original.width  * scale));
        int h = Mathf.Max(1, Mathf.RoundToInt(original.height * scale));

        Color[] src = (scale < 1f) ? DownscaleBilinear(original, w, h) : original.GetPixels();

        Color[] work = ProcessPipeline(src, w, h);

        var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.filterMode = original.filterMode;
        result.SetPixels(work);
        result.Apply(false, false);

        if (sharpen > 0.001f)
            result = ApplySharpen(result, sharpen);

        return result;
    }

    Texture2D ApplyAllAdjustmentsFull(Texture2D original)
    {
        MakeTextureReadable(original);

        int w = original.width;
        int h = original.height;

        Color[] src = original.GetPixels();

        Color[] work = ProcessPipeline(src, w, h);

        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.filterMode = original.filterMode;
        result.SetPixels(work);
        result.Apply(false, false);

        if (sharpen > 0.001f)
            result = ApplySharpen(result, sharpen);

        return result;
    }

    // Gom toàn bộ thuật toán xử lý màu vào 1 hàm chung
    Color[] ProcessPipeline(Color[] src, int w, int h)
    {
        Color[] work = new Color[src.Length];

        // --- 1) White balance + Exposure + Brightness/Contrast + Gamma (Basic + phần WB của Color)
        float tNorm = Mathf.Clamp(temperature / 100f, -1f, 1f);
        float tiNorm = Mathf.Clamp(tint / 100f, -1f, 1f);

        float rGain = 1f + 0.10f * tNorm + 0.05f * tiNorm;
        float gGain = 1f - 0.05f * tiNorm;
        float bGain = 1f - 0.10f * tNorm + 0.05f * tiNorm;

        float exposureMul = Mathf.Pow(2f, exposureEV);

        for (int i = 0; i < src.Length; i++)
        {
            Color c = src[i];

            // White balance
            c.r *= rGain; c.g *= gGain; c.b *= bGain;

            // Exposure
            c.r *= exposureMul; c.g *= exposureMul; c.b *= exposureMul;

            // Brightness + Contrast
            c.r = Mathf.Clamp01((c.r - 0.5f) * (1f + contrast) + 0.5f + brightness);
            c.g = Mathf.Clamp01((c.g - 0.5f) * (1f + contrast) + 0.5f + brightness);
            c.b = Mathf.Clamp01((c.b - 0.5f) * (1f + contrast) + 0.5f + brightness);

            // Gamma
            c.r = Mathf.Pow(Mathf.Clamp01(c.r), 1f / gammaVal);
            c.g = Mathf.Pow(Mathf.Clamp01(c.g), 1f / gammaVal);
            c.b = Mathf.Pow(Mathf.Clamp01(c.b), 1f / gammaVal);

            work[i] = new Color(c.r, c.g, c.b, src[i].a);
        }

        // --- 1b) Shadows / Highlights (Advanced)
        if (Mathf.Abs(shadowsAdj) > 1e-4f || Mathf.Abs(highlightsAdj) > 1e-4f)
        {
            for (int i = 0; i < work.Length; i++)
            {
                Color c = work[i];
                float Y = Luma(c);

                // weight cho vùng shadow & highlight
                float wSh = Mathf.Clamp01(1f - Smooth01((Y - 0.0f) / 0.5f)); // mạnh ở vùng tối
                float wHi = Mathf.Clamp01(Smooth01((Y - 0.5f) / 0.5f));      // mạnh ở vùng sáng

                // Áp chỉnh: thêm/giảm sáng theo trọng số
                float shAmt = shadowsAdj * wSh;
                float hiAmt = highlightsAdj * wHi;

                c.r = Mathf.Clamp01(c.r + shAmt + hiAmt);
                c.g = Mathf.Clamp01(c.g + shAmt + hiAmt);
                c.b = Mathf.Clamp01(c.b + shAmt + hiAmt);

                work[i] = new Color(c.r, c.g, c.b, work[i].a);
            }
        }

        // --- 2) Hue/Saturation/Vibrance (Color)
        for (int i = 0; i < work.Length; i++)
        {
            Color c = work[i];
            Color.RGBToHSV(new Color(c.r, c.g, c.b, 1f), out float hue, out float s, out float v);

            hue = (hue + (hueShiftDeg / 360f)) % 1f; if (hue < 0) hue += 1f;
            s   = Mathf.Clamp01(s + saturation);
            s   = Mathf.Clamp01(s + vibrance * (1f - s)); // vibrance mạnh hơn ở màu ít bão hoà

            Color outRGB = Color.HSVToRGB(hue, s, v);
            outRGB.a = c.a;
            work[i] = outRGB;
        }

        // --- 3) 3-Way Color Balance (Color Filters)
        for (int i = 0; i < work.Length; i++)
        {
            Color c = work[i];
            float Y = Luma(c);

            float wSh = Mathf.Clamp01(1f - Smooth01((Y - 0.0f) / 0.5f)); // mạnh ở vùng tối
            float wHi = Mathf.Clamp01(Smooth01((Y - 0.5f) / 0.5f));      // mạnh ở vùng sáng
            float wMi = Mathf.Clamp01(1f - wSh - wHi);

            c.r = Mathf.Clamp01(c.r + shadowsTint.r    * wSh + midtonesTint.r    * wMi + highlightsTint.r    * wHi);
            c.g = Mathf.Clamp01(c.g + shadowsTint.g    * wSh + midtonesTint.g    * wMi + highlightsTint.g    * wHi);
            c.b = Mathf.Clamp01(c.b + shadowsTint.b    * wSh + midtonesTint.b    * wMi + highlightsTint.b    * wHi);

            work[i] = new Color(c.r, c.g, c.b, work[i].a);
        }

        // --- 4) Levels (Color Filters)
        float black = Mathf.Min(levelBlack, levelWhite - 0.001f);
        float white = Mathf.Max(levelWhite, black + 0.001f);
        float invRange = 1f / (white - black);
        float midG = levelMidGamma;

        for (int i = 0; i < work.Length; i++)
        {
            Color c = work[i];
            c.r = Mathf.Pow(Mathf.Clamp01((c.r - black) * invRange), 1f / midG);
            c.g = Mathf.Pow(Mathf.Clamp01((c.g - black) * invRange), 1f / midG);
            c.b = Mathf.Pow(Mathf.Clamp01((c.b - black) * invRange), 1f / midG);
            work[i] = new Color(c.r, c.g, c.b, work[i].a);
        }

        // --- 5) Clarity (Advanced)
        if (Mathf.Abs(clarity) > 1e-4f)
        {
            for (int i = 0; i < work.Length; i++)
            {
                Color c = work[i];
                float y = Luma(c);
                float wMid = Mathf.Exp(-Mathf.Pow((y - 0.5f) / 0.25f, 2f)); // peak ở mid (0.5)
                float k = clarity * wMid;
                c.r = Mathf.Clamp01((c.r - 0.5f) * (1f + k) + 0.5f);
                c.g = Mathf.Clamp01((c.g - 0.5f) * (1f + k) + 0.5f);
                c.b = Mathf.Clamp01((c.b - 0.5f) * (1f + k) + 0.5f);
                work[i] = new Color(c.r, c.g, c.b, work[i].a);
            }
        }

        return work;
    }

    // Simple unsharp mask via 3x3 box blur high-pass
    Texture2D ApplySharpen(Texture2D tex, float amount)
    {
        int w = tex.width, h = tex.height;
        Color[] src = tex.GetPixels();
        Color[] dst = new Color[src.Length];

        System.Func<int,int,int> Idx = (x,y) => y * w + x;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color sum = Color.black;
                int count = 0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int yy = Mathf.Clamp(y + dy, 0, h - 1);
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = Mathf.Clamp(x + dx, 0, w - 1);
                        sum += src[Idx(xx, yy)];
                        count++;
                    }
                }
                Color blur = sum / count;
                Color orig = src[Idx(x, y)];
                Color high = orig - blur;
                Color outC = orig + amount * high;
                outC.a = orig.a;
                dst[Idx(x, y)] = ClampColor01(outC);
            }
        }

        Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outTex.filterMode = tex.filterMode;
        outTex.SetPixels(dst);
        outTex.Apply(false, false);
        return outTex;
    }

    // ===== Helpers =====
    float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    float Smooth01(float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
    Color ClampColor01(Color c) => new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), Mathf.Clamp01(c.a));

    // Downscale bilinear an toàn (CPU)
    Color[] DownscaleBilinear(Texture2D srcTex, int dstW, int dstH)
    {
        int sw = srcTex.width, sh = srcTex.height;
        Color[] src = srcTex.GetPixels();
        Color[] dst = new Color[dstW * dstH];

        float xRatio = (sw - 1f) / (float)dstW;
        float yRatio = (sh - 1f) / (float)dstH;

        for (int y = 0; y < dstH; y++)
        {
            float sy = y * yRatio;
            int y0 = (int)sy;
            int y1 = Mathf.Min(y0 + 1, sh - 1);
            float fy = sy - y0;

            for (int x = 0; x < dstW; x++)
            {
                float sx = x * xRatio;
                int x0 = (int)sx;
                int x1 = Mathf.Min(x0 + 1, sw - 1);
                float fx = sx - x0;

                int i00 = y0 * sw + x0;
                int i10 = y0 * sw + x1;
                int i01 = y1 * sw + x0;
                int i11 = y1 * sw + x1;

                Color c00 = src[i00];
                Color c10 = src[i10];
                Color c01 = src[i01];
                Color c11 = src[i11];

                Color c0 = Color.Lerp(c00, c10, fx);
                Color c1 = Color.Lerp(c01, c11, fx);
                Color c  = Color.Lerp(c0,  c1,  fy);

                dst[y * dstW + x] = c;
            }
        }
        return dst;
    }

    void OverwriteTexture(Texture2D original, Texture2D modified)
    {
        string path = AssetDatabase.GetAssetPath(original);

        // Save importer settings
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        bool wasReadable = importer != null && importer.isReadable;
        TextureImporterCompression compression = importer != null ? importer.textureCompression : TextureImporterCompression.Uncompressed;
        TextureImporterType textureType = importer != null ? importer.textureType : TextureImporterType.Default;
        TextureImporterNPOTScale npot = importer != null ? importer.npotScale : TextureImporterNPOTScale.None;

        // Write PNG
        byte[] pngData = modified.EncodeToPNG();
        File.WriteAllBytes(path, pngData);

        // Reimport and restore settings
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer is TextureImporter texImporter)
        {
            texImporter.isReadable = wasReadable;
            texImporter.textureCompression = compression;
            texImporter.textureType = textureType;
            texImporter.npotScale = npot;

            texImporter.ClearPlatformTextureSettings("Android");
            texImporter.ClearPlatformTextureSettings("iPhone");
            texImporter.ClearPlatformTextureSettings("Standalone");
            texImporter.ClearPlatformTextureSettings("WebGL");

            texImporter.SaveAndReimport();
        }
    }

    void MakeTextureReadable(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    // ===== Resets =====
    void ResetBasic()
    {
        exposureEV = 0f;
        gammaVal   = 1f;
        brightness = 0f;
        contrast   = 0f;
    }

    void ResetColor()
    {
        temperature = 0f;
        tint        = 0f;
        hueShiftDeg = 0f;
        saturation  = 0f;
        vibrance    = 0f;
    }

    void ResetAdvanced()
    {
        shadowsAdj    = 0f;
        highlightsAdj = 0f;
        clarity       = 0f;
        sharpen       = 0f;
    }

    void ResetFilters()
    {
        shadowsTint    = Color.black;
        midtonesTint   = Color.black;
        highlightsTint = Color.black;

        levelBlack     = 0f;
        levelWhite     = 1f;
        levelMidGamma  = 1f;
    }

    void ResetAll()
    {
        ResetBasic();
        ResetColor();
        ResetAdvanced();
        ResetFilters();
    }
}
