using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

public class BlockNode : Node
{
    public string GUID;
    public string BlockName;
    public bool EntryPoint = false;
    public DialogueBlock BlockData;

    private Port _defaultOutputPort;
    private VisualElement _commandsContainer;

    public BlockNode(DialogueBlock block)
    {
        this.BlockData = block;
        this.title = block.BlockName;
        this.GUID = block.GUID;

        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        inputPort.portName = "In";
        inputContainer.Add(inputPort);

        _defaultOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        _defaultOutputPort.portName = "Next";
        outputContainer.Add(_defaultOutputPort);

        var titleLabel = this.Q<Label>("title-label");
        var titleContainer = this.Q("title");
        var titleField = new TextField { name = "title-field", value = this.title, isDelayed = true };
        titleField.RegisterValueChangedCallback(evt =>
        {
            this.title = evt.newValue;
            BlockData.BlockName = evt.newValue;
        });
        titleContainer.Insert(0, titleField);
        titleLabel.visible = false;

        _commandsContainer = new VisualElement { name = "commands-container" };
        this.mainContainer.Add(_commandsContainer);
        
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        
        var addSayCommandButton = new Button(AddSayCommand) { text = "新增說話 (Say)" };
        buttonContainer.Add(addSayCommandButton);
        
        // 【修正】確保按鈕呼叫的方法存在
        var addChoiceCommandButton = new Button(AddChoiceCommand) { text = "新增選項 (Choice)" };
        buttonContainer.Add(addChoiceCommandButton);
        
        this.mainContainer.Add(buttonContainer);
    }
    
    // --- 以下是原本 SayCommand 的方法，保持不變 ---
    public void AddSayCommandUI(SayCommand command)
    {
        var commandContainer = new VisualElement();
        commandContainer.style.flexDirection = FlexDirection.Column;
        commandContainer.style.borderLeftWidth = 1;
        commandContainer.style.borderRightWidth = 1;
        commandContainer.style.borderTopWidth = 1;
        commandContainer.style.borderBottomWidth = 1;
        commandContainer.style.borderTopColor = Color.gray;
        commandContainer.style.borderBottomColor = Color.gray;
        commandContainer.style.borderLeftColor = Color.gray;
        commandContainer.style.borderRightColor = Color.gray;
        commandContainer.style.marginTop = 5;

        var speakerField = new TextField("說話者 (Speaker)") { value = command.SpeakerName };
        speakerField.RegisterValueChangedCallback(evt => { command.SpeakerName = evt.newValue; });
        commandContainer.Add(speakerField);

        var dialogueField = new TextField("對話內容 (Text)") { value = command.DialogueText, multiline = true };
        dialogueField.style.minHeight = 40;
        dialogueField.RegisterValueChangedCallback(evt => { command.DialogueText = evt.newValue; });
        commandContainer.Add(dialogueField);

        var deleteButton = new Button(() => {
            BlockData.Commands.Remove(command);
            _commandsContainer.Remove(commandContainer);
        }) { text = "刪除此指令" };
        commandContainer.Add(deleteButton);

        _commandsContainer.Add(commandContainer);
    }

    private void AddSayCommand()
    {
        var newCommand = new SayCommand { SpeakerName = "新角色", DialogueText = "新的對話內容..." };
        BlockData.Commands.Add(newCommand);
        AddSayCommandUI(newCommand);
    }

    // --- 以下是新增與修正的 ChoiceCommand 相關方法 ---

    public bool HasChoiceCommand()
    {
        return BlockData.Commands.OfType<ChoiceCommand>().Any();
    }

    public void UpdateDefaultPortVisibility()
    {
        _defaultOutputPort.SetEnabled(!HasChoiceCommand());
        // 【修正】使用 .visible 屬性，而不是 SetVisible() 方法
        _defaultOutputPort.visible = !HasChoiceCommand();
    }

    /// <summary>
    /// 【新增】補上這個遺漏的方法
    /// </summary>
    private void AddChoiceCommand()
    {
        if (HasChoiceCommand())
        {
            EditorUtility.DisplayDialog("錯誤", "一個區塊 (Block) 只能有一個選項指令 (Choice Command)。", "確定");
            return;
        }

        var newCommand = new ChoiceCommand();
        BlockData.Commands.Add(newCommand);
        AddChoiceCommandUI(newCommand);
        UpdateDefaultPortVisibility();
    }
    
    public void AddChoiceCommandUI(ChoiceCommand command)
    {
        var commandContainer = new VisualElement { name = "choice-command-container" };
        // 【修正】分別設定 padding 的四個方向
        commandContainer.style.paddingTop = 5;
        commandContainer.style.paddingBottom = 5;
        commandContainer.style.paddingLeft = 5;
        commandContainer.style.paddingRight = 5;
        commandContainer.style.borderTopWidth = 1;
        commandContainer.style.borderTopColor = Color.gray;

        var addChoiceButton = new Button(() => {
            var newChoice = new ChoiceCommand.Choice { ChoiceText = "新選項" };
            command.Choices.Add(newChoice);
            CreateChoicePort(command, newChoice, commandContainer);
        }) { text = "新增選項" };
        commandContainer.Add(addChoiceButton);

        var deleteButton = new Button(() => {
            // 在刪除指令前，先把所有由這個指令創建的 Port 都從節點上移除
            var portsToRemove = outputContainer.Query<Port>().Where(p => p.userData is ChoiceCommand.Choice).ToList();
            foreach (var port in portsToRemove)
            {
                outputContainer.Remove(port);
            }
            
            BlockData.Commands.Remove(command);
            _commandsContainer.Remove(commandContainer);
            UpdateDefaultPortVisibility();
        }) { text = "刪除此選項指令" };
        commandContainer.Add(deleteButton);

        _commandsContainer.Add(commandContainer);

        foreach (var choice in command.Choices)
        {
            CreateChoicePort(command, choice, commandContainer);
        }
        UpdateDefaultPortVisibility();
    }

    private void CreateChoicePort(ChoiceCommand ownerCommand, ChoiceCommand.Choice choice, VisualElement commandContainer)
    {
        var choiceContainer = new VisualElement();
        choiceContainer.style.flexDirection = FlexDirection.Row;
        choiceContainer.style.alignItems = Align.Center;

        var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        port.portName = "";
        port.userData = choice;
        choiceContainer.Add(port);

        var textField = new TextField { value = choice.ChoiceText, isDelayed = true };
        textField.RegisterValueChangedCallback(evt => { choice.ChoiceText = evt.newValue; });
        textField.style.flexGrow = 1;
        choiceContainer.Add(textField);

        var deleteChoiceButton = new Button(() => {
            ownerCommand.Choices.Remove(choice);
            outputContainer.Remove(port);
            commandContainer.Remove(choiceContainer);
        }) { text = "X" };
        choiceContainer.Add(deleteChoiceButton);

        outputContainer.Add(port);
        commandContainer.Insert(commandContainer.childCount - 2, choiceContainer);
    }
    
    // --- SetEntryPointStyle 方法保持不變 ---
    public void SetEntryPointStyle(bool isEntryPoint)
    {
        this.EntryPoint = isEntryPoint;
        var titleStyle = this.Q("title").style;

        if (isEntryPoint)
        {
            titleStyle.backgroundColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f, 0.8f));
            this.capabilities &= ~Capabilities.Deletable;
        }
        else
        {
            titleStyle.backgroundColor = new StyleColor(StyleKeyword.Null); 
            this.capabilities |= Capabilities.Deletable;
        }
    }
}