using System;

/// <summary>
/// 定義角色名稱的字體樣式。
/// </summary>
public enum SpeakerNameStyle
{
    Normal,
    Bold,
    Italic
}

public enum DialogueNodeType
{
    Normal,        // 普通對話
    Choice,        // 選擇分支
    Condition,     // 條件檢查
    Action,        // 執行動作
    RandomChoice   // 隨機選擇
}

public enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual
}