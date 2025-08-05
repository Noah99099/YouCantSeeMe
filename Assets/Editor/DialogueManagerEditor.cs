using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueManager))]
public class DialogueManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 先繪製預設的欄位，例如 "全域資源參考" 和 "全域設定"
        DrawDefaultInspector();

        // 獲取我們要操作的 DialogueManager 物件
        var manager = (DialogueManager)target;
        // 找到 "managedDialogues" 這個列表屬性
        var list = serializedObject.FindProperty("managedDialogues");
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("統一對話管理", EditorStyles.boldLabel);

        // 迭代繪製列表中的每一個元素
        for (int i = 0; i < list.arraySize; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            
            // 開始一個帶有邊框的垂直佈局，讓每個元素更清晰
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // 【*** 以下是固定顯示的欄位 ***】
            EditorGUILayout.PropertyField(element.FindPropertyRelative("Name"), new GUIContent("規則名稱"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("DialogueContainer"), new GUIContent("對話檔案"));
            
            var triggerTypeProp = element.FindPropertyRelative("TriggerType");
            EditorGUILayout.PropertyField(triggerTypeProp, new GUIContent("觸發類型"));

            // 【*** 以下是根據觸發類型動態顯示的欄位 ***】
            var triggerType = (DialogueTriggerType)triggerTypeProp.enumValueIndex;
            switch (triggerType)
            {
                // 當選擇 OnInteraction 時，只顯示 InteractionTarget
                case DialogueTriggerType.OnInteraction:
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("InteractionTarget"), new GUIContent("互動目標"));
                    break;
                // 當選擇 OnZoneEnter 時，只顯示 ZoneTarget
                case DialogueTriggerType.OnZoneEnter:
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("ZoneTarget"), new GUIContent("觸發區域"));
                    break;
                // 當選擇 OnEvent 時，只顯示 EventToListenFor
                case DialogueTriggerType.OnEvent:
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("EventToListenFor"), new GUIContent("監聽事件"));
                    break;
                // OnSceneStart 不需要額外欄位，所以 case 為空
                case DialogueTriggerType.OnSceneStart:
                default:
                    break;
            }

            // TriggerOnlyOnce 是所有類型都需要的欄位
            EditorGUILayout.PropertyField(element.FindPropertyRelative("TriggerOnlyOnce"), new GUIContent("只觸發一次"));
            
            // 繪製一個刪除按鈕
            if (GUILayout.Button("刪除此規則", GUILayout.Height(20)))
            {
                list.DeleteArrayElementAtIndex(i);
                break; 
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // 繪製一個新增按鈕
        if (GUILayout.Button("新增對話規則", GUILayout.Height(30)))
        {
            list.InsertArrayElementAtIndex(list.arraySize);
        }
        
        // 套用所有屬性修改
        serializedObject.ApplyModifiedProperties();
    }
}