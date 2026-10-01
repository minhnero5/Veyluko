using System.IO;
using UnityEditor;
using UnityEngine;

public class SliceSprite : EditorWindow
{
    private DefaultAsset targetFolder;
    private bool includeSubfolders = true;
    private float alphaThreshold = 0.5f;
    private Vector2 pivot = new Vector2(0.5f, 0.5f);
    private float pixelsPerUnit = 100f;

    [MenuItem("Tools/Slice & Trim")]
    private static void ShowWindow() => GetWindow<SliceSprite>("Slice & Trim (1 cell)");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Cắt 1 ô + Trim viền trong suốt cho PNG trong folder", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Folder (Project)", targetFolder, typeof(DefaultAsset), false);
        includeSubfolders = EditorGUILayout.Toggle("Include subfolders", includeSubfolders);
        alphaThreshold = EditorGUILayout.Slider("Alpha threshold", alphaThreshold, 0f, 1f);
        pivot = EditorGUILayout.Vector2Field("Pivot", pivot);
        pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(targetFolder == null))
        {
            if (GUILayout.Button("Run Slice & Trim (metadata only)"))
            {
                var folderPath = AssetDatabase.GetAssetPath(targetFolder);
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    EditorUtility.DisplayDialog("Error", "Hãy chọn 1 folder trong cửa sổ Project.", "OK");
                    return;
                }
                ProcessFolder(folderPath, includeSubfolders, overwritePng: false);
            }

            if (GUILayout.Button("Run & Overwrite PNG (Slice + Trim + Ghi đè file)"))
            {
                if (!EditorUtility.DisplayDialog(
                        "Xác nhận ghi đè",
                        "Thao tác này sẽ GHI ĐÈ các file PNG gốc bằng ảnh đã trim. Bạn có muốn tiếp tục?",
                        "Ghi đè", "Huỷ")) return;

                var folderPath = AssetDatabase.GetAssetPath(targetFolder);
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    EditorUtility.DisplayDialog("Error", "Hãy chọn 1 folder trong cửa sổ Project.", "OK");
                    return;
                }
                ProcessFolder(folderPath, includeSubfolders, overwritePng: true);
            }
        }
    }

    private void ProcessFolder(string folderPath, bool recursive, bool overwritePng)
    {
        int processed = 0;
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;

                if (!recursive)
                {
                    var parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
                    if (parent != folderPath) continue;
                }

                if (SliceAndTrimSingle(assetPath, overwritePng)) processed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Done", $"Processed {processed} texture(s).", "OK");
        Debug.Log($"[SliceTrimOneCell] overwrite={overwritePng} processed={processed} folder={folderPath}");
    }

    private bool SliceAndTrimSingle(string assetPath, bool overwritePng)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return false;

        // Đảm bảo có thể đọc & thiết lập cơ bản
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single; // tạm
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.isReadable = true; // để có thể GetPixels khi cần
        importer.SaveAndReimport();

        // Tính trimRect (đọc trực tiếp file PNG để luôn readable)
        if (!TryComputeTrimRectFromPngFile(assetPath, out Rect trimRect, alphaThreshold))
        {
            // rỗng hoàn toàn → dùng full rect
            var tex0 = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex0 == null)
            {
                Debug.LogWarning($"Cannot load texture: {assetPath}");
                return false;
            }
            trimRect = new Rect(0, 0, tex0.width, tex0.height);
        }

        if (!overwritePng)
        {
            // Không ghi đè file: dùng spritesheet Multiple với đúng 1 cell = trimRect
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = new[]
            {
                new SpriteMetaData
                {
                    name = Path.GetFileNameWithoutExtension(assetPath),
                    rect = trimRect,
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = pivot,
                    border = Vector4.zero
                }
            };
            importer.SaveAndReimport();

            importer.isReadable = false;
            importer.SaveAndReimport();
            return true;
        }

        // --- GHI ĐÈ PNG BẰNG ẢNH ĐÃ TRIM ---
        var fullTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (fullTex == null)
        {
            Debug.LogWarning($"Cannot load texture for overwrite: {assetPath}");
            return false;
        }

        int x = Mathf.RoundToInt(trimRect.x);
        int y = Mathf.RoundToInt(trimRect.y);
        int w = Mathf.RoundToInt(trimRect.width);
        int h = Mathf.RoundToInt(trimRect.height);

        // Sao chép vùng trim
        Color[] cutPixels;
        try
        {
            cutPixels = fullTex.GetPixels(x, y, w, h);
        }
        catch
        {
            // Trong một số case GetPixels vẫn fail nếu importer chưa kịp reimport
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            fullTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (fullTex == null) return false;
            cutPixels = fullTex.GetPixels(x, y, w, h);
        }

        var newTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        newTex.SetPixels(cutPixels);
        newTex.Apply();

        // Ghi đè file PNG
        byte[] pngData = newTex.EncodeToPNG();
        Object.DestroyImmediate(newTex);

        if (pngData == null || pngData.Length == 0)
        {
            Debug.LogWarning($"Encode PNG failed: {assetPath}");
            return false;
        }

        File.WriteAllBytes(ToAbsolutePath(assetPath), pngData);

        // Reimport và set về Single (full rect) + pivot
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return false;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = pivot;          // << dùng spritePivot, không dùng spriteAlignment
        importer.isReadable = false;           // tắt lại cho nhẹ
        importer.SaveAndReimport();

        return true;
    }

    /// <summary>
    /// Đọc bytes PNG trực tiếp để tạo Texture2D tạm (readable) rồi tính trim.
    /// Không phụ thuộc importer.isReadable.
    /// </summary>
    private bool TryComputeTrimRectFromPngFile(string assetPath, out Rect rect, float aThresh)
    {
        rect = default;

        var absolute = ToAbsolutePath(assetPath);
        if (!File.Exists(absolute)) return false;

        byte[] bytes = File.ReadAllBytes(absolute);
        var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        if (!tmp.LoadImage(bytes, false))
        {
            Object.DestroyImmediate(tmp);
            return false;
        }

        try
        {
            var pixels = tmp.GetPixels32(); // luôn readable
            int w = tmp.width, h = tmp.height;

            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    var p = pixels[row + x];
                    if (p.a / 255f > aThresh)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX < minX || maxY < minY) return false;

            rect = new Rect(minX, minY, (maxX - minX + 1), (maxY - minY + 1)); // gốc bottom-left
            return true;
        }
        finally
        {
            Object.DestroyImmediate(tmp);
        }
    }

    private static string ToAbsolutePath(string assetPath)
    {
        var projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        return Path.Combine(projectRoot, assetPath).Replace("\\", "/");
    }
}
