using UnityEngine;
using UnityEditor;
using XNodeEditor; // 必須引用 XNodeEditor
using XNode;

// ---------------------------------------------------------------------
// 這是針對 "PriorityRouterNode" 的優化編輯器
// ---------------------------------------------------------------------
[CustomNodeEditor(typeof(PriorityRouterNode))]
public class PriorityRouterNodeEditor : NodeEditor
{
    private bool showDetails = true; // 預設展開

    public override void OnBodyGUI()
    {
        // 1. 繪製標準的標題
        base.OnHeaderGUI();

        // 2. 繪製一個 "摺疊/展開" 的按鈕
        // 使用 Unity 的 Style 讓它看起來像個標籤
        if (GUILayout.Button(showDetails ? "▼ 隱藏詳細設定 (提升效能)" : "▶ 顯示詳細設定", EditorStyles.toolbarButton))
        {
            showDetails = !showDetails;
        }

        // 3. 根據狀態決定要繪製什麼
        if (showDetails)
        {
            // --- 展開模式：繪製原本的所有內容 ---
            base.OnBodyGUI();
        }
        else
        {
            // --- 隱藏模式：只繪製必要的端口 (Ports) ---
            // 這樣連線才不會消失，但不會繪製那個超耗效能的 List
            
            // 取得目標節點
            PriorityRouterNode node = target as PriorityRouterNode;

            // 繪製 "entry" 入口
            NodeEditorGUILayout.PortField(node.GetInputPort("entry"));

            // 繪製 "exitElse" 出口
            NodeEditorGUILayout.PortField(node.GetOutputPort("exitElse"));

            // 手動繪製動態列表的出口 (只畫點，不畫內容)
            // 這能讓您在隱藏時依然看得到連線
            if (node.conditions != null)
            {
                GUILayout.Label($"已設定 {node.conditions.Count} 個條件...", EditorStyles.miniLabel);
                
                for (int i = 0; i < node.conditions.Count; i++)
                {
                    // 獲取動態生成的端口名稱
                    string portName = "conditions " + i;
                    NodePort port = node.GetOutputPort(portName);
                    
                    if (port != null)
                    {
                        // 簡單繪製一個端口，不繪製內容
                        NodeEditorGUILayout.PortField(new GUIContent($"條件 {i}"), port);
                    }
                }
            }
        }
    }
}

// ---------------------------------------------------------------------
// 這是針對 "ConditionalChoiceNode" 的優化編輯器
// (邏輯同上)
// ---------------------------------------------------------------------
[CustomNodeEditor(typeof(ConditionalChoiceNode))]
public class ConditionalChoiceNodeEditor : NodeEditor
{
    private bool showDetails = true;

    public override void OnBodyGUI()
    {
        base.OnHeaderGUI();

        if (GUILayout.Button(showDetails ? "▼ 隱藏詳細設定" : "▶ 顯示詳細設定", EditorStyles.toolbarButton))
        {
            showDetails = !showDetails;
        }

        if (showDetails)
        {
            base.OnBodyGUI();
        }
        else
        {
            ConditionalChoiceNode node = target as ConditionalChoiceNode;

            // 繪製入口
            NodeEditorGUILayout.PortField(node.GetInputPort("entry"));

            // 繪製動態出口 (Conditional Choices)
            if (node.conditionalChoices != null)
            {
                GUILayout.Label($"[{node.conditionalChoices.Count} 個動態選項]", EditorStyles.miniLabel);
                for (int i = 0; i < node.conditionalChoices.Count; i++)
                {
                    NodePort port = node.GetOutputPort("conditionalChoices " + i);
                    if (port != null) NodeEditorGUILayout.PortField(new GUIContent($"選項 {i}"), port);
                }
            }

            // 繪製預設出口 (Default Choices)
            if (node.defaultChoices != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"[{node.defaultChoices.Count} 個預設選項]", EditorStyles.miniLabel);
                for (int i = 0; i < node.defaultChoices.Count; i++)
                {
                    NodePort port = node.GetOutputPort("defaultChoices " + i);
                    if (port != null) NodeEditorGUILayout.PortField(new GUIContent($"預設 {i}"), port);
                }
            }
        }
    }
}