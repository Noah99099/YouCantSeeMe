using UnityEngine;

public class InteractableRole : MonoBehaviour
{
    [Header("要解鎖的角色")]
    public RoleData targetRole; // ex: Role1, Role2
    [Header("要解鎖的 Carousel")]
    public CarouselData unlockCarousel;
    [Header("RolePastManager腳本")]
    public RolePastManager rolePastManager;

    public string objectName = "神秘物品"; // 提示顯示用(PromptText)

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
        Destroy(gameObject); // 解鎖後物體消失
    }
}
