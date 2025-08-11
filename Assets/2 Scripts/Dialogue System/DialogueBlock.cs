using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueBlock
{
    [SerializeField] private string _guid;
    public string GUID { get => _guid; set => _guid = value; }
    public string BlockName;
    
    // 【新增】用來儲存下一個區塊的 GUID
    public string NextBlockGuid;

    [SerializeReference] 
    public List<Command> Commands = new List<Command>();
    
    public Vector2 Position; 
}