using UnityEngine;
using UnityEditor;
using XNodeEditor;
using XNode;
using System;
using System.Collections.Generic;

[CustomNodeGraphEditor(typeof(DialogueGraph))]
public class DialogueGraphEditor : NodeGraphEditor 
{
    // 1. 保留選單美化邏輯
    public override string GetNodeMenuName(System.Type type) 
    {
        if (typeof(BaseNode).IsAssignableFrom(type) || type == typeof(CommentNode))
        {
            return base.GetNodeMenuName(type).Replace("Node", "");
        }
        return null;
    }

    // 2. 繪製全域清理按鈕
    public override void OnGUI()
    {
        base.OnGUI();

        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        
        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // 紅色按鈕
        if (GUILayout.Button("🧹 全域清理：移除幽靈連結", GUILayout.Height(30)))
        {
            CleanUpAllNodes();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndArea();
    }

    // 3. 加強版清理邏輯
    private void CleanUpAllNodes()
    {
        DialogueGraph graph = target as DialogueGraph;
        if (graph == null) return;

        int cleanCount = 0;
        Debug.Log($"[DialogueGraph] 正在深度清理 '{graph.name}'...");

        foreach (Node node in graph.nodes)
        {
            bool nodeChanged = false;

            // A. 清理 PriorityRouterNode
            if (node is PriorityRouterNode routerNode)
            {
                // 強制 XNode 刷新端口狀態，確保資料同步
                routerNode.UpdatePorts();

                if (routerNode.conditions != null)
                {
                    for (int i = routerNode.conditions.Count - 1; i >= 0; i--)
                    {
                        if (IsPortInvalid(routerNode, "conditions " + i))
                        {
                            routerNode.conditions.RemoveAt(i);
                            nodeChanged = true;
                            cleanCount++;
                        }
                    }
                }
            }
            // B. 清理 ConditionalChoiceNode
            else if (node is ConditionalChoiceNode choiceNode)
            {
                choiceNode.UpdatePorts();

                // 清理動態選項
                if (choiceNode.conditionalChoices != null)
                {
                    for (int i = choiceNode.conditionalChoices.Count - 1; i >= 0; i--)
                    {
                        if (IsPortInvalid(choiceNode, "conditionalChoices " + i))
                        {
                            choiceNode.conditionalChoices.RemoveAt(i);
                            nodeChanged = true;
                            cleanCount++;
                        }
                    }
                }
                // 清理預設選項
                if (choiceNode.defaultChoices != null)
                {
                    for (int i = choiceNode.defaultChoices.Count - 1; i >= 0; i--)
                    {
                        if (IsPortInvalid(choiceNode, "defaultChoices " + i))
                        {
                            choiceNode.defaultChoices.RemoveAt(i);
                            nodeChanged = true;
                            cleanCount++;
                        }
                    }
                }
            }
            // C. 清理 ChoiceNode (舊版)
            else if (node is ChoiceNode oldChoiceNode)
            {
                oldChoiceNode.UpdatePorts();
                if (oldChoiceNode.choices != null)
                {
                    for (int i = oldChoiceNode.choices.Count - 1; i >= 0; i--)
                    {
                        if (IsPortInvalid(oldChoiceNode, "choices " + i))
                        {
                            oldChoiceNode.choices.RemoveAt(i);
                            // 同步移除 Key (如果有)
                            if (oldChoiceNode.choiceKeys != null && i < oldChoiceNode.choiceKeys.Count)
                                oldChoiceNode.choiceKeys.RemoveAt(i);
                            
                            nodeChanged = true;
                            cleanCount++;
                        }
                    }
                }
            }

            if (nodeChanged)
            {
                EditorUtility.SetDirty(node); // 標記修改
            }
        }

        if (cleanCount > 0)
        {
            Debug.Log($"[DialogueGraph] 清理完成！共移除了 {cleanCount} 個無效/幽靈連結。");
            // 雙重保險：強制存檔
            AssetDatabase.SaveAssets(); 
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.Log($"[DialogueGraph] 圖形很乾淨，沒有發現問題。");
        }
    }

    /// <summary>
    /// 嚴格檢查端口是否有效
    /// </summary>
    private bool IsPortInvalid(Node node, string portName)
    {
        NodePort port = node.GetOutputPort(portName);

        // 1. 端口根本不存在 -> 無效
        if (port == null) return true;

        // 2. 沒連接任何東西 -> 無效
        if (!port.IsConnected) return true;

        // 3. 【關鍵修正】連接了東西，但那個東西是 Null (Missing Node) -> 無效
        //    這是抓出 "幽靈連結" 的關鍵
        if (port.Connection == null || port.Connection.node == null) return true;

        // 通過所有檢查 -> 有效
        return false;
    }
}