using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "GlobalVariableDatabase", menuName = "Dialogue/Global Variable Database")]
public class GlobalVariableDatabase : ScriptableObject
{

    [Tooltip("遊戲開始時，變數會被重置為這個列表中的初始值")]
    public List<Variable> initialVariables = new List<Variable>();
    [Tooltip("遊戲執行期間，實際會被修改的變數列表")]
    public List<Variable> runtimeVariables = new List<Variable>();

    public void ResetVariables()
    {
        runtimeVariables.Clear();
        foreach (var initialVar in initialVariables)
        {
            runtimeVariables.Add(new Variable { name = initialVar.name, value = initialVar.value });
        }
    }
    
    public float GetVariable(string variableName)
    {
        var variable = runtimeVariables.FirstOrDefault(v => v.name == variableName);
        if (variable != null) return variable.value;
        return 0f;
    }

    public void SetVariable(string variableName, float newValue)
    {
        var variable = runtimeVariables.FirstOrDefault(v => v.name == variableName);
        if (variable != null) variable.value = newValue;
        else runtimeVariables.Add(new Variable { name = variableName, value = newValue });
    }
}