// 請用這份程式碼完整取代你的 DialogueGraphView.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements; // 需要引用這個來使用 EnumField 和 ColorField
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    // ... 建構函式和 GetCompatiblePorts, BuildContextualMenu 方法保留不變 ...
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
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var graphViewMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        bool isEntryPoint = nodes.ToList().Count == 0;
        string nodeName = isEntryPoint ? "對話進入點" : "新的對話";
        string speakerName = isEntryPoint ? "系統" : "角色";
        evt.menu.AppendAction("新增對話節點", (action) =>
        {
            CreateNode(nodeName, isEntryPoint, graphViewMousePosition, speakerName: speakerName);
        });
    }

    // 在 DialogueGraphView.cs 中，找到並取代這個方法
    public void CreateNode(string nodeName, bool isEntryPoint, Vector2 position, string guid = null, string text = null, string speakerName = null, SpeakerNameStyle style = SpeakerNameStyle.Normal, Color color = default)
    {
        var node = new DialogueNode
        {
            title = isEntryPoint ? "進入點" : nodeName,
            GUID = guid ?? Guid.NewGuid().ToString(),
            EntryPoint = isEntryPoint,
            NameStyle = style,
            NameColor = (color == default) ? Color.white : color
        };

        node.SetDialogueText(text ?? nodeName);
        node.SetSpeakerName(speakerName ?? "角色");

        // --- 修改點: 我們不再把所有東西都塞到 titleContainer ---

        // 1. 將「新增選項」按鈕保留在標題列
        var button = new Button(() => { AddChoicePort(node, "新選項"); });
        button.text = "新增選項";
        node.titleContainer.Add(button);
        
        // 2. 啟用並將設定項全部移到 extensionContainer
        var extensionContainer = node.extensionContainer;
        extensionContainer.style.paddingLeft = 5; // 增加一點左邊距
        extensionContainer.style.paddingRight = 5;

        var speakerField = new TextField("角色:");
        speakerField.RegisterValueChangedCallback(evt => node.SetSpeakerName(evt.newValue));
        speakerField.SetValueWithoutNotify(node.SpeakerName);
        extensionContainer.Add(speakerField); // <-- 改為加到 extensionContainer

        var styleField = new EnumField("樣式:", node.NameStyle);
        styleField.RegisterValueChangedCallback(evt => node.SetNameStyle((SpeakerNameStyle)evt.newValue));
        extensionContainer.Add(styleField); // <-- 改為加到 extensionContainer

        var colorField = new ColorField("顏色:");
        colorField.RegisterValueChangedCallback(evt => node.SetNameColor(evt.newValue));
        colorField.SetValueWithoutNotify(node.NameColor);
        extensionContainer.Add(colorField); // <-- 改為加到 extensionContainer

        // 3. 主對話內容輸入框，維持不變，仍在 mainContainer
        var textField = new TextField(string.Empty) { multiline = true };
        textField.RegisterValueChangedCallback(evt => node.SetDialogueText(evt.newValue));
        textField.SetValueWithoutNotify(node.DialogueText);
        node.mainContainer.Add(textField);

        // 4. 接口 (Ports) 邏輯維持不變
        if (!node.EntryPoint)
        {
            var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            inputPort.portName = "輸入";
            node.inputContainer.Add(inputPort);
        }
        var continuePort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        continuePort.portName = "繼續";
        node.outputContainer.Add(continuePort);
        
        // 最後刷新並設定位置
        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(position, new Vector2(250, 200))); // 將預設寬度改為固定值，高度可以稍作調整
        AddElement(node);
    }
    
    // ... AddChoicePort 和 RemovePort 方法保留不變 ...
    public void AddChoicePort(DialogueNode node, string portName = "") { /* ... */ }
    private void RemovePort(Node node, Port port) { /* ... */ }
    
    // --- Save 和 Load 方法升級 ---
    public void Save(DialogueContainerSO dialogueContainer)
    {
        dialogueContainer.NodeLinks.Clear();
        dialogueContainer.DialogueNodes.Clear();

        foreach (var edge in edges.ToList()) { /* ... */ }
        
        foreach (var node in nodes.ToList().Cast<DialogueNode>())
        {
            dialogueContainer.DialogueNodes.Add(new DialogueNodeData
            {
                Guid = node.GUID,
                SpeakerName = node.SpeakerName,
                DialogueText = node.DialogueText,
                EntryPoint = node.EntryPoint,
                NameStyle = node.NameStyle, // 儲存樣式
                NameColor = node.NameColor, // 儲存顏色
                Position = node.GetPosition().position
            });
        }
    }
    
    public void Load(DialogueContainerSO dialogueContainer)
    {
        ClearGraph();
        foreach (var nodeData in dialogueContainer.DialogueNodes)
        {
            CreateNode(nodeData.DialogueText, nodeData.EntryPoint, nodeData.Position, nodeData.Guid, nodeData.DialogueText, nodeData.SpeakerName, nodeData.NameStyle, nodeData.NameColor);
        }
        
        foreach (var nodeData in dialogueContainer.DialogueNodes) { /* ... */ }
    }
    
    public void ClearGraph() { /* ... */ }
}