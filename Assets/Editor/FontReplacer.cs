using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FontReplacer : EditorWindow
{
    private Font newUIFont;
    private TMP_FontAsset newTMPFont;

    [MenuItem("Tools/Font Replacer (Full)")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacer>("Font Replacer");
    }

    void OnGUI()
    {
        GUILayout.Label("Replace Fonts in Scene, Prefabs & All Scenes", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        newUIFont = (Font)EditorGUILayout.ObjectField("New UI Font (Text)", newUIFont, typeof(Font), false);
        newTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New TMP Font", newTMPFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Replace Fonts in Current Scene"))
        {
            ReplaceFontsInScene();
        }

        if (GUILayout.Button("Replace Fonts in All Prefabs"))
        {
            ReplaceFontsInPrefabs();
        }

        if (GUILayout.Button("Replace Fonts in All Scenes"))
        {
            ReplaceFontsInAllScenes();
        }

        if (GUILayout.Button("🔥 Replace EVERYTHING"))
        {
            ReplaceFontsInScene();
            ReplaceFontsInPrefabs();
            ReplaceFontsInAllScenes();
        }
    }

    // ================= SCENE (CURRENT) =================
    void ReplaceFontsInScene()
    {
        if (!ValidateFonts()) return;

        int count = ReplaceFontsInCurrentOpenedScene();
        Debug.Log($"[Current Scene] Font replaced on {count} components.");
    }

    int ReplaceFontsInCurrentOpenedScene()
    {
        int count = 0;

        // UI.Text
        Text[] uiTexts = FindObjectsOfType<Text>(true);
        foreach (Text t in uiTexts)
        {
            if (newUIFont != null && t.font != newUIFont)
            {
                Undo.RecordObject(t, "Replace UI Font");
                t.font = newUIFont;
                EditorUtility.SetDirty(t);
                count++;
            }
        }

        // TMP UGUI
        TextMeshProUGUI[] tmpTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI t in tmpTexts)
        {
            if (newTMPFont != null && t.font != newTMPFont)
            {
                Undo.RecordObject(t, "Replace TMP Font");
                t.font = newTMPFont;
                EditorUtility.SetDirty(t);
                count++;
            }
        }

        return count;
    }

    // ================= PREFABS =================
    void ReplaceFontsInPrefabs()
    {
        if (!ValidateFonts()) return;

        string[] allPrefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in allPrefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            bool changed = false;

            // UI.Text
            Text[] uiTexts = root.GetComponentsInChildren<Text>(true);
            foreach (Text t in uiTexts)
            {
                if (newUIFont != null && t.font != newUIFont)
                {
                    t.font = newUIFont;
                    EditorUtility.SetDirty(t);
                    changed = true;
                    count++;
                }
            }

            // TMP UGUI
            TextMeshProUGUI[] tmpTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI t in tmpTexts)
            {
                if (newTMPFont != null && t.font != newTMPFont)
                {
                    t.font = newTMPFont;
                    EditorUtility.SetDirty(t);
                    changed = true;
                    count++;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[Prefab] Updated: {path}");
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[Prefabs] Font replaced on {count} components.");
    }

    // ================= ALL SCENES =================
    void ReplaceFontsInAllScenes()
    {
        if (!ValidateFonts()) return;

        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
        int totalCount = 0;

        foreach (string guid in sceneGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            int count = ReplaceFontsInCurrentOpenedScene();

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Scene] Updated: {path} ({count} items)");
            }

            totalCount += count;
        }

        Debug.Log($"[All Scenes] Font replaced on {totalCount} components.");
    }

    // ================= VALIDATION =================
    bool ValidateFonts()
    {
        if (newUIFont == null && newTMPFont == null)
        {
            Debug.LogWarning("Please assign at least one font!");
            return false;
        }
        return true;
    }
}