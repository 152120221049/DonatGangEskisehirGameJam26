#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class PrefabBrushTool : EditorWindow
{
    private GameObject brushPrefab;
    private bool isPainting = false;
    private bool alignToSurface = true;
    private bool snapToGrid = false;
    private float gridSize = 1f;
    private float brushSpacing = 1f;
    private Vector3 lastPaintPos = Vector3.positiveInfinity;

    [MenuItem("Tools/Prefab Brush Tool")]
    public static void ShowWindow()
    {
        GetWindow<PrefabBrushTool>("Prefab Brush");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Brush Settings", EditorStyles.boldLabel);
        
        brushPrefab = (GameObject)EditorGUILayout.ObjectField("Brush Prefab", brushPrefab, typeof(GameObject), false);
        alignToSurface = EditorGUILayout.Toggle("Align to Surface Normal", alignToSurface);
        
        GUILayout.Space(5);
        snapToGrid = EditorGUILayout.Toggle("Snap to Grid", snapToGrid);
        if (snapToGrid)
        {
            gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);
            // Don't allow 0 or negative grid sizes
            if (gridSize <= 0.01f) gridSize = 0.01f;
        }

        GUILayout.Space(5);
        brushSpacing = EditorGUILayout.FloatField("Brush Spacing", brushSpacing);
        if (brushSpacing < 0.1f) brushSpacing = 0.1f;

        GUILayout.Space(10);

        if (isPainting)
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("PAINTING ACTIVE - Click to Stop", GUILayout.Height(30)))
            {
                StopPainting();
            }
        }
        else
        {
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Start Painting", GUILayout.Height(30)))
            {
                if (brushPrefab == null)
                {
                    EditorUtility.DisplayDialog("Error", "Lütfen önce bir Prefab seç!", "OK");
                    return;
                }
                StartPainting();
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.HelpBox("NASIL KULLANILIR:\n1. 'Start Painting'e tıkla.\n2. Scene ekranında CTRL + SOL TIK (veya basılı tutarak sürükle) ile prefablari fırça gibi yerleştir.\n3. İşin bitince ESC'ye bas veya durdur.", MessageType.Info);
    }

    private void StartPainting()
    {
        isPainting = true;
        // Subscribe to the Scene View event
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void StopPainting()
    {
        isPainting = false;
        // Unsubscribe
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnDestroy()
    {
        StopPainting();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting || brushPrefab == null) return;

        Event e = Event.current;

        // Use CTRL + Left Click or Drag to paint
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.control)
        {
            // Cast a ray from the mouse position into the 3D scene
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // If dragging, only paint if we moved far enough from the last painted object
                if (e.type == EventType.MouseDown || Vector3.Distance(hit.point, lastPaintPos) >= brushSpacing)
                {
                    PaintPrefab(hit);
                }
                e.Use(); // Consume the event so we don't accidentally select other objects
            }
        }
        
        // Reset last paint position when releasing the mouse
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            lastPaintPos = Vector3.positiveInfinity;
        }
        
        // Stop painting when pressing Escape
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            StopPainting();
            Repaint(); // Update the editor window to show "Start Painting" again
        }
    }

    private void PaintPrefab(RaycastHit hit)
    {
        // Safely instantiate the prefab keeping its prefab link
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(brushPrefab);
        if (newObj == null) return;

        Vector3 finalPos = hit.point;

        if (snapToGrid)
        {
            finalPos.x = Mathf.Round(finalPos.x / gridSize) * gridSize;
            finalPos.y = Mathf.Round(finalPos.y / gridSize) * gridSize;
            finalPos.z = Mathf.Round(finalPos.z / gridSize) * gridSize;
        }

        newObj.transform.position = finalPos;
        
        if (alignToSurface)
        {
            newObj.transform.up = hit.normal;
        }

        // Register action so you can CTRL+Z to undo a stroke!
        Undo.RegisterCreatedObjectUndo(newObj, "Paint Prefab");
        
        // Save position to calculate spacing while dragging
        lastPaintPos = hit.point;
    }
}
#endif
