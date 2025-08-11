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
        
        var createdNodes = new Dictionary<string, BlockNode>();
        
        // 1. 創建節點
        foreach (var blockData in dialogueContainer.Blocks)
        {
            var node = CreateBlockNode(blockData);
            createdNodes[blockData.GUID] = node;
        }

        // 2. 將列表中的第一個節點設為入口點樣式
        if (dialogueContainer.Blocks.Count > 0 && createdNodes.ContainsKey(dialogueContainer.Blocks[0].GUID))
        {
            createdNodes[dialogueContainer.Blocks[0].GUID].SetEntryPointStyle(true);
        }

        // 3. 創建連線
        foreach (var blockData in dialogueContainer.Blocks)
        {
            if (!string.IsNullOrEmpty(blockData.NextBlockGuid))
            {
                if (createdNodes.TryGetValue(blockData.GUID, out var sourceNode) && 
                    createdNodes.TryGetValue(blockData.NextBlockGuid, out var targetNode))
                {
                    var sourcePort = sourceNode.outputContainer.Q<Port>();
                    var targetPort = targetNode.inputContainer.Q<Port>();
                    var edge = sourcePort.ConnectTo(targetPort);
                    AddElement(edge);
                }
            }
        }
    }

    public void Save(DialogueContainerSO dialogueContainer)
    {
        if (dialogueContainer == null) return;

        var blockNodes = nodes.Cast<BlockNode>().ToList();
        
        // 清除舊的連線資料
        foreach (var node in blockNodes)
        {
            node.BlockData.NextBlockGuid = null;
        }

        // 根據畫面上的連線，重新寫入連線資料
        foreach (var edge in edges)
        {
            var sourceNode = edge.output.node as BlockNode;
            var targetNode = edge.input.node as BlockNode;
            if (sourceNode != null && targetNode != null)
            {
                sourceNode.BlockData.NextBlockGuid = targetNode.GUID;
            }
        }
        
        // 將節點資料存入 Container 中
        dialogueContainer.Blocks.Clear();
        foreach (var node in blockNodes)
        {
            node.BlockData.Position = node.GetPosition().position;
            dialogueContainer.Blocks.Add(node.BlockData);
        }
        
        EditorUtility.SetDirty(dialogueContainer);
    }
    
    private BlockNode CreateBlockNode(DialogueBlock block)
    {
        if (string.IsNullOrEmpty(block.GUID))
        {
            block.GUID = Guid.NewGuid().ToString();
        }
        var node = new BlockNode(block);
        node.SetPosition(new Rect(block.Position, new Vector2(250, 200)));
        AddElement(node);
        foreach (var command in block.Commands)
        {
            if (command is SayCommand sayCommand) node.AddSayCommandUI(sayCommand);
        }
        return node;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var graphMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        evt.menu.AppendAction("創建區塊 (Create Block)", (action) => 
        {
            CreateBlockNode(new DialogueBlock 
            { 
                BlockName = "New Block",
                GUID = Guid.NewGuid().ToString(),
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
        // 當有元素被刪除時 (這部分邏輯不變)
        if (graphViewChange.elementsToRemove != null)
        {
            // 目前我們依靠儲存時重建所有資料，所以此處暫時不需要額外邏輯
        }

        // 當有連線被建立時
        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                // 取得連線的來源節點
                var sourceNode = edge.output.node as BlockNode;
                if (sourceNode == null) continue;

                // 【修正】從節點的 outputContainer 中，明確地查詢 Port 類型的物件
                var outputPort = sourceNode.outputContainer.Q<Port>();
                if (outputPort == null) continue;
                
                // 【修正】現在我們在正確的 Port 物件上，檢查其連線數量
                if (outputPort.connections.Count() > 1)
                {
                    // 找到舊的連線 (即不是我們剛剛建立的這一條)
                    var oldEdge = outputPort.connections.First(x => x != edge);
                    
                    // 將舊的連線從圖表中移除
                    RemoveElement(oldEdge);
                }
            }
        }

        return graphViewChange;
    }
}