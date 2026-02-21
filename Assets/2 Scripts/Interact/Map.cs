using System;
using UnityEngine;

public class Map : MonoBehaviour, IInteractable
{
    // 您可以在這裡添加物品的名稱或描述，以便在UI提示中使用
    public string itemName = "平面圖";

    // 當此物件被拾取時的事件（不再用單例）
    public static event Action GetMap;

    #region ** IInteractable要求內容 **
    // 2. 實作提示文字
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? $"按 [叉] 獲得{itemName}" : $"按 [滑鼠左鍵] 獲得{itemName}";
    }

    // 3. 實作互動行為
    public void Interact(PlayerInteraction player)
    {
        Debug.Log($"[Map] 玩家獲得平面圖");
        Collect(); // 執行它原本的邏輯       
    }
    #endregion

    /// <summary>
    /// 呼叫此方法以觸發拾取事件。
    /// </summary>
    public void Collect()
    {
        Debug.Log($"Map: {itemName} 已被拾取！觸發 GetMap 事件。");
        GetMap?.Invoke();

        // ***** 【關鍵補齊】：原本寫在 PlayerInteraction 的銷毀邏輯，現在要自己負責！ *****
        Destroy(gameObject);
    }
}
