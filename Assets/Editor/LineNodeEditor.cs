using UnityEditor;
using UnityEngine;
using XNodeEditor;
using System.Linq;
using System; // 需要引用 System 來使用 ArrayUtility

[CustomNodeEditor(typeof(LineNode))]
public class LineNodeEditor : NodeEditor
{
    private LineNode lineNode;

    public override void OnBodyGUI()
    {
        serializedObject.Update();

        // 繪製輸入輸出埠
        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("entry"), GUIContent.none);

        // --- 繪製角色和表情的下拉選單 ---
        lineNode = target as LineNode;

        var allProfiles = GetAllCharacterProfilesInProject();
        if (allProfiles != null && allProfiles.Length > 0)
        {
            // 角色ID下拉選單
            string[] characterIDs = allProfiles.Select(p => p.characterID).ToArray();
            int charIndex = ArrayUtility.IndexOf(characterIDs, lineNode.line.characterID);
            if (charIndex < 0) charIndex = 0; // 預設
            int newCharIndex = EditorGUILayout.Popup("Character ID", charIndex, characterIDs);
            lineNode.line.characterID = characterIDs[newCharIndex];

            // 表情下拉選單
            var currentProfile = allProfiles.FirstOrDefault(p => p.characterID == lineNode.line.characterID);
            if (currentProfile != null && currentProfile.expressions.Count > 0)
            {
                string[] expressionKeywords = currentProfile.expressions.Select(e => e.keyword).ToArray();
                int exprIndex = ArrayUtility.IndexOf(expressionKeywords, lineNode.line.expression);
                if (exprIndex < 0) exprIndex = 0;
                int newExprIndex = EditorGUILayout.Popup("Expression", exprIndex, expressionKeywords);
                lineNode.line.expression = expressionKeywords[newExprIndex];
            }
            else
            {
                lineNode.line.expression = EditorGUILayout.TextField("Expression", lineNode.line.expression);
            }
        }
        else
        {
            // 如果找不到任何 Profile，退回文字輸入
            lineNode.line.characterID = EditorGUILayout.TextField("Character ID", lineNode.line.characterID);
            lineNode.line.expression = EditorGUILayout.TextField("Expression", lineNode.line.expression);
        }

        // --- 繪製 DialogueLine 中的剩餘欄位 ---
        EditorGUILayout.Space(); // 增加一點間距
        // 我們直接繪製 line 這個結構，讓 Unity 自動處理剩下的欄位
        EditorGUILayout.PropertyField(serializedObject.FindProperty("line"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isImportant"));

        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("exit"), GUIContent.none);

        serializedObject.ApplyModifiedProperties();
    }

    public override int GetWidth()
    {
        return 300; // 可以稍微調回窄一點的寬度
    }

    private CharacterProfile[] GetAllCharacterProfilesInProject()
    {
        string[] guids = AssetDatabase.FindAssets("t:CharacterProfile");
        CharacterProfile[] profiles = new CharacterProfile[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            profiles[i] = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
        }
        return profiles;
    }
}