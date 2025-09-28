using XNodeEditor;
using UnityEditor;


[CustomNodeGraphEditor(typeof(DialogueGraph))]
public class DialogueGraphEditor : NodeGraphEditor 
{
    public override string GetNodeMenuName(System.Type type) 
    {
        if (typeof(BaseNode).IsAssignableFrom(type))
        {
            return base.GetNodeMenuName(type).Replace("Dialogue/", "");
        }
        return null;
    }
}