// InteractableObject.cs
using UnityEngine;
using UnityEngine.Events; // 引用 UnityEvent

public class InteractableObject : MonoBehaviour
{
    [Header("功能：單一物品放置判定點")]
    [Header("交互物件設置")]
    public string objectName = "物件"; // 物件名稱
    public ItemData requiredItem; // 需要的物品

    [Header("行為設定")] // [新增]
    [Tooltip("勾選：使用正確物品後銷毀此物件 (如一般交互點)\n不勾選：保留此物件但無法再互動 (如料理)")]
    public bool destroyOnUse = true; // 預設為 true，向下兼容你的舊機關

    [Header("視野下互動設定")]
    public bool interactiveInYang = true;
    public bool interactiveInYin = false;

    [Header("成功和失敗事件")]
    public UnityEvent onCorrectItemUsed;
    public UnityEvent onWrongItemUsed;

    /// <summary>
    /// 判斷該視野是否可互動
    /// </summary>
    public bool IsInteractiveIn(ViewType view)
    {
        return view == ViewType.Yang ? interactiveInYang : interactiveInYin;
    }

    /// <summary>
    /// 嘗試使用物品
    /// </summary>
    public bool UseItem(ItemData item)
    {
        if (item == null) return false;

        if (item == requiredItem)
        {
            Debug.Log($"[InteractableObject] 使用了正確物品 {item.itemName} 於 {objectName}");

            // 1. 觸發事件 (通知 Manager)
            onCorrectItemUsed?.Invoke();

            // 2. 根據設定決定去留
            if (destroyOnUse)
            {
                Destroy(gameObject); // 舊邏輯：直接銷毀
            }
            else
            {
                // 新邏輯：不銷毀，但要關閉互動功能
                // 關閉 Collider 讓射線無法再偵測到它，提示UI自然會消失
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;

                // 也可以選擇關閉腳本本身
                this.enabled = false;
            }

            return true;
        }
        else
        {
            Debug.Log($"[InteractableObject] 使用了錯誤物品 {item.itemName} 於 {objectName}");
            onWrongItemUsed?.Invoke();
            return false; // 使用失敗
        }
    }
}