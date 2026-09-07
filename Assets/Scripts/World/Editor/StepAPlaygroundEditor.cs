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
        if (GUILayout.Button("Rebuild Playground", GUILayout.Height(32)))
        {
            Undo.RegisterFullObjectHierarchyUndo(playground.gameObject, "Rebuild Playground");
            playground.Rebuild();
            EditorUtility.SetDirty(playground);
        }

        EditorGUILayout.HelpBox(
            "选中这个物体后点上面的按钮，会重新生成平台和带动画的主角。然后按 Play，用 A/D 跑、空格跳。",
            MessageType.Info);
    }
}
