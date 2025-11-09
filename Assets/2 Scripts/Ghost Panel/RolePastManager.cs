using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// [!!] 這是「純邏輯」管理器 [!!]
// 把它放在 Scene 1 的一個「空」的「根物件」上，例如 [Managers]
public class RolePastManager : MonoBehaviour
{
    public static RolePastManager Instance { get; private set; }

    // [!!] 核心資料庫 [!!]
    public Dictionary<RoleData, HashSet<CarouselData>> unlockedMemories = new Dictionary<RoleData, HashSet<CarouselData>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // 這次它會成功，因為它在「根物件」上

        unlockedMemories = new Dictionary<RoleData, HashSet<CarouselData>>();
    }

    /// <summary> 
    /// (核心) 當互動物體解鎖新的 Carousel 時呼叫 
    /// </summary>
    public void AddCarouselToRole(RoleData role, CarouselData newCarousel)
    {
        // 防呆
        if (role == null || newCarousel == null)
        {
            Debug.LogWarning($"[RPM] AddCarouselToRole: 傳入的 role 或 newCarousel 是 null，已中斷。");
            return;
        }

        Debug.Log($"[RPM] AddCarouselToRole: 正在為 '{role.name}' 添加回憶 '{newCarousel.name}'。");

        if (!unlockedMemories.ContainsKey(role))
        {
            unlockedMemories[role] = new HashSet<CarouselData>();
        }

        bool isNewCarousel = unlockedMemories[role].Add(newCarousel);

        Debug.Log($"[RPM] AddCarouselToRole: 字典中目前所有 Keys: {string.Join(", ", unlockedMemories.Keys.Select(k => k.name))}");

        if (isNewCarousel)
        {
            // 呼叫 CheckForNewPuzzleUnlocks 時，
            // 傳入 EClueType.Memory 來告知 CCM 這次的解鎖類型
            ClueCombinationManager.Instance?.CheckForNewPuzzleUnlocks(false, EClueType.Memory);
        }
    }
}