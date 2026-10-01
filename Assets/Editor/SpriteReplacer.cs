using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class SpriteReplacer : EditorWindow
{
    private List<Sprite> oldSprites = new List<Sprite>();
    private List<Sprite> newSprites = new List<Sprite>();
    private DefaultAsset targetFolder;
    private Vector2 scroll;
    private bool includeBuildScenes = true; // ✅ Tự động lấy scene trong Build Settings

    [MenuItem("Tools/Multi Sprite Replacer (Auto Build Scenes)")]
    public static void ShowWindow()
    {
        GetWindow<SpriteReplacer>("Sprite Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("🔁 Multi Sprite Replacer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        includeBuildScenes = EditorGUILayout.Toggle("📜 Replace in Build Settings Scenes", includeBuildScenes);
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("📁 Target Folder (optional)", targetFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space(10);
        GUILayout.Label("🖼 Sprite Pairs (Old → New)", EditorStyles.boldLabel);

        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag Sprites Here", EditorStyles.helpBox);
        HandleSpriteDragAndDrop(dropArea);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(250));
        for (int i = 0; i < oldSprites.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            DrawSpritePreview(oldSprites[i]);
            oldSprites[i] = (Sprite)EditorGUILayout.ObjectField(oldSprites[i], typeof(Sprite), false, GUILayout.Width(100));
            GUILayout.Label("→", GUILayout.Width(20));
            DrawSpritePreview(newSprites[i]);
            newSprites[i] = (Sprite)EditorGUILayout.ObjectField(newSprites[i], typeof(Sprite), false, GUILayout.Width(100));
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                oldSprites.RemoveAt(i);
                newSprites.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        EditorGUILayout.EndScrollView();

        if (oldSprites.Count > 0 && GUILayout.Button("🗑 Clear Sprite List"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Clear all sprite pairs?", "Yes", "No"))
            {
                oldSprites.Clear();
                newSprites.Clear();
            }
        }

        GUILayout.Space(10);
        if (GUILayout.Button("🚀 Replace Sprites Now"))
        {
            if (!ValidateInputs()) return;
            int total = 0;

            if (includeBuildScenes)
                total += ReplaceInBuildScenes();

            if (targetFolder != null)
                total += ReplaceInFolder();

            EditorUtility.DisplayDialog("✅ Done", $"Replaced {total} sprite reference(s).", "OK");
        }
    }

    #region --- Drag & UI ---
    private void HandleSpriteDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object draggedObj in DragAndDrop.objectReferences)
                {
                    if (draggedObj is Sprite sprite && !oldSprites.Contains(sprite))
                    {
                        oldSprites.Add(sprite);
                        newSprites.Add(null);
                    }
                    else if (draggedObj is Texture2D texture)
                    {
                        string path = AssetDatabase.GetAssetPath(texture);
                        Object[] subs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                        foreach (var s in subs)
                        {
                            if (s is Sprite sp && !oldSprites.Contains(sp))
                            {
                                oldSprites.Add(sp);
                                newSprites.Add(null);
                            }
                        }
                    }
                }
                evt.Use();
            }
        }
    }

    private void DrawSpritePreview(Sprite sprite)
    {
        if (sprite != null)
        {
            Texture2D tex = AssetPreview.GetAssetPreview(sprite) ?? AssetPreview.GetMiniThumbnail(sprite);
            GUILayout.Box(tex, GUILayout.Width(40), GUILayout.Height(40));
        }
        else
            GUILayout.Box("", GUILayout.Width(40), GUILayout.Height(40));
    }
    #endregion

    #region --- Validation ---
    private bool ValidateInputs()
    {
        if (oldSprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No sprite pairs to replace.", "OK");
            return false;
        }

        for (int i = 0; i < oldSprites.Count; i++)
        {
            if (oldSprites[i] == null || newSprites[i] == null)
            {
                EditorUtility.DisplayDialog("Error", $"Sprite pair at row {i + 1} is incomplete.", "OK");
                return false;
            }
        }
        return true;
    }
    #endregion

    #region --- Replace Logic ---
    private int ReplaceInFolder()
    {
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
        int replaceCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            bool modified = false;

            foreach (Object asset in assets)
            {
                if (asset == null) continue;
                SerializedObject so = new SerializedObject(asset);
                SerializedProperty prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        for (int i = 0; i < oldSprites.Count; i++)
                        {
                            if (prop.objectReferenceValue == oldSprites[i])
                            {
                                prop.objectReferenceValue = newSprites[i];
                                modified = true;
                                replaceCount++;
                                break;
                            }
                        }
                    }
                }

                if (modified)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                }
            }
        }

        AssetDatabase.SaveAssets();
        return replaceCount;
    }

    private int ReplaceInBuildScenes()
    {
        int total = 0;
        var buildScenes = EditorBuildSettings.scenes;

        foreach (var s in buildScenes)
        {
            if (!s.enabled) continue;
            var path = s.path;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            int replaced = ReplaceInSceneObjects();
            total += replaced;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"✅ Scene '{Path.GetFileNameWithoutExtension(path)}': replaced {replaced} sprite(s)");
        }

        return total;
    }

    private int ReplaceInSceneObjects()
    {
        int replaced = 0;
        foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
        {
            // SpriteRenderer
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                for (int i = 0; i < oldSprites.Count; i++)
                    if (sr.sprite == oldSprites[i])
                    {
                        sr.sprite = newSprites[i];
                        replaced++;
                        EditorUtility.SetDirty(sr);
                    }
            }

            // UI.Image
            var img = obj.GetComponent<Image>();
            if (img != null)
            {
                for (int i = 0; i < oldSprites.Count; i++)
                    if (img.sprite == oldSprites[i])
                    {
                        img.sprite = newSprites[i];
                        replaced++;
                        EditorUtility.SetDirty(img);
                    }
            }

            // ✅ Sprite trong script custom
            var comps = obj.GetComponents<MonoBehaviour>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                SerializedObject so = new SerializedObject(c);
                SerializedProperty prop = so.GetIterator();
                bool modified = false;

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        for (int i = 0; i < oldSprites.Count; i++)
                        {
                            if (prop.objectReferenceValue == oldSprites[i])
                            {
                                prop.objectReferenceValue = newSprites[i];
                                modified = true;
                                replaced++;
                                break;
                            }
                        }
                    }
                }

                if (modified)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(c);
                }
            }
        }
        return replaced;
    }
    #endregion
}
