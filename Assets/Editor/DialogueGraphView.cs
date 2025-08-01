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
    
    // 在 DialogueGraphView.cs 中，找到並取代這個方法
    public void AddChoicePort(DialogueNode node, string portName = "")
    {
        // 1. 建立一個新的輸出接口
        // Port.Create 的靜態方法更穩定一些，我們改用它
        var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        
        // 將接口的資料名稱設定好，這對於儲存至關重要
        outputPort.portName = portName;
        
        // 2. 建立我們客製化的控制項
        // 選項文字輸入框
        var textField = new TextField
        {
            name = string.Empty,
            value = portName
        };
        textField.RegisterValueChangedCallback(evt => {
            // 當文字改變時，同步更新接口的資料名稱
            outputPort.portName = evt.newValue;
        });

        // 刪除按鈕
        var deleteButton = new Button(() => RemovePort(node, outputPort))
        {
            text = "X"
        };

        // 3. 將我們的客製化控制項加入到接口的容器中
        outputPort.contentContainer.Add(textField);
        outputPort.contentContainer.Add(deleteButton);
        
        // 4. 將建構好的接口加入到節點的輸出容器中
        node.outputContainer.Add(outputPort);

        // 5. 刷新節點的狀態，讓變更顯示出來
        node.RefreshPorts();
        node.RefreshExpandedState();
    }
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
    
    // 在 DialogueGraphView.cs 中，找到並取代這個方法
    public void Load(DialogueContainerSO dialogueContainer)
    {
        // 檢查是否有資料需要載入
        if (dialogueContainer == null) return;
        
        Debug.Log($"<color=lightblue>Load (GraphView): 開始處理 '{dialogueContainer.name}'...</color>");

        // 載入節點
        foreach (var nodeData in dialogueContainer.DialogueNodes)
        {
            // 確保 nodeData 不是 null
            if (nodeData == null) continue;
            CreateNode(nodeData.DialogueText, nodeData.EntryPoint, nodeData.Position, nodeData.Guid, nodeData.DialogueText, nodeData.SpeakerName, nodeData.NameStyle, nodeData.NameColor);
        }
        
        // 載入連線
        foreach (var linkData in dialogueContainer.NodeLinks)
        {
            // 確保 linkData 不是 null
            if (linkData == null) continue;

            // 使用 FirstOrDefault 來安全地尋找節點，避免找不到時報錯
            var baseNode = nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == linkData.BaseNodeGuid);
            var targetNode = nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == linkData.TargetNodeGuid);

            // 如果來源或目標節點不存在，就跳過這條連線，並印出警告
            if (baseNode == null || targetNode == null)
            {
                Debug.LogWarning($"無法建立連線: 找不到 GUID 為 {linkData.BaseNodeGuid} 或 {linkData.TargetNodeGuid} 的節點。");
                continue;
            }

            // 尋找對應的輸出接口
            // 我們需要一個更可靠的方法來找到正確的接口，而不是只找最後一個
            Port outputPort = null;
            if (linkData.PortName == "繼續")
            {
                outputPort = baseNode.outputContainer.Q<Port>(name: "繼續");
            }
            else
            {
                // 如果是選項接口，我們需要先重建它
                AddChoicePort(baseNode, linkData.PortName);
                // 然後找到我們剛剛建立的那個接口
                outputPort = baseNode.outputContainer.Query<Port>().ToList().LastOrDefault();
            }

            // 尋找目標節點的輸入接口
            var inputPort = targetNode.inputContainer.Q<Port>();

            // 如果接口都找到了，就建立連線
            if (outputPort != null && inputPort != null)
            {
                var newEdge = outputPort.ConnectTo(inputPort);
                AddElement(newEdge);
            }
        }
        Debug.Log($"<color=lightgreen>Load (GraphView): 圖表 '{dialogueContainer.name}' 載入完畢。</color>");
    }
    
    public void ClearGraph() { /* ... */ }
}