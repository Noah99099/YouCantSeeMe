using System;

// [Serializable] 讓這個類別的實例可以被 Unity 序列化並顯示在 Inspector 中
[Serializable]
public abstract class Command
{
    /// <summary>
    /// 執行這個指令。
    /// </summary>
    /// <param name="onComplete">當指令執行完畢時要呼叫的回呼函式。</param>
    public abstract void Execute(DialogueRunner runner, Action onComplete);
}