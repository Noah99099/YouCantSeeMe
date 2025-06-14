using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    // ... 建構函式和 GetCompatiblePorts 方法保留不變 ...
    public DialogueGraphView()
    {
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
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
    
    // --- 修改: BuildContextualMenu 方法 ---
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var graphViewMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        
        // --- 新增邏輯: 決定新節點是否為進入點 ---
        // 規則：如果圖上沒有任何節點，那麼第一個建立的節點就是進入點
        bool isEntryPoint = nodes.ToList().Count == 0;
        
        string nodeName = isEntryPoint ? "對話進入點" : "新的對話";
        string speakerName = isEntryPoint ? "系統" : "角色";
        
        evt.menu.AppendAction("新增對話節點", (action) =>
        {
            CreateNode(nodeName, isEntryPoint, graphViewMousePosition, speakerName: speakerName);
        });
    }

    public void CreateNode(string nodeName, bool isEntryPoint, Vector2 position, string guid = null, string text = null, string speakerName = null)
    {
        var node = new DialogueNode
        {
            title = isEntryPoint ? "進入點" : nodeName, // 進入點的標題固定
            GUID = guid ?? Guid.NewGuid().ToString(),
            EntryPoint = isEntryPoint // 設定節點的進入點狀態
        };
        
        // ... 後面的程式碼與上一版相同，但為了完整性，這裡全部列出 ...
        node.SetDialogueText(text ?? nodeName);
        node.SetSpeakerName(speakerName ?? "角色");

        var speakerField = new TextField("角色:");
        speakerField.RegisterValueChangedCallback(evt => node.SetSpeakerName(evt.newValue));
        speakerField.SetValueWithoutNotify(node.SpeakerName);
        node.titleContainer.Insert(1, speakerField);

        var textField = new TextField(string.Empty);
        textField.RegisterValueChangedCallback(evt => node.SetDialogueText(evt.newValue));
        textField.SetValueWithoutNotify(node.DialogueText);
        textField.multiline = true;
        node.mainContainer.Add(textField);

        if (!node.EntryPoint) // 進入點不需要輸入接口
        {
            var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            inputPort.portName = "輸入";
            node.inputContainer.Add(inputPort);
        }

        var continuePort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        continuePort.portName = "繼續";
        node.outputContainer.Add(continuePort);

        var button = new Button(() => { AddChoicePort(node, "新選項"); });
        button.text = "新增選項";
        node.titleContainer.Add(button);

        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(position, new Vector2(200, 200)));
        AddElement(node);
    }
    
    public void AddChoicePort(DialogueNode node, string portName = "")
    {
        var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        var oldLabel = outputPort.contentContainer.Q<Label>("type");
        outputPort.contentContainer.Remove(oldLabel);
        var textField = new TextField { name = string.Empty, value = portName };
        textField.RegisterValueChangedCallback(evt => outputPort.portName = evt.newValue);
        outputPort.contentContainer.Add(new Label("  "));
        outputPort.contentContainer.Add(textField);
        var deleteButton = new Button(() => RemovePort(node, outputPort)) { text = "X" };
        outputPort.contentContainer.Add(deleteButton);
        outputPort.portName = portName;
        node.outputContainer.Add(outputPort);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }
    private void RemovePort(Node node, Port port)
    {
        var targetEdges = edges.ToList().Where(x => x.output == port || x.input == port);
        if (targetEdges.Any())
        {
            var edge = targetEdges.First();
            edge.input.Disconnect(edge);
            edge.output.Disconnect(edge);
            RemoveElement(edge);
        }
        node.outputContainer.Remove(port);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }
    
    public void Save(DialogueContainerSO dialogueContainer)
    {
        dialogueContainer.NodeLinks.Clear();
        dialogueContainer.DialogueNodes.Clear();

        foreach (var edge in edges.ToList())
        {
            var outputNode = edge.output.node as DialogueNode;
            var inputNode = edge.input.node as DialogueNode;
            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.GUID,
                TargetNodeGuid = inputNode.GUID,
                PortName = edge.output.portName
            });
        }
        
        foreach (var node in nodes.ToList().Cast<DialogueNode>())
        {
            dialogueContainer.DialogueNodes.Add(new DialogueNodeData
            {
                Guid = node.GUID,
                SpeakerName = node.SpeakerName,
                DialogueText = node.DialogueText,
                EntryPoint = node.EntryPoint, // 把 EntryPoint 狀態存進去！
                Position = node.GetPosition().position
            });
        }
    }
    
    public void Load(DialogueContainerSO dialogueContainer)
    {
        // 載入節點
        foreach (var nodeData in dialogueContainer.DialogueNodes)
        {
            CreateNode(nodeData.DialogueText, nodeData.EntryPoint, nodeData.Position, nodeData.Guid, nodeData.DialogueText, nodeData.SpeakerName);
        }
        
        // 載入連線
        foreach (var nodeData in dialogueContainer.DialogueNodes)
        {
            var links = dialogueContainer.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.Guid).ToList();
            foreach (var link in links)
            {
                var baseNode = nodes.ToList().Cast<DialogueNode>().First(x => x.GUID == link.BaseNodeGuid);
                var targetNode = nodes.ToList().Cast<DialogueNode>().First(x => x.GUID == link.TargetNodeGuid);
                
                if (link.PortName == "繼續")
                {
                    var continuePort = baseNode.outputContainer.Q<Port>(name: "繼續");
                    var newEdge = continuePort.ConnectTo(targetNode.inputContainer[0] as Port);
                    AddElement(newEdge);
                }
                else
                {
                    AddChoicePort(baseNode, link.PortName);
                    var newEdge = baseNode.outputContainer.Query<Port>().ToList().Last().ConnectTo(targetNode.inputContainer[0] as Port);
                    AddElement(newEdge);
                }
            }
        }
    }
    
    public void ClearGraph()
    {
        edges.ForEach(edge => RemoveElement(edge));
        nodes.ForEach(node => RemoveElement(node));
    }
}