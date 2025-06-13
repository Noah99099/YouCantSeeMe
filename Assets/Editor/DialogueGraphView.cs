using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    // ... 建構函式 DialogueGraphView() 保留不變 ...
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
    
    // 我們將 CreateNode 方法的存取權限改為 public，這樣 DialogueGraph 才能呼叫它
    public void CreateNode(string nodeName, bool isEntryPoint, Vector2 position, string guid = null, string text = null)
    {
        var node = new DialogueNode
        {
            title = nodeName,
            GUID = guid ?? Guid.NewGuid().ToString(), // 如果傳入的 guid 是 null，就產生一個新的
            EntryPoint = isEntryPoint
        };
        
        node.SetDialogueText(text ?? nodeName);

        var textField = new TextField(string.Empty);
        textField.RegisterValueChangedCallback(evt => node.SetDialogueText(evt.newValue));
        textField.SetValueWithoutNotify(node.DialogueText);
        textField.multiline = true;
        node.mainContainer.Add(textField);

        if (!isEntryPoint)
        {
            var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            inputPort.portName = "輸入";
            node.inputContainer.Add(inputPort);
        }

        var button = new Button(() => { AddChoicePort(node, "新選項"); });
        button.text = "新增選項";
        node.titleContainer.Add(button);

        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(position, new Vector2(200, 150)));
        AddElement(node);
    }
    
    // ... AddChoicePort 和 RemovePort 方法保留不變 ...
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

    // --- 新增: 儲存執行的核心方法 ---
    public void Save(DialogueContainerSO dialogueContainer)
    {
        dialogueContainer.NodeLinks.Clear();
        dialogueContainer.DialogueNodes.Clear();

        // 儲存節點連線
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
        
        // 儲存節點本身
        foreach (var node in nodes.ToList().Cast<DialogueNode>())
        {
            dialogueContainer.DialogueNodes.Add(new DialogueNodeData
            {
                Guid = node.GUID,
                DialogueText = node.DialogueText,
                Position = node.GetPosition().position
            });
        }
    }
    
    // --- 新增: 讀取執行的核心方法 ---
    public void Load(DialogueContainerSO dialogueContainer)
    {
        // 載入節點
        foreach (var nodeData in dialogueContainer.DialogueNodes)
        {
            CreateNode(nodeData.DialogueText, false, nodeData.Position, nodeData.Guid, nodeData.DialogueText);
        }
        
        // 載入連線
        foreach (var nodeData in dialogueContainer.DialogueNodes)
        {
            // 找到所有從此節點出發的連線
            var links = dialogueContainer.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.Guid).ToList();
            foreach (var link in links)
            {
                var baseNode = nodes.ToList().Cast<DialogueNode>().First(x => x.GUID == link.BaseNodeGuid);
                var targetNode = nodes.ToList().Cast<DialogueNode>().First(x => x.GUID == link.TargetNodeGuid);
                
                // 為來源節點新增接口
                AddChoicePort(baseNode, link.PortName);
                
                // 找到剛剛新增的接口，並與目標節點的輸入接口連接
                var newEdge = baseNode.outputContainer.Query<Port>().ToList().Last().ConnectTo(targetNode.inputContainer[0] as Port);
                AddElement(newEdge);
            }
        }
    }
    
    // --- 新增: 清空圖表的方法 ---
    public void ClearGraph()
    {
        // 刪除所有連線
        edges.ForEach(edge => RemoveElement(edge));
        // 刪除所有節點
        nodes.ForEach(node => RemoveElement(node));
    }
}