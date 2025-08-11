using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;

public class BlockNode : Node
{
    public string GUID;
    public string BlockName;
    public bool EntryPoint = false;
    public DialogueBlock BlockData;

    // 用來存放所有指令UI的容器
    private VisualElement _commandsContainer;

    public BlockNode(DialogueBlock block)
    {
        this.BlockData = block;
        this.title = block.BlockName;
        this.GUID = block.GUID; // 【修正】直接從傳入的資料中讀取 GUID，確保其持久不變

        // --- 後續的 UI 創建邏輯與之前相同 ---
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        inputPort.portName = "In";
        inputContainer.Add(inputPort);

        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        var titleLabel = this.Q<Label>("title-label");
        var titleContainer = this.Q("title");
        var titleField = new TextField { name = "title-field", value = this.title, isDelayed = true };
        titleField.RegisterValueChangedCallback(evt =>
        {
            this.title = evt.newValue;
            BlockData.BlockName = evt.newValue; // 改名只影響名稱，不影響 GUID
        });
        titleContainer.Insert(0, titleField);
        titleLabel.visible = false;

        _commandsContainer = new VisualElement { name = "commands-container" };
        this.mainContainer.Add(_commandsContainer);

        var addSayCommandButton = new Button(AddSayCommand) { text = "新增說話指令 (Say)" };
        this.mainContainer.Add(addSayCommandButton);
    }

    /// <summary>
    /// 為一個 SayCommand 資料創建對應的 UI 元素並加入到節點中
    /// </summary>
    public void AddSayCommandUI(SayCommand command)
    {
        // 建立一個容器來放這個指令的所有UI
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

        // 說話者名稱的輸入框
        var speakerField = new TextField("說話者 (Speaker)") { value = command.SpeakerName };
        speakerField.RegisterValueChangedCallback(evt =>
        {
            command.SpeakerName = evt.newValue;
        });
        commandContainer.Add(speakerField);

        // 對話內容的輸入框
        var dialogueField = new TextField("對話內容 (Text)") { value = command.DialogueText, multiline = true };
        dialogueField.style.minHeight = 40; // 讓多行輸入框有個初始高度
        dialogueField.RegisterValueChangedCallback(evt =>
        {
            command.DialogueText = evt.newValue;
        });
        commandContainer.Add(dialogueField);

        // 刪除按鈕
        var deleteButton = new Button(() =>
        {
            BlockData.Commands.Remove(command); // 從資料中移除
            _commandsContainer.Remove(commandContainer); // 從UI中移除
        })
        { text = "刪除此指令" };
        commandContainer.Add(deleteButton);

        // 將這個指令的UI容器，加入到節點的指令列表中
        _commandsContainer.Add(commandContainer);
    }

    /// <summary>
    /// 處理按鈕點擊：創建新的 SayCommand 資料，並呼叫 UI 創建方法
    /// </summary>
    private void AddSayCommand()
    {
        // 1. 創建新的指令資料
        var newCommand = new SayCommand
        {
            SpeakerName = "新角色",
            DialogueText = "新的對話內容..."
        };

        // 2. 將資料加入到 Block 的指令列表中
        BlockData.Commands.Add(newCommand);

        // 3. 根據新的資料，創建對應的 UI
        AddSayCommandUI(newCommand);
    }
    
    public void SetEntryPointStyle(bool isEntryPoint)
{
    this.EntryPoint = isEntryPoint;
    var titleStyle = this.Q("title").style;

    if (isEntryPoint)
    {
        // 如果是入口點，設定為明亮的綠色，且不可刪除
        titleStyle.backgroundColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f, 0.8f));
        this.capabilities &= ~Capabilities.Deletable;
    }
    else
    {
        // 如果不是入口點，還原為預設顏色，且可以被刪除
        titleStyle.backgroundColor = new StyleColor(StyleKeyword.Null); 
        this.capabilities |= Capabilities.Deletable;
    }
}
}