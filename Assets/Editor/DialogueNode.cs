using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class DialogueNode : Node
{
    public string GUID;
    public string SpeakerName;
    public string DialogueText;
    public bool EntryPoint = false;
    public Color NameColor;
    public Vector2 Position;
    
    // 【*** 新增欄位 ***】
    // 用來在 UI 上直接存取節點是否為重要
    public bool IsImportant; 

    private DialogueGraphView _graphView;
    private Button _addChoiceButton;
    private VisualElement _choiceContainer;
    
    public void Setup(DialogueGraphView graphView, bool isEntryPoint)
    {
        _graphView = graphView;
        EntryPoint = isEntryPoint;

        SetupNodeStyle();

        if (!isEntryPoint)
        {
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(string));
            inputPort.portName = "";
            inputContainer.Add(inputPort);
        }
        
        CreateContinuePort();
        SetupNodeContent();
        SetupChoiceManagement();

        RefreshExpandedState();
        RefreshPorts();
    }

    private void SetupNodeStyle()
    {
        if (EntryPoint)
        {
            style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            title = "📍 " + (string.IsNullOrEmpty(SpeakerName) ? "對話開始" : SpeakerName);
        }
        else
        {
            style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            title = string.IsNullOrEmpty(SpeakerName) ? "對話節點" : SpeakerName;
        }

        style.minWidth = 250;
    }

    private void CreateContinuePort()
    {
        var continuePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(string));
        continuePort.portName = "繼續";
        continuePort.portColor = Color.green;
        outputContainer.Add(continuePort);
    }

    private void SetupNodeContent()
    {
        var speakerNameTextField = new TextField("角色名稱:")
        {
            value = SpeakerName
        };
        speakerNameTextField.style.marginBottom = 5;
        speakerNameTextField.RegisterValueChangedCallback(evt =>
        {
            SpeakerName = evt.newValue;
            title = EntryPoint ? "📍 " + SpeakerName : SpeakerName;
        });
        titleContainer.Add(speakerNameTextField);
        
        var dialogueTextArea = new TextField("對話內容:")
        {
            value = DialogueText,
            multiline = true
        };
        dialogueTextArea.style.whiteSpace = WhiteSpace.Normal;
        dialogueTextArea.style.height = 80;
        dialogueTextArea.style.marginBottom = 10;
        dialogueTextArea.RegisterValueChangedCallback(evt => DialogueText = evt.newValue);
        mainContainer.Add(dialogueTextArea);
        
        var nameColorField = new ColorField("名稱顏色:")
        {
            value = NameColor
        };
        nameColorField.style.marginBottom = 10;
        extensionContainer.Add(nameColorField);

        // 【*** 新增 UI 元件 ***】
        // 創建一個 Toggle (勾選框) 來設定 IsImportant 屬性
        var importantToggle = new Toggle("重要節點 (跳過時停留)")
        {
            value = IsImportant // 初始值
        };
        importantToggle.style.marginTop = 5;
        // 當勾選框的狀態改變時，更新節點的 IsImportant 變數
        importantToggle.RegisterValueChangedCallback(evt =>
        {
            IsImportant = evt.newValue;
        });
        extensionContainer.Add(importantToggle);
    }

    // ... (SetupChoiceManagement 及之後的所有方法保持不變) ...
    private void SetupChoiceManagement()
    {
        _choiceContainer = new VisualElement();
        _choiceContainer.style.marginTop = 10;
        _choiceContainer.style.paddingTop = 5;
        _choiceContainer.style.borderTopWidth = 1;
        _choiceContainer.style.borderTopColor = Color.gray;
        
        var choiceLabel = new Label("對話選項:");
        choiceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        choiceLabel.style.marginBottom = 5;
        _choiceContainer.Add(choiceLabel);

        _addChoiceButton = new Button(AddNewChoice)
        {
            text = "➕ 添加選項"
        };
        _addChoiceButton.style.backgroundColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        _addChoiceButton.style.color = Color.white;
        _addChoiceButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        _addChoiceButton.style.height = 30;
        _addChoiceButton.style.marginBottom = 5;
        _choiceContainer.Add(_addChoiceButton);

        var helpText = new Label("💡 提示：添加選項後，玩家將看到多個選擇按鈕而不是「繼續」提示。");
        helpText.style.fontSize = 11;
        helpText.style.color = new Color(0.7f, 0.7f, 0.7f);
        helpText.style.whiteSpace = WhiteSpace.Normal;
        helpText.style.marginBottom = 5;
        _choiceContainer.Add(helpText);

        extensionContainer.Add(_choiceContainer);
    }

    private void AddNewChoice()
    {
        string defaultChoiceName = $"選項 {GetChoicePortCount() + 1}";
        AddChoicePort(_graphView, defaultChoiceName);
    }

    public int GetChoicePortCount()
    {
        return outputContainer.Query<Port>().ToList().Count(p => p.portName != "繼續");
    }
    
    public void AddChoicePort(DialogueGraphView graphView, string portName)
    {
        var choicePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(string));
        choicePort.portName = portName;
        choicePort.portColor = Color.yellow;
        
        var portContainer = new VisualElement();
        portContainer.style.flexDirection = FlexDirection.Row;
        portContainer.style.alignItems = Align.Center;
        portContainer.style.marginBottom = 3;
        
        var textField = new TextField()
        {
            value = portName
        };
        textField.style.flexGrow = 1;
        textField.style.marginRight = 5;
        textField.RegisterValueChangedCallback(evt =>
        {
            choicePort.portName = evt.newValue;
        });
        
        var deleteButton = new Button(() =>
        {
            schedule.Execute(() => RemovePort(graphView, choicePort));
        })
        {
            text = "🗑️"
        };
        deleteButton.style.width = 25;
        deleteButton.style.height = 20;
        deleteButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        deleteButton.style.color = Color.white;
        
        portContainer.Add(textField);
        portContainer.Add(deleteButton);
        choicePort.contentContainer.Add(portContainer);

        var continuePort = outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == "繼續");
        if (continuePort != null)
        {
            var continueIndex = outputContainer.IndexOf(continuePort);
            outputContainer.Insert(continueIndex, choicePort);
        }
        else
        {
            outputContainer.Add(choicePort);
        }

        RefreshExpandedState();
        RefreshPorts();
        UpdateAddChoiceButtonText();
    }
    
    public void RemovePort(DialogueGraphView graphView, Port port)
    {
        if (port == null) return;

        if (port.portName == "繼續")
        {
            Debug.LogWarning("無法刪除「繼續」端口！");
            return;
        }

        if (port.connected)
        {
            var edgesToRemove = port.connections.ToList();
            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }
        }
        
        outputContainer.Remove(port);
        RefreshExpandedState();
        RefreshPorts();
        UpdateAddChoiceButtonText();
    }

    private void UpdateAddChoiceButtonText()
    {
        int choiceCount = GetChoicePortCount();
        if (_addChoiceButton != null)
        {
            _addChoiceButton.text = choiceCount == 0 ? "➕ 添加選項" : $"➕ 添加選項 ({choiceCount})";
        }
        
        var continuePort = outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == "繼續");
        if (continuePort != null)
        {
            continuePort.style.display = choiceCount > 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    public void LoadChoicePorts(List<string> choiceNames, DialogueGraphView graphView)
    {
        foreach (string choiceName in choiceNames)
        {
            AddChoicePort(graphView, choiceName);
        }
    }

    public List<string> GetChoicePortNames()
    {
        return outputContainer.Query<Port>().ToList()
            .Where(p => p.portName != "繼續")
            .Select(p => p.portName)
            .ToList();
    }

    public bool HasChoices()
    {
        return GetChoicePortCount() > 0;
    }
}