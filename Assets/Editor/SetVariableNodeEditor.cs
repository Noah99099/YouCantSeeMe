using UnityEditor;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(SetVariableNode))]
public class SetVariableNodeEditor : NodeEditor
{
    public override void OnBodyGUI()
    {
        serializedObject.Update();

        SetVariableNode node = target as SetVariableNode;
        DialogueGraph graph = node.graph as DialogueGraph;

        // 繪製輸入和輸出埠
        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("entry"), GUIContent.none);
        
        // 使用我們剛才建立的輔助方法來繪製下拉選單
        node.variableName = DialogueEditorHelper.DrawVariablePopup(graph, node.variableName);

        // 繪製剩餘的欄位
        EditorGUILayout.PropertyField(serializedObject.FindProperty("value"));
        
        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("exit"), GUIContent.none);

        serializedObject.ApplyModifiedProperties();
    }
}