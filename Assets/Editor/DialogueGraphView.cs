using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    private Vector2 _newNodePosition;
    
    public DialogueGraphView()
    {
        // 設定樣式，使其填滿整個編輯器視窗
        style.flexGrow = 1;

        // 設定縮放的最小和最大值
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        // 【核心】加入操縱器
        this.AddManipulator(new ContentDragger());    // 這個負責拖曳整個畫布 (平移)
        this.AddManipulator(new SelectionDragger());  // 這個負責拖曳選中的元素 (節點)
        this.AddManipulator(new RectangleSelector()); // 這個負責用滑鼠框選多個元素

        // 加入網格背景
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // 初始化節點創建的預設位置
        _newNodePosition = new Vector2(100, 200);
    }

    public void PopulateView(DialogueContainerSO dialogueContainer)
    {
        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;

        if (dialogueContainer == null) return;
        
        Debug.Log("--- 開始載入視圖 (PopulateView) ---");
        Debug.Log($"準備載入 {dialogueContainer.Blocks.Count} 個區塊 (Blocks) 和 {dialogueContainer.NodeLinks.Count} 條連線 (Links)。");

        // 1. 創建節點
        var createdNodes = new Dictionary<string, BlockNode>();
        foreach (var blockData in dialogueContainer.Blocks)
        {
            // 【監視點 1】印出剛從檔案讀取出來的原始資料
            Debug.Log($"讀取 Block 資料: 名稱='{blockData.BlockName}', GUID='{blockData.GUID}', 指令數量={blockData.Commands.Count}");

            var node = CreateBlockNode(blockData);

            // 【監視點 2】印出創建完成後，節點實際持有的 GUID
            Debug.Log($"創建 Node 物件: 名稱='{node.BlockData.BlockName}', 實際 GUID='{node.GUID}'");

            // 【監視點 3】檢查是否有重複的 GUID 被創建，這是最可能出錯的地方
            if (!string.IsNullOrEmpty(node.GUID) && createdNodes.ContainsKey(node.GUID))
            {
                Debug.LogError($"致命錯誤：偵測到重複的 GUID！GUID '{node.GUID}' 已經被節點 '{createdNodes[node.GUID].BlockData.BlockName}' 佔用。這是導致連線錯誤的直接原因。");
            }
            else
            {
                createdNodes[node.GUID] = node;
            }
        }

        Debug.Log("--- 所有節點創建完畢，開始處理連線 ---");

        // 2. 創建連線
        foreach (var linkData in dialogueContainer.NodeLinks)
        {
            Debug.Log($"嘗試連線: 來源GUID='{linkData.BaseNodeGuid}' -> 目標GUID='{linkData.TargetNodeGuid}'");
            if (createdNodes.TryGetValue(linkData.BaseNodeGuid, out var sourceNode) && 
                createdNodes.TryGetValue(linkData.TargetNodeGuid, out var targetNode))
            {
                Debug.Log($"<color=green>連線成功</color>: 找到來源='{sourceNode.BlockData.BlockName}' 和 目標='{targetNode.BlockData.BlockName}'。");
                var sourcePort = sourceNode.outputContainer.Q<Port>();
                var targetPort = targetNode.inputContainer.Q<Port>();
                var edge = sourcePort.ConnectTo(targetPort);
                AddElement(edge);
            }
            else
            {
                Debug.LogError($"<color=red>連線失敗</color>: 找不到對應的 GUID。來源是否存在: {createdNodes.ContainsKey(linkData.BaseNodeGuid)}, 目標是否存在: {createdNodes.ContainsKey(linkData.TargetNodeGuid)}");
            }
        }
        Debug.Log("--- 載入視圖結束 ---");
    }

    public void Save(DialogueContainerSO dialogueContainer)
    {
        if (dialogueContainer == null) return;

        // 清除舊資料
        dialogueContainer.Blocks.Clear();
        dialogueContainer.NodeLinks.Clear();

        var blockNodes = nodes.Cast<BlockNode>().ToList();
        
        // 1. 儲存節點
        foreach (var node in blockNodes)
        {
            node.BlockData.GUID = node.GUID; 
            node.BlockData.Position = node.GetPosition().position;
            dialogueContainer.Blocks.Add(node.BlockData);
        }
        
        // 2. 儲存連線
        foreach (var edge in edges)
        {
            var sourceNode = edge.output.node as BlockNode;
            var targetNode = edge.input.node as BlockNode;

            if (sourceNode == null || targetNode == null) continue;

            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = sourceNode.GUID,
                PortName = edge.output.portName,
                TargetNodeGuid = targetNode.GUID
            });
        }
        
        EditorUtility.SetDirty(dialogueContainer);
        AssetDatabase.SaveAssets();
    }
    
    private BlockNode CreateBlockNode(DialogueBlock block)
    {
        // 【關鍵修正】在載入時，檢查 GUID 是否因序列化問題而遺失。
        // 如果遺失了 (變成空值或 null)，就立刻為它重新生成一個，確保它永遠有有效的ID。
        if (string.IsNullOrEmpty(block.GUID))
        {
            Debug.LogWarning($"偵測到一個 Block (名稱: {block.BlockName}) 缺少 GUID，已為其自動生成一個新ID。");
            block.GUID = Guid.NewGuid().ToString();
        }

        // 後續邏輯不變，現在傳入的 block 一定有 GUID
        var node = new BlockNode(block);
        node.SetPosition(new Rect(block.Position, new Vector2(250, 200)));
        AddElement(node);

        foreach (var command in block.Commands)
        {
            if (command is SayCommand sayCommand)
            {
                node.AddSayCommandUI(sayCommand);
            }
        }
        return node;
    }
    // 【修改】右鍵創建新節點的邏輯
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var graphMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        evt.menu.AppendAction("Create Block", (action) => 
        {
            // 【修正】創建新 Block 時，立即為其分配一個新的、隨機的 GUID
            CreateBlockNode(new DialogueBlock 
            { 
                BlockName = "New Block",
                GUID = Guid.NewGuid().ToString(), // 確保新節點有唯一的ID
                Position = graphMousePosition
            });
        });
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node).ToList();
    }
    
    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        return graphViewChange;
    }
}