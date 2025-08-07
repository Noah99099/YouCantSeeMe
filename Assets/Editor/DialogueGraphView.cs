// 檔案：DialogueGraphView.cs (最終修正版)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    private DialogueContainerSO _currentDialogueContainer;
    private readonly float _nodeWidth = 280f;

    public DialogueGraphView()
    {
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
        
        AddDialogueNodeStyles();
    }

    private void AddDialogueNodeStyles()
    {
        var styleSheet = Resources.Load<StyleSheet>("DialogueNodeStyles");
        if (styleSheet != null)
        {
            styleSheets.Add(styleSheet);
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    // 【*** 關鍵修改點 ***】
    // 這個方法負責建立右鍵選單
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var graphViewMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        
        // 在空白處右鍵，只提供「新增節點」功能
        evt.menu.AppendAction("新增對話節點", action => CreateNode("新的對話", nodes.ToList().Count == 0, graphViewMousePosition));
        
        // 如果右鍵點擊的目標是一個 DialogueNode...
        if (evt.target is DialogueNode clickedNode)
        {
            evt.menu.AppendSeparator();
            
            // 只有在非結束點時，才顯示「添加選項」
            if (!clickedNode.EndPoint)
            {
                evt.menu.AppendAction("添加選項", action => AddChoicePort(clickedNode, $"選項 {clickedNode.GetChoicePortCount() + 1}"));
            }

            // 只有在非結束點時，才顯示「設為入口點」
            if (!clickedNode.EndPoint)
            {
                evt.menu.AppendAction("設為入口點", action => SetAsEntryPoint(clickedNode));
            }
            
            // 新增「設為結束點」的選項，點擊後會呼叫 SetAsEndPoint 方法
            evt.menu.AppendAction("設為結束點", action => SetAsEndPoint(clickedNode));
            
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("複製節點", action => DuplicateNode(clickedNode, graphViewMousePosition));
        }
    }

    public void CreateNode(string nodeName, bool isEntryPoint, Vector2 position)
    {
        var dialogueNode = new DialogueNode
        {
            title = nodeName,
            DialogueText = "新的對話內容",
            SpeakerName = nodeName,
            GUID = Guid.NewGuid().ToString(),
            EntryPoint = isEntryPoint,
            Position = position,
            NameColor = Color.white,
            IsImportant = false
        };
        
        dialogueNode.Setup(this, isEntryPoint);
        dialogueNode.SetPosition(new Rect(position, new Vector2(_nodeWidth, 200)));
        
        AddElement(dialogueNode);
    }
    
    public void AddChoicePort(DialogueNode node, string portName)
    {
        node.AddChoicePort(this, portName);
    }

    private void SetAsEntryPoint(DialogueNode targetNode)
    {
        // 確保目標節點不是結束點
        if(targetNode.EndPoint)
        {
            targetNode.SetAsEndPoint(false);
        }
        
        // 移除其他節點的入口點狀態
        foreach (var node in nodes.ToList().Cast<DialogueNode>())
        {
            if (node == targetNode)
            {
                node.EntryPoint = true;
            }
            else
            {
                node.EntryPoint = false;
            }
            node.SetupNodeStyle(); // 統一呼叫樣式更新
        }
    }

    private void SetAsEndPoint(DialogueNode targetNode)
    {
        // 切換目標節點的結束點狀態
        targetNode.SetAsEndPoint(!targetNode.EndPoint);
    }

    private void DuplicateNode(DialogueNode originalNode, Vector2 position)
    {
        var newNode = new DialogueNode
        {
            title = originalNode.title + " (複製)",
            DialogueText = originalNode.DialogueText,
            SpeakerName = originalNode.SpeakerName + " (複製)",
            GUID = Guid.NewGuid().ToString(),
            EntryPoint = false,
            EndPoint = originalNode.EndPoint,
            Position = position + new Vector2(50, 50),
            NameColor = originalNode.NameColor,
            IsImportant = originalNode.IsImportant
        };
        
        newNode.Setup(this, false);
        newNode.SetPosition(new Rect(position + new Vector2(50, 50), new Vector2(_nodeWidth, 200)));
        
        if (!newNode.EndPoint)
        {
            var choiceNames = originalNode.GetChoicePortNames();
            foreach (var choiceName in choiceNames)
            {
                AddChoicePort(newNode, choiceName);
            }
        }
        
        AddElement(newNode);
    }

    public void Save(DialogueContainerSO dialogueContainer)
    {
        dialogueContainer.NodeLinks.Clear();
        dialogueContainer.DialogueNodes.Clear();

        var edges = new List<Edge>(this.edges.ToList());
        foreach (var edge in edges)
        {
            var outputPort = edge.output;
            var inputPort = edge.input;

            if (outputPort?.node is DialogueNode baseNode && inputPort?.node is DialogueNode targetNode)
            {
                dialogueContainer.NodeLinks.Add(new NodeLinkData
                {
                    BaseNodeGuid = baseNode.GUID,
                    TargetNodeGuid = targetNode.GUID,
                    PortName = outputPort.portName
                });
            }
        }
        
        foreach (var node in nodes.ToList().Cast<DialogueNode>())
        {
            dialogueContainer.DialogueNodes.Add(new DialogueNodeData
            {
                Guid = node.GUID,
                DialogueText = node.DialogueText,
                SpeakerName = node.SpeakerName,
                EntryPoint = node.EntryPoint,
                EndPoint = node.EndPoint,
                Position = node.GetPosition().position,
                NameColor = node.NameColor,
                IsImportant = node.IsImportant 
            });
        }
        
        Debug.Log($"已儲存 {dialogueContainer.DialogueNodes.Count} 個節點和 {dialogueContainer.NodeLinks.Count} 個連線");
    }

    public void LoadGraph(DialogueContainerSO dialogueContainer)
    {
        DeleteElements(nodes.ToList());
        DeleteElements(edges.ToList());
        _currentDialogueContainer = dialogueContainer;

        if (dialogueContainer.DialogueNodes == null || dialogueContainer.DialogueNodes.Count == 0) 
        {
            Debug.Log("對話容器為空，創建空白圖表");
            return;
        }

        foreach (var nodeData in dialogueContainer.DialogueNodes)
        {
            var dialogueNode = new DialogueNode
            {
                GUID = nodeData.Guid,
                DialogueText = nodeData.DialogueText,
                SpeakerName = nodeData.SpeakerName,
                EntryPoint = nodeData.EntryPoint,
                EndPoint = nodeData.EndPoint,
                NameColor = nodeData.NameColor,
                Position = nodeData.Position,
                IsImportant = nodeData.IsImportant 
            };
            
            dialogueNode.Setup(this, dialogueNode.EntryPoint);
            dialogueNode.SetPosition(new Rect(dialogueNode.Position, new Vector2(_nodeWidth, 200)));
            AddElement(dialogueNode);
        }

        var nodeChoices = new Dictionary<string, List<string>>();
        foreach (var linkData in dialogueContainer.NodeLinks)
        {
            if (linkData.PortName != "繼續")
            {
                if (!nodeChoices.ContainsKey(linkData.BaseNodeGuid))
                {
                    nodeChoices[linkData.BaseNodeGuid] = new List<string>();
                }
                if (!nodeChoices[linkData.BaseNodeGuid].Contains(linkData.PortName))
                {
                    nodeChoices[linkData.BaseNodeGuid].Add(linkData.PortName);
                }
            }
        }

        foreach (var kvp in nodeChoices)
        {
            var node = nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == kvp.Key);
            if (node != null && !node.EndPoint)
            {
                node.LoadChoicePorts(kvp.Value, this);
            }
        }

        foreach (var linkData in dialogueContainer.NodeLinks)
        {
            var baseNode = nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == linkData.BaseNodeGuid);
            var targetNode = nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == linkData.TargetNodeGuid);

            if (baseNode == null || targetNode == null)
            {
                Debug.LogWarning($"無法建立連線: 找不到節點 {linkData.BaseNodeGuid} 或 {linkData.TargetNodeGuid}");
                continue;
            }
            
            var outputPort = baseNode.outputContainer.Query<Port>().ToList()
                .FirstOrDefault(p => p.portName == linkData.PortName);
            var inputPort = targetNode.inputContainer.Q<Port>();
            
            if (outputPort != null && inputPort != null)
            {
                var newEdge = outputPort.ConnectTo(inputPort);
                AddElement(newEdge);
            }
        }
        
        Debug.Log($"已載入 {dialogueContainer.DialogueNodes.Count} 個節點和 {dialogueContainer.NodeLinks.Count} 個連線");
    }
}