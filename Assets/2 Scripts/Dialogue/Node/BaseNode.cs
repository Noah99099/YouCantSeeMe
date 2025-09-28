using UnityEngine;
using XNode;

public abstract class BaseNode : Node 
{
    [Header("劇情設置")]
    public bool isImportant = false;
    // 我們將 entry 和 exit 移到真正需要它們的子類別中

    public override object GetValue(NodePort port)
    {
        return null;
    }

    // 這個方法暫時用不到，但可以先留著
    public virtual BaseNode GetNextNode()
    {
        return null;
    }
}