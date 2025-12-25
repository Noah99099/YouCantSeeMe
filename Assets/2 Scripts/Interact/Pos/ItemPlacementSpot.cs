using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class ItemPlacementSpot : MonoBehaviour
{
    [Header("功能：多物品放置判定點")]
    [Header("判定設置")]
    public string spotName = "放置點";

    // 核心修改1：改為 List，允許這個點接受多種不同的物品 (例如 A 或 B 都可以)
    public List<ItemData> acceptedItems = new List<ItemData>();

    [Header("生成設置")]
    // 核心修改2：指定物品生成的位置 (如果為空，則默認生成在當前物件位置)
    public Transform spawnRoot;
    public bool disableColliderAfterPlaced = true; // 放置成功後是否關閉碰撞體(避免重複放置)

    [Header("視野下互動設定")]
    public bool interactiveInYang = true;
    public bool interactiveInYin = false;

    [Header("事件")]
    // 成功時傳出 ItemData，方便你做更細緻的處理 (例如播放特定音效)
    public UnityEvent<ItemData> onCorrectItemPlaced;
    public UnityEvent onWrongItemUsed;

    // 內部狀態：是否已經放了東西
    private bool isOccupied = false;

    /// <summary>
    /// 判斷該視野是否可互動
    /// </summary>
    public bool IsInteractiveIn(ViewType view)
    {
        // 如果已經放了東西，就不允許再互動
        if (isOccupied) return false;

        return view == ViewType.Yang ? interactiveInYang : interactiveInYin;
    }

    /// <summary>
    /// 嘗試放置物品
    /// </summary>
    public bool TryPlaceItem(ItemData item)
    {
        if (isOccupied) return false;
        if (item == null) return false;

        // 檢查玩家手上的物品是否在允許清單中
        if (acceptedItems.Contains(item))
        {
            Debug.Log($"[ItemPlacementSpot] 在 {spotName} 成功放置了 {item.itemName}");

            // 1. 生成物品模型
            PlaceItemModel(item);

            // 2. 標記為已占用
            isOccupied = true;

            // 3. 觸發成功事件
            onCorrectItemPlaced?.Invoke(item);

            // 4. 根據設定關閉碰撞體 (讓準心不再顯示交互圖示)
            if (disableColliderAfterPlaced)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }

            return true; // 告訴系統消耗物品
        }
        else
        {
            Debug.Log($"[ItemPlacementSpot] {spotName} 不接受物品 {item.itemName}");
            onWrongItemUsed?.Invoke();
            return false;
        }
    }

    /// <summary>
    /// 實際生成模型的邏輯
    /// </summary>
    private void PlaceItemModel(ItemData item)
    {
        if (item.modelPrefab == null)
        {
            Debug.LogWarning($"物品 {item.itemName} 沒有設定 modelPrefab，無法生成模型！");
            return;
        }

        // 決定生成點：如果有指定 spawnRoot 就用它的位置，否則用腳本掛載的位置
        Transform targetTransform = spawnRoot != null ? spawnRoot : transform;

        // 生成模型
        GameObject placedObj = Instantiate(item.modelPrefab, targetTransform.position, targetTransform.rotation);

        // 建議將生成的物品設為 spawnRoot 的子物件，保持場景整潔
        placedObj.transform.SetParent(targetTransform);

        // (可選) 如果生成的模型本身有 Collider，可能需要在這裡移除或設為 Trigger，避免擋住射線
    }
}