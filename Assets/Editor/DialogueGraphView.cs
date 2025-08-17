using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    public DialogueGraphView()
    {
        style.flexGrow = 1;
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
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

        // 2. 設定入口點樣式
        if (dialogueContainer.Blocks.Count > 0 && createdNodes.ContainsKey(dialogueContainer.Blocks[0].GUID))
        {
            createdNodes[dialogueContainer.Blocks[0].GUID].SetEntryPointStyle(true);
        }

        // 3. 創建連線
        foreach (var blockData in dialogueContainer.Blocks)
        {
            if (!createdNodes.ContainsKey(blockData.GUID)) continue;
            var sourceNode = createdNodes[blockData.GUID];

            // 3a. 處理預設的 "Next" 連線
            if (!string.IsNullOrEmpty(blockData.NextBlockGuid) && createdNodes.ContainsKey(blockData.NextBlockGuid))
            {
                var targetNode = createdNodes[blockData.NextBlockGuid];
                var sourcePort = sourceNode.outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.userData == null);
                var targetPort = targetNode.inputContainer.Q<Port>();
                
                if (sourcePort != null && targetPort != null)
                {
                    var edge = sourcePort.ConnectTo(targetPort);
                    AddElement(edge);
                }
            }
            
            // 3b. 處理選項指令的連線
            var choiceCommand = blockData.Commands.OfType<ChoiceCommand>().FirstOrDefault();
            if (choiceCommand != null)
            {
                foreach (var choice in choiceCommand.Choices)
                {
                    if (!string.IsNullOrEmpty(choice.TargetBlockGuid) && createdNodes.ContainsKey(choice.TargetBlockGuid))
                    {
                        var targetNode = createdNodes[choice.TargetBlockGuid];
                        var sourcePort = sourceNode.outputContainer.Query<Port>().ToList()
                                         .FirstOrDefault(p => p.userData == choice);
                        var targetPort = targetNode.inputContainer.Q<Port>();
                                         
                        if (sourcePort != null && targetPort != null)
                        {
                            var edge = sourcePort.ConnectTo(targetPort);
                            AddElement(edge);
                        }
                    }
                }
            }
        }
    }

    public void Save(DialogueContainerSO dialogueContainer)
    {
        if (dialogueContainer == null) return;

        var blockNodes = nodes.Cast<BlockNode>().ToList();
        
        foreach (var node in blockNodes)
        {
            node.BlockData.NextBlockGuid = null;
            var choiceCmd = node.BlockData.Commands.OfType<ChoiceCommand>().FirstOrDefault();
            if (choiceCmd != null)
            {
                foreach (var choice in choiceCmd.Choices)
                {
                    choice.TargetBlockGuid = null;
                }
            }
        }

        foreach (var edge in edges)
        {
            var sourceNode = edge.output.node as BlockNode;
            var targetNode = edge.input.node as BlockNode;

            if (sourceNode != null && targetNode != null)
            {
                if (edge.output.userData == null)
                {
                    sourceNode.BlockData.NextBlockGuid = targetNode.GUID;
                }
                else if (edge.output.userData is ChoiceCommand.Choice choiceData)
                {
                    choiceData.TargetBlockGuid = targetNode.GUID;
                }
            }
        }
        
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
            if (command is SayCommand sayCommand) 
                node.AddSayCommandUI(sayCommand);
            else if (command is ChoiceCommand choiceCommand)
                node.AddChoiceCommandUI(choiceCommand);
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
        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                var sourceNode = edge.output.node as BlockNode;
                if (sourceNode == null) continue;

                if (edge.output.userData == null)
                {
                    var outputPort = sourceNode.outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.userData == null);

                    // 確保我們真的找到了 port 才繼續
                    if (outputPort != null && outputPort.connections.Count() > 1)
                    {
                        var oldEdge = outputPort.connections.First(x => x != edge);
                        RemoveElement(oldEdge);
                    }
                }
            }
        }

        return graphViewChange;
    }
}