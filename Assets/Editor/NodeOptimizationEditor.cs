using UnityEngine;
using UnityEditor;
using XNodeEditor;
using XNode;
using System.Collections.Generic;

// ---------------------------------------------------------------------
// 針對 "PriorityRouterNode" 的優化編輯器 (含清理功能)
// ---------------------------------------------------------------------
[CustomNodeEditor(typeof(PriorityRouterNode))]
public class PriorityRouterNodeEditor : NodeEditor
{
    private bool showDetails = true;

    public override void OnBodyGUI()
    {
        base.OnHeaderGUI();

        // --- 工具列 ---
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(showDetails ? "▼ 隱藏細節" : "▶ 顯示細節", EditorStyles.toolbarButton))
        {
            showDetails = !showDetails;
        }
        
        // 【新增】清理按鈕
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // 淡紅色按鈕
        if (GUILayout.Button("🧹 清理無效條件", EditorStyles.toolbarButton))
        {
            CleanUpRouterNode();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // --- 內容繪製 ---
        if (showDetails)
        {
            base.OnBodyGUI();
        }
        else
        {
            // 隱藏模式：只繪製端口
            PriorityRouterNode node = target as PriorityRouterNode;
            NodeEditorGUILayout.PortField(node.GetInputPort("entry"));
            NodeEditorGUILayout.PortField(node.GetOutputPort("exitElse"));

            if (node.conditions != null)
            {
                GUILayout.Label($"[{node.conditions.Count} 個條件]", EditorStyles.miniLabel);
                for (int i = 0; i < node.conditions.Count; i++)
                {
                    NodePort port = node.GetOutputPort("conditions " + i);
                    if (port != null) NodeEditorGUILayout.PortField(new GUIContent($"條件 {i}"), port);
                }
            }
        }
    }

    /// <summary>
    /// 自動移除沒有連線的條件
    /// </summary>
    private void CleanUpRouterNode()
    {
        PriorityRouterNode node = target as PriorityRouterNode;
        if (node.conditions == null) return;

        // 倒序遍歷，因為我們要移除元素
        bool changed = false;
        for (int i = node.conditions.Count - 1; i >= 0; i--)
        {
            NodePort port = node.GetOutputPort("conditions " + i);
            // 如果端口不存在，或者沒有連接到任何東西
            if (port == null || !port.IsConnected)
            {
                node.conditions.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
        {
            Debug.Log($"[PriorityRouterNode] 已清理無效的連接。");
            // 標記為已修改，確保 Unity 會存檔
            EditorUtility.SetDirty(target); 
        }
    }
}

// ---------------------------------------------------------------------
// 針對 "ConditionalChoiceNode" 的優化編輯器 (含清理功能)
// ---------------------------------------------------------------------
[CustomNodeEditor(typeof(ConditionalChoiceNode))]
public class ConditionalChoiceNodeEditor : NodeEditor
{
    private bool showDetails = true;

    public override void OnBodyGUI()
    {
        base.OnHeaderGUI();

        // --- 工具列 ---
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(showDetails ? "▼ 隱藏細節" : "▶ 顯示細節", EditorStyles.toolbarButton))
        {
            showDetails = !showDetails;
        }

        // 【新增】清理按鈕
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("🧹 清理無效選項", EditorStyles.toolbarButton))
        {
            CleanUpChoiceNode();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        if (showDetails)
        {
            base.OnBodyGUI();
        }
        else
        {
            ConditionalChoiceNode node = target as ConditionalChoiceNode;
            NodeEditorGUILayout.PortField(node.GetInputPort("entry"));

            if (node.conditionalChoices != null)
            {
                GUILayout.Label($"[{node.conditionalChoices.Count} 個動態選項]", EditorStyles.miniLabel);
                for (int i = 0; i < node.conditionalChoices.Count; i++)
                {
                    NodePort port = node.GetOutputPort("conditionalChoices " + i);
                    if (port != null) NodeEditorGUILayout.PortField(new GUIContent($"選項 {i}"), port);
                }
            }

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

    private void CleanUpChoiceNode()
    {
        ConditionalChoiceNode node = target as ConditionalChoiceNode;
        bool changed = false;

        // 1. 清理動態選項
        if (node.conditionalChoices != null)
        {
            for (int i = node.conditionalChoices.Count - 1; i >= 0; i--)
            {
                NodePort port = node.GetOutputPort("conditionalChoices " + i);
                if (port == null || !port.IsConnected)
                {
                    node.conditionalChoices.RemoveAt(i);
                    changed = true;
                }
            }
        }

        // 2. 清理預設選項
        if (node.defaultChoices != null)
        {
            for (int i = node.defaultChoices.Count - 1; i >= 0; i--)
            {
                NodePort port = node.GetOutputPort("defaultChoices " + i);
                if (port == null || !port.IsConnected)
                {
                    node.defaultChoices.RemoveAt(i);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            Debug.Log($"[ConditionalChoiceNode] 已清理無效的選項。");
            EditorUtility.SetDirty(target);
        }
    }
}