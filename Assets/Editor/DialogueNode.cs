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
    public bool EndPoint = false; 
    public Color NameColor;
    public Vector2 Position;
    
    public bool IsImportant; 

    private DialogueGraphView _graphView;
    private Button _addChoiceButton;
    private VisualElement _choiceContainer;
    
    public void Setup(DialogueGraphView graphView, bool isEntryPoint)
    {
        _graphView = graphView;
        EntryPoint = isEntryPoint;

        SetupNodeContent();

        if (!isEntryPoint)
        {
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(string));
            inputPort.portName = "";
            inputContainer.Add(inputPort);
        }
        
        if (!EndPoint)
        {
            CreateContinuePort();
            SetupChoiceManagementUI();
        }

        SetupNodeStyle();
        RefreshExpandedState();
        RefreshPorts();
    }
    
    public void SetAsEndPoint(bool isEndPoint)
    {
        EndPoint = isEndPoint;
        if (EndPoint) EntryPoint = false; 
        
        outputContainer.Clear();

        if (!isEndPoint)
        {
            CreateContinuePort();
            if (_choiceContainer != null && !extensionContainer.Contains(_choiceContainer))
            {
                extensionContainer.Add(_choiceContainer);
            }
        }
        else
        {
            if (_choiceContainer != null && extensionContainer.Contains(_choiceContainer))
            {
                extensionContainer.Remove(_choiceContainer);
            }
        }
        
        SetupNodeStyle();
        RefreshPorts();
    }

    public void SetupNodeStyle()
    {
        if (EntryPoint)
        {
            style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            title = "📍 " + (string.IsNullOrEmpty(SpeakerName) ? "對話開始" : SpeakerName);
        }
        else if (EndPoint)
        {
            style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);
            title = "🛑 " + (string.IsNullOrEmpty(SpeakerName) ? "對話結束" : SpeakerName);
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
        if (outputContainer.Query<Port>().ToList().Any(p => p.portName == "繼續")) return;

        var continuePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(string));
        continuePort.portName = "繼續";
        continuePort.portColor = Color.green;
        outputContainer.Add(continuePort);
    }

    private void SetupNodeContent()
    {
        var speakerNameTextField = new TextField("角色名稱:") { value = SpeakerName };
        speakerNameTextField.RegisterValueChangedCallback(evt => { SpeakerName = evt.newValue; SetupNodeStyle(); });
        titleContainer.Add(speakerNameTextField);
        
        var dialogueTextArea = new TextField("對話內容:") { value = DialogueText, multiline = true };
        dialogueTextArea.style.whiteSpace = WhiteSpace.Normal;
        dialogueTextArea.style.height = 80;
        dialogueTextArea.RegisterValueChangedCallback(evt => DialogueText = evt.newValue);
        mainContainer.Add(dialogueTextArea);
        
        var nameColorField = new ColorField("名稱顏色:") { value = NameColor };
        nameColorField.RegisterValueChangedCallback(evt => NameColor = evt.newValue);
        extensionContainer.Add(nameColorField);

        var importantToggle = new Toggle("重要節點 (跳過時停留)") { value = IsImportant };
        importantToggle.RegisterValueChangedCallback(evt => IsImportant = evt.newValue);
        extensionContainer.Add(importantToggle);
    }

    private void SetupChoiceManagementUI()
    {
        _choiceContainer = new VisualElement();
        _choiceContainer.style.marginTop = 10;
        _choiceContainer.style.paddingTop = 5;
        _choiceContainer.style.borderTopWidth = 1;
        _choiceContainer.style.borderTopColor = Color.gray;
        
        var choiceLabel = new Label("對話選項:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
        _choiceContainer.Add(choiceLabel);

        _addChoiceButton = new Button(AddNewChoice) { text = "➕ 添加選項" };
        _choiceContainer.Add(_addChoiceButton);
        
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
        
        var textField = new TextField() { value = portName };
        textField.RegisterValueChangedCallback(evt => choicePort.portName = evt.newValue);
        
        var deleteButton = new Button(() => schedule.Execute(() => RemovePort(graphView, choicePort))) { text = "🗑️" };
        
        portContainer.Add(textField);
        portContainer.Add(deleteButton);
        choicePort.contentContainer.Add(portContainer);

        outputContainer.Add(choicePort);
        RefreshPorts();
        UpdateContinuePortVisibility();
    }
    
    public void RemovePort(DialogueGraphView graphView, Port port)
    {
        if (port == null || port.portName == "繼續") return;
        if (port.connected)
        {
            var edgesToRemove = port.connections.ToList();
            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }
        }
        outputContainer.Remove(port);
        RefreshPorts();
        UpdateContinuePortVisibility();
    }

    private void UpdateContinuePortVisibility()
    {
        var continuePort = outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == "繼續");
        if (continuePort != null)
        {
            continuePort.style.display = GetChoicePortCount() > 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    public void LoadChoicePorts(List<string> choiceNames, DialogueGraphView graphView)
    {
        foreach (string choiceName in choiceNames)
        {
            AddChoicePort(graphView, choiceName);
        }
    }

    // 【*** 重新加入遺失的方法 ***】
    // 這個方法會獲取節點上所有「選項」端口的名稱
    public List<string> GetChoicePortNames()
    {
        return outputContainer.Query<Port>().ToList()
            .Where(p => p.portName != "繼續")
            .Select(p => p.portName)
            .ToList();
    }
}