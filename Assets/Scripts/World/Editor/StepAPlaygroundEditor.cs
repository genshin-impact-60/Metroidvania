using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StepAPlayground))]
public class StepAPlaygroundEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        var playground = (StepAPlayground)target;
        if (GUILayout.Button("Create / Refresh Player Prefab", GUILayout.Height(24)))
            PlayerPrefabBuilder.CreateOrUpdateMenu();

        if (GUILayout.Button("Rebuild Playground", GUILayout.Height(32)))
        {
            PlayerPrefabBuilder.EnsurePrefabAndWireScenes();
            EnvKitBuilder.EnsureKitsAndConvert();
            Undo.RegisterFullObjectHierarchyUndo(playground.gameObject, "Rebuild Playground");
            PlaygroundSeeder.Rebuild(playground);
            EditorUtility.SetDirty(playground);
        }

        EditorGUILayout.HelpBox(
            "这是 SampleScene 编辑器沙盒，不是正式关卡。正式切片在 Cathedral_Hub。\n"
            + "Rebuild 只在编辑器里种 Prefab；Play 不会重建。",
            MessageType.Info);
    }
}
