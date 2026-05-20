using System;
using UnityEngine;

public class InteractableRole : MonoBehaviour, IInteractable
{
    [Header("功能：鬼視野結束後獲得情報")]
    [Header("要解鎖的角色")]
    public RoleData targetRole; // ex: Role1, Role2
    [Header("要解鎖的 Carousel")]
    public CarouselData unlockCarousel;
    public string objectName = "神秘物品"; // 提示顯示用(PromptText)

    // [新增] 用來存放額外獲得的普通物品
    [Header("可選功能：同時獲得普通物品 (若無則留空)")]
    public ItemData optionalItemData;

    [Header("解鎖後要刪掉的物件")]
    public GameObject[] objects;

    #region ** IInteractable要求內容 **
    // 2. 實作提示文字
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? $"按 [叉] 與 {objectName} 對話" : $"按 [滑鼠左鍵] 與 {objectName} 對話";
    }

    // 3. 實作互動行為
    public void Interact(PlayerInteraction player)
    {
        Debug.Log($"[InteractableRole] 玩家與{objectName}交互");
        Interact(); // 執行它原本的邏輯
    }
    #endregion

    /// <summary>
    /// 執行交互（由 PlayerInteraction 呼叫）
    /// </summary>
    public void Interact()
    {
        // [!!] 新增的偵錯區塊 [!!]
        Debug.Log($"[InteractableRole] Interact() for '{this.gameObject.name}' 已被呼叫。");
        Debug.Log($"[InteractableRole] --- 正在檢查 IF 條件 ---");

        // 檢查 1: RolePastManager.Instance
        if (RolePastManager.Instance == null)
        {
            Debug.LogError($"[InteractableRole] 條件 1 失敗: RolePastManager.Instance 是 null！");
        }
        else
        {
            Debug.Log($"[InteractableRole] 條件 1 通過: RolePastManager.Instance 存在 (ID: {RolePastManager.Instance.GetInstanceID()})。");
        }

        // 檢查 2: targetRole
        if (targetRole == null)
        {
            Debug.LogError($"[InteractableRole] 條件 2 失敗: targetRole 是 null！");
        }
        else
        {
            Debug.Log($"[InteractableRole] 條件 2 通過: targetRole 是 '{targetRole.name}'。");
        }

        // 檢查 3: unlockCarousel
        if (unlockCarousel == null)
        {
            // [!!] 這 99% 是問題所在 [!!]
            Debug.LogError($"[InteractableRole] 條件 3 失敗: unlockCarousel 是 null！");
        }
        else
        {
            Debug.Log($"[InteractableRole] 條件 3 通過: unlockCarousel 是 '{unlockCarousel.name}'。");
        }
        Debug.Log($"[InteractableRole] --- IF 條件檢查完畢 ---");
        // [!!] 偵錯區塊結束 [!!]

        // [新增] 第 2.5 步：如果有放入 ItemData，就加進普通背包
        if (optionalItemData != null)
        {
            // 這裡需要替換成你遊戲中實際的「普通背包管理器」
            InventoryManager.Instance.AddItem(optionalItemData);
            Debug.Log($"[InteractableVoiceItem] 額外獲得了普通物品: {optionalItemData.itemName}");
        }

        // 不再使用 rolePastManager 變數，
        // 而是直接使用「全局單例」 RolePastManager.Instance
        if (RolePastManager.Instance != null && targetRole != null && unlockCarousel != null)
        {
            // 執行解鎖
            RolePastManager.Instance.AddCarouselToRole(targetRole, unlockCarousel);
            Debug.Log($"已解鎖 {targetRole.roleName} 的 Carousel: {unlockCarousel.name}");

            // ***** 【UX 修正】：關閉碰撞體，防止玩家在影片播放前重複狂點 *****
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }   
    }

    public void DestroyObjectsAfterVideo() 
    {
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null) // 加個 null 檢查比較安全
            {
                Destroy(objects[i]); // 解鎖後刪掉的物件
            }
        }
    }
}
