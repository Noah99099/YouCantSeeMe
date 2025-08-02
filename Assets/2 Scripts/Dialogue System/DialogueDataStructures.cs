using System;
using UnityEngine;

[Serializable]
public class DialogueCondition
{
    public string variableName;
    public ComparisonOperator comparisonOperator;
    public string compareValue;
}

[Serializable]
public class DialogueAction
{
    public string actionType;
    public string parameter1;
    public string parameter2;
}