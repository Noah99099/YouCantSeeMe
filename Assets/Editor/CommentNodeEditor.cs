using UnityEditor;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(CommentNode))]
public class CommentNodeEditor : NodeEditor
{
    // 我們覆寫 OnBodyGUI 來客製化節點在 Inspector 中的樣子
    public override void OnBodyGUI()
    {
        // 使用 SerializedObject 來安全地存取和修改屬性
        serializedObject.Update();
        
        // 只繪製 "text" 這個欄位，GUIContent.none 表示我們不需要在它前面加上標籤
        EditorGUILayout.PropertyField(serializedObject.FindProperty("text"), GUIContent.none);
        
        // 應用所有修改
        serializedObject.ApplyModifiedProperties();
    }

    // 我們覆寫 GetWidth 來讓節點有一個比預設更大的寬度
    public override int GetWidth()
    {
        // 您可以在這裡自由調整註解節點的預設寬度
        return 300;
    }
}