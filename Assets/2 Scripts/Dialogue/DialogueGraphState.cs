using System.Collections.Generic;

[System.Serializable] // 這個屬性是讓這個類別可以被 Unity 序列化的關鍵
public class DialogueGraphState
{
    public string graphGUID; // 使用 Asset 的 GUID 作為唯一識別碼
    public List<Variable> variables;

    public DialogueGraphState(string guid, List<Variable> vars)
    {
        graphGUID = guid;
        variables = vars;
    }
}

// 建立一個容器來儲存所有對話圖形的狀態
[System.Serializable]
public class AllDialoguesState
{
    public List<DialogueGraphState> graphStates;
}