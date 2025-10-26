using System;
using UnityEngine;

public class Map : MonoBehaviour
{
    // 您可以在這裡添加物品的名稱或描述，以便在UI提示中使用
    public string itemName = "平面圖";

    // 當此物件被拾取時的事件（不再用單例）
    public static event Action GetMap;

    /// <summary>
    /// 呼叫此方法以觸發拾取事件。
    /// </summary>
    public void Collect()
    {
        Debug.Log($"Map: {itemName} 已被拾取！");
        GetMap?.Invoke();
    }
}
