using System;
using UnityEngine;

public class InteractableRole : MonoBehaviour
{
    [Header("功能：鬼視野結束後獲得情報")]
    [Header("要解鎖的角色")]
    public RoleData targetRole; // ex: Role1, Role2
    [Header("要解鎖的 Carousel")]
    public CarouselData unlockCarousel;
    [Header("RolePastManager腳本")]
    public RolePastManager rolePastManager;
    public string objectName = "神秘物品"; // 提示顯示用(PromptText)
    [Header("解鎖後要刪掉的物件")]
    public GameObject[] objects;

    /// <summary>
    /// 執行交互（由 PlayerInteraction 呼叫）
    /// </summary>
    public void Interact()
    {
        if (rolePastManager != null && targetRole != null && unlockCarousel != null)
        {
            rolePastManager.AddCarouselToRole(targetRole, unlockCarousel);
            Debug.Log($"已解鎖 {targetRole.roleName} 的 Carousel: {unlockCarousel.name}");
        }

        for (int i = 0; i < objects.Length; i++)
        {
            Destroy(objects[i]); // 解鎖後刪掉的物件
        }
    }
}
