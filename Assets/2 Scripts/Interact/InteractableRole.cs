using System;
using UnityEngine;

public class InteractableRole : MonoBehaviour
{
    [Header("功能：鬼視野結束後獲得情報")]
    [Header("要解鎖的角色")]
    public RoleData targetRole; // ex: Role1, Role2
    [Header("要解鎖的 Carousel")]
    public CarouselData unlockCarousel;

    public string objectName = "神秘物品"; // 提示顯示用(PromptText)
    [Header("解鎖後要刪掉的物件")]
    public GameObject[] objects;

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

        // 不再使用 rolePastManager 變數，
        // 而是直接使用「全局單例」 RolePastManager.Instance
        if (RolePastManager.Instance != null && targetRole != null && unlockCarousel != null)
        {
            // [!!] 修改這一行 [!!]
            RolePastManager.Instance.AddCarouselToRole(targetRole, unlockCarousel);

            Debug.Log($"已解K {targetRole.roleName} 的 Carousel: {unlockCarousel.name}");
        }   
    }

    public void DestoryObjectsAfterVideo() 
    {
        for (int i = 0; i < objects.Length; i++)
        {
            Destroy(objects[i]); // 解鎖後刪掉的物件
        }
    }
}
