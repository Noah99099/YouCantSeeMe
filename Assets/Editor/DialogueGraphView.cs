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
    private readonly float _nodeWidth = 280f; // 增加寬度以容納新的UI元素

    public DialogueGraphView()
    {
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
        
        // 添加樣式表
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

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var graphViewMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        bool isEntryPoint = nodes.ToList().Count == 0;
        
        evt.menu.AppendAction("新增對話節點", action => CreateNode("新的對話", isEntryPoint, graphViewMousePosition));
        
        // 如果右鍵點擊的是節點，添加節點特定的選項
        if (evt.target is DialogueNode clickedNode)
        {
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("添加選項", action => AddChoicePort(clickedNode, $"選項 {clickedNode.GetChoicePortCount() + 1}"));
            evt.menu.AppendAction("設為入口點", action => SetAsEntryPoint(clickedNode));
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
            NameColor = Color.white
        };
        
        dialogueNode.Setup(this, isEntryPoint);
        dialogueNode.SetPosition(new Rect(position, new Vector2(_nodeWidth, 200)));
        
        AddElement(dialogueNode);
    }
    
    // 為節點添加選項的方法
    public void AddChoicePort(DialogueNode node, string portName)
    {
        node.AddChoicePort(this, portName);
    }

    private void SetAsEntryPoint(DialogueNode targetNode)
    {
        // 移除其他節點的入口點狀態
        foreach (var node in nodes.ToList().Cast<DialogueNode>())
        {
            if (node != targetNode && node.EntryPoint)
            {
                node.EntryPoint = false;
                node.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                node.title = node.SpeakerName;
            }
        }
        
        // 設置新的入口點
        targetNode.EntryPoint = true;
        targetNode.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        targetNode.title = "📍 " + targetNode.SpeakerName;
    }

    private void DuplicateNode(DialogueNode originalNode, Vector2 position)
    {
        var newNode = new DialogueNode
        {
            title = originalNode.title + " (複製)",
            DialogueText = originalNode.DialogueText,
            SpeakerName = originalNode.SpeakerName + " (複製)",
            GUID = Guid.NewGuid().ToString(),
            EntryPoint = false, // 複製的節點不應該是入口點
            Position = position + new Vector2(50, 50), // 稍微偏移位置
            NameColor = originalNode.NameColor
        };
        
        newNode.Setup(this, false);
        newNode.SetPosition(new Rect(position + new Vector2(50, 50), new Vector2(_nodeWidth, 200)));
        
        // 複製原節點的所有選項
        var choiceNames = originalNode.GetChoicePortNames();
        foreach (var choiceName in choiceNames)
        {
            AddChoicePort(newNode, choiceName);
        }
        
        AddElement(newNode);
    }

    public void Save(DialogueContainerSO dialogueContainer)
    {
        // 清除舊資料
        dialogueContainer.NodeLinks.Clear();
        dialogueContainer.DialogueNodes.Clear();

        // 儲存連線資訊
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
        
        // 儲存節點資訊
        foreach (var node in nodes.ToList().Cast<DialogueNode>())
        {
            dialogueContainer.DialogueNodes.Add(new DialogueNodeData
            {
                Guid = node.GUID,
                DialogueText = node.DialogueText,
                SpeakerName = node.SpeakerName,
                EntryPoint = node.EntryPoint,
                Position = node.GetPosition().position,
                NameColor = node.NameColor
            });
        }
        
        Debug.Log($"已儲存 {dialogueContainer.DialogueNodes.Count} 個節點和 {dialogueContainer.NodeLinks.Count} 個連線");
    }

    public void LoadGraph(DialogueContainerSO dialogueContainer)
    {
        // 清空目前的圖表
        DeleteElements(nodes.ToList());
        DeleteElements(edges.ToList());
        _currentDialogueContainer = dialogueContainer;

        if (dialogueContainer.DialogueNodes == null || dialogueContainer.DialogueNodes.Count == 0) 
        {
            Debug.Log("對話容器為空，創建空白圖表");
            return;
        }

        // 重新創建所有節點
        foreach (var nodeData in dialogueContainer.DialogueNodes)
        {
            var dialogueNode = new DialogueNode
            {
                GUID = nodeData.Guid,
                DialogueText = nodeData.DialogueText,
                SpeakerName = nodeData.SpeakerName,
                EntryPoint = nodeData.EntryPoint,
                NameColor = nodeData.NameColor,
                Position = nodeData.Position,
            };
            
            dialogueNode.Setup(this, dialogueNode.EntryPoint);
            dialogueNode.SetPosition(new Rect(dialogueNode.Position, new Vector2(_nodeWidth, 200)));
            AddElement(dialogueNode);
        }

        // 收集每個節點的選項端口名稱
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

        // 為每個節點添加其選項端口
        foreach (var kvp in nodeChoices)
        {
            var node = nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == kvp.Key);
            if (node != null)
            {
                node.LoadChoicePorts(kvp.Value, this);
            }
        }

        // 重新建立所有連線
        foreach (var linkData in dialogueContainer.NodeLinks)
        {
            var baseNode = nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == linkData.BaseNodeGuid);
            var targetNode = nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == linkData.TargetNodeGuid);

            if (baseNode == null || targetNode == null)
            {
                Debug.LogWarning($"無法建立連線: 找不到節點 {linkData.BaseNodeGuid} 或 {linkData.TargetNodeGuid}");
                continue;
            }

            // 尋找對應的輸出端口
            var outputPort = baseNode.outputContainer.Query<Port>().ToList()
                .FirstOrDefault(p => p.portName == linkData.PortName);
            var inputPort = targetNode.inputContainer.Q<Port>();

            // 建立連線
            if (outputPort != null && inputPort != null)
            {
                var newEdge = outputPort.ConnectTo(inputPort);
                AddElement(newEdge);
            }
        }
        
        Debug.Log($"已載入 {dialogueContainer.DialogueNodes.Count} 個節點和 {dialogueContainer.NodeLinks.Count} 個連線");
    }
}