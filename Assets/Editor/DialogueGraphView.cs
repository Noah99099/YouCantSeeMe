using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    public DialogueGraphView()
    {
        // ... (這部分跟之前一樣，保留不動)
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
        // ... (以上保留)

        // --- 修改部分: 我們不再手動產生節點，而是留給右鍵選單 ---
        // GenerateNode("對話進入點", true, new Vector2(100, 200));
        // GenerateNode("選項 A", false, new Vector2(400, 200));
        // GenerateNode("選項 B", false, new Vector2(400, 350));
    }

    // --- 新增: 覆寫 GetCompatiblePorts 方法來定義連線規則 ---
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        // 這個方法決定了從一個接口可以連接到哪些其他接口
        var compatiblePorts = new List<Port>();

        ports.ForEach(port =>
        {
            // 基本規則：
            // 1. 接口的節點不能是同一個節點
            // 2. 接口的方向不能相同 (不能從 output 連到 output)
            if (startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });

        return compatiblePorts;
    }


    // --- 新增: 覆寫 BuildContextualMenu 來建立右鍵選單 ---
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        // base.BuildContextualMenu(evt); // 可以保留或移除，它會加入一些預設選項

        // 獲取滑鼠點擊的位置 (相對於 graphView)
        var graphViewMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);

        // 在選單中加入我們自己的選項
        evt.menu.AppendAction("新增對話節點", (action) =>
        {
            // 呼叫我們的節點生成方法，並傳入滑鼠點擊的位置
            CreateNode("新的對話", false, graphViewMousePosition);
        });
    }


    // --- 修改: 我們把 GenerateNode 改名為 CreateNode，功能更強大 ---
    private void CreateNode(string nodeName, bool isEntryPoint, Vector2 position)
    {
        // 創建一個 DialogueNode 的實例
        var node = new DialogueNode
        {
            title = nodeName,
            GUID = Guid.NewGuid().ToString(),
            EntryPoint = isEntryPoint
        };
        
        node.SetDialogueText(nodeName); // 初始設定節點的文字

        // --- 新增: 為節點加入可編輯的文字區域 ---
        var textField = new TextField(string.Empty);
        textField.RegisterValueChangedCallback(evt =>
        {
            // 當文字框內容改變時，更新節點的資料
            node.SetDialogueText(evt.newValue);
        });
        textField.SetValueWithoutNotify(node.title); // 設定初始值
        textField.multiline = true; // 允許多行輸入
        node.mainContainer.Add(textField); // 將文字框加入到節點的主容器中


        // --- 以下的接口設定邏輯與之前類似 ---
        if (!isEntryPoint)
        {
            var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            inputPort.portName = "輸入";
            node.inputContainer.Add(inputPort);
        }

        // 建立一個按鈕，用來新增選項接口
        var button = new Button(() => { AddChoicePort(node); });
        button.text = "新增選項";
        node.titleContainer.Add(button); // 將按鈕加到標題容器中

        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(position, new Vector2(200, 150)));
        AddElement(node);
    }
    
    // --- 新增: 一個專門用來新增選項接口的方法 ---
    public void AddChoicePort(DialogueNode node, string portName = "")
    {
        // 一個接口代表一個選項
        var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));

        // 接口旁邊的文字輸入框，用來編輯選項文字
        var oldLabel = outputPort.contentContainer.Q<Label>("type");
        outputPort.contentContainer.Remove(oldLabel);

        var textField = new TextField
        {
            name = string.Empty,
            value = portName
        };
        textField.RegisterValueChangedCallback(evt => outputPort.portName = evt.newValue);
        outputPort.contentContainer.Add(new Label("  ")); // 增加一點間距
        outputPort.contentContainer.Add(textField);

        // 刪除該選項的按鈕
        var deleteButton = new Button(() => RemovePort(node, outputPort))
        {
            text = "X"
        };
        outputPort.contentContainer.Add(deleteButton);
        
        outputPort.portName = portName;
        node.outputContainer.Add(outputPort);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }
    
    // --- 新增: 刪除接口的方法 ---
    private void RemovePort(Node node, Port port)
    {
        // 找到與此接口相連的所有連線
        var targetEdges = edges.ToList().Where(x => x.output == port || x.input == port);
        
        // 如果有連線，就先刪除連線
        if (targetEdges.Any())
        {
            var edge = targetEdges.First();
            edge.input.Disconnect(edge);
            edge.output.Disconnect(edge);
            RemoveElement(edge);
        }
        
        // 刪除接口本身
        node.outputContainer.Remove(port);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }
}