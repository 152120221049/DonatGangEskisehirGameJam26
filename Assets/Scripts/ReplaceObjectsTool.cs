#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ReplaceObjectsTool : EditorWindow
{
    GameObject targetToReplace;    // The "Old" object type to find
    GameObject replacementPrefab; // The "New" object to swap in

    [MenuItem("Tools/Replace Objects Tool")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceObjectsTool>("Replace Objects");
    }

    void OnGUI()
    {
        GUILayout.Label("Scene Search & Replace", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox("This tool will find EVERY instance of the 'Target' in the scene and replace it with the 'Replacement'.", MessageType.Warning);

        targetToReplace = (GameObject)EditorGUILayout.ObjectField(
            "Target (Old Object)", targetToReplace, typeof(GameObject), true);

        replacementPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Replacement (New Prefab)", replacementPrefab, typeof(GameObject), false);

        GUILayout.Space(10);

        if (GUILayout.Button("Find and Replace All in Scene"))
        {
            if (targetToReplace == null || replacementPrefab == null)
            {
                EditorUtility.DisplayDialog("Hata", "Lütfen hem hedefi hem de yeni nesneyi seç!", "Tamam");
                return;
            }

            ReplaceAllInScene();
        }
    }

    void ReplaceAllInScene()
    {
        // 1. Find all GameObjects in the active scene
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> matches = new List<GameObject>();

        // 2. Identify which ones match our target
        GameObject targetSource = PrefabUtility.GetCorrespondingObjectFromSource(targetToReplace);
        if (targetSource == null) targetSource = targetToReplace;

        foreach (GameObject go in allObjects)
        {
            // Match if it's the same prefab instance OR has the exact same name
            GameObject goSource = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if ((goSource != null && goSource == targetSource) || go.name == targetToReplace.name)
            {
                // Don't replace the replacement itself if it's in the scene!
                if (go != replacementPrefab) matches.Add(go);
            }
        }

        if (matches.Count == 0)
        {
            EditorUtility.DisplayDialog("Bilgi", "Sahnede eşleşen nesne bulunamadı.", "Tamam");
            return;
        }

        if (!EditorUtility.DisplayDialog("Onay", matches.Count + " nesne değiştirilecek. Emin misin?", "Evet", "Hayır"))
            return;

        int count = 0;
        foreach (GameObject source in matches)
        {
            if (source == null) continue;

            GameObject newObj;
            if (PrefabUtility.IsPartOfAnyPrefab(replacementPrefab))
            {
                newObj = (GameObject)PrefabUtility.InstantiatePrefab(replacementPrefab);
            }
            else
            {
                newObj = Instantiate(replacementPrefab);
            }

            if (newObj == null) continue;

            Undo.RegisterCreatedObjectUndo(newObj, "Replace Object");

            // Copy transform data (Position and Rotation only, keep original scale)
            newObj.transform.SetParent(source.transform.parent);
            newObj.transform.position = source.transform.position;
            newObj.transform.rotation = source.transform.rotation;
            newObj.name = replacementPrefab.name; // Clean up the name

            Undo.DestroyObjectImmediate(source);
            count++;
        }

        Debug.Log("[ReplaceTool] Successfully swapped " + count + " objects in the scene.");
    }
}
#endif
