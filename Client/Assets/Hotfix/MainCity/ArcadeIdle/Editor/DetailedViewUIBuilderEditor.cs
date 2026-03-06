using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DetailedViewUIBuilder))]
public class DetailedViewUIBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Build UI", GUILayout.Height(40)))
        {
            var builder = (DetailedViewUIBuilder)target;
            Undo.RegisterFullObjectHierarchyUndo(builder.gameObject, "Build DetailedView UI");
            builder.BuildUI();
            EditorUtility.SetDirty(builder.gameObject);
        }
    }

    [MenuItem("Tools/Build Detailed View UI")]
    static void BuildFromMenu()
    {
        var builder = FindFirstObjectByType<DetailedViewUIBuilder>();
        if (builder == null)
        {
            Debug.LogError("DetailedViewUIBuilder not found in scene.");
            return;
        }
        Undo.RegisterFullObjectHierarchyUndo(builder.gameObject, "Build DetailedView UI");
        builder.BuildUI();
        EditorUtility.SetDirty(builder.gameObject);
        Debug.Log("DetailedView UI built successfully!");
    }
}
