using UnityEditor;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(ConditionalNode))]
public class ConditionalNodeEditor : NodeEditor
{
    public override void OnBodyGUI()
    {
        serializedObject.Update();

        ConditionalNode node = target as ConditionalNode;
        DialogueGraph graph = node.graph as DialogueGraph;

        // 繪製輸入埠
        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("entry"), GUIContent.none);

        // 同樣使用輔助方法來繪製下拉選單
        node.variableName = DialogueEditorHelper.DrawVariablePopup(graph, node.variableName);

        // 繪製剩餘的欄位
        EditorGUILayout.PropertyField(serializedObject.FindProperty("comparison"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("valueToCompare"));

        // 繪製兩個輸出埠
        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("exitTrue"));
        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("exitFalse"));

        serializedObject.ApplyModifiedProperties();
    }
}