// RolePastManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic; // [新] 為了使用 Dictionary 和 List
using System.Linq; // [新] 為了 .ToList()

public class RolePastManager : MonoBehaviour
{
    // [!!] 新增：Singleton (單例) 實例 [!!]
    public static RolePastManager Instance { get; private set; }

    [Header("功能：管理一個角色的多個 Carousel（含文本、大箭頭）")]
    // 一角色一方案，一方案多carousel -> 一個角色有多個carousel
    public RoleData[] allRoles; // 所有角色的資料（包含多個 CarouselData）
    public CarouselController carouselController; //CarouselController腳本
    public TMP_Text text1;
    public TMP_Text text2;

    [Header("大箭頭 UI（切換 CarouselData）")]
    public Button leftArrow; //上一個CarouselData的大箭頭
    public Button rightArrow; //下一個CarouselData的大箭頭

    [Header("右側 UI 父物件")]
    public GameObject rightInformation; // 控制整個右側的開關

    // --- [新] 狀態管理 ---
    /// <summary>
    /// [新] 儲存玩家「已解鎖」的回憶。
    /// Key: RoleData (角色), Value: 該角色已解鎖的回憶 Set
    /// </summary>
    public Dictionary<RoleData, HashSet<CarouselData>> unlockedMemories = new Dictionary<RoleData, HashSet<CarouselData>>();

    // --- [修改] 私有變數 ---
    private RoleData currentRole;
    private int currentCarouselIndex = 0; // 當前角色的第幾個 CarouselData

    // [新] 用於顯示的「已解鎖」回憶的「排序列表」
    private List<CarouselData> _currentRoleUnlockedList = new List<CarouselData>();

    private void Awake()
    {
        // 檢查是否已經有一個 Instance 存在
        if (Instance != null && Instance != this)
        {
            // 如果有，代表這個是重複的 (例如 RPM_B)
            // 立刻銷毀這個重複的物件，然後返回
            Debug.LogWarning("[RPM] 發現重複的 RolePastManager 實例，已銷毀。");
            Destroy(this.gameObject); // 銷毀這個「組件」所在的「整個遊戲物件」
            return;
        }

        // 如果 Instance 是空的，將它設為 "this" (也就是 RPM_A)
        Instance = this;

        // [重要] 讓這個管理器在切換場景或 UI 時「倖存」，不要被銷毀
        DontDestroyOnLoad(this.gameObject);

        // [新] 初始化字典 (只會在 RPM_A 上執行一次)
        unlockedMemories = new Dictionary<RoleData, HashSet<CarouselData>>();
    }

    private void Start()
    {
        if (rightInformation == null)
        {
            Debug.LogError("RightInformation 沒有在 Inspector 綁定！");
        }

        // [修改] 預設隱藏右側。
        // 我們不再檢查 allRoles[0].carousels.Length，因為那是不準的 (SO)
        // 預設隱藏右側，因為沒有一個Role有Carousel
        rightInformation.SetActive(false);

        // 這裡加檢查按鈕 1 對應的角色
        //if (allRoles != null && allRoles.Length > 0)
        //{
        //    RoleData role1 = allRoles[0];
        //    if (role1 != null && role1.carousels != null && role1.carousels.Length > 0)
        //    {
        //        // 如果 Role1 已經有 Carousels，就直接顯示右側資訊
        //        rightInformation.SetActive(true);
        //        ShowRole(role1);
        //    }
        //}

        // 綁定大箭頭的點擊事件
        if (leftArrow != null)
            leftArrow.onClick.AddListener(PreviousCarousel);

        if (rightArrow != null)
            rightArrow.onClick.AddListener(NextCarousel);

        // [修改] 遊戲一開始時，我們假設玩家還沒解鎖任何東西
        // 如果您希望一開始就解鎖某個角色的某個回憶 (例如教學)，
        // 您可以在這裡手動呼叫 AddCarouselToRole(allRoles[0], allRoles[0].carousels[0]);
    }

    public void ShowRole(RoleData role) //選擇一個角色的方案（由左側按鈕呼叫）
    {
        // [!!] 偵錯 3 [!!]
        Debug.Log($"[RPM] ShowRole: 按鈕點擊，要求顯示 '{role.name}' (InstanceID: {role.GetInstanceID()})");
        Debug.Log($"[RPM] ShowRole: 將檢查字典中是否存在此 Key...");

        currentRole = role;
        currentCarouselIndex = 0;

        // [新] 刷新當前角色的已解鎖列表
        UpdateUnlockedListForCurrentRole();

        ShowCurrentCarousel();
    }

    /// <summary>
    /// [新] 輔助函式：刷新 _currentRoleUnlockedList
    /// </summary>
    private void UpdateUnlockedListForCurrentRole()
    {
        _currentRoleUnlockedList.Clear();

        // [!!] 偵錯 4 [!!]
        if (currentRole == null)
        {
            Debug.LogWarning("[RPM] UpdateUnlockedList: currentRole 是 null。");
            return;
        }

        if (unlockedMemories.ContainsKey(currentRole))
        {
            // [!!] 偵錯 5 [!!]
            Debug.Log("[RPM] UpdateUnlockedList: Key 找到了！正在填充列表。");
            _currentRoleUnlockedList = unlockedMemories[currentRole].ToList();
        }
        else
        {
            // [!!] 偵錯 6 [!!]
            Debug.LogWarning($"[RPM] UpdateUnlockedList: 找不到 Key '{currentRole.name}' (ID: {currentRole.GetInstanceID()})！");
        }
    }

    public void NextCarousel() //切換到該角色的下一個 CarouselData（由大箭頭呼叫）
    {
        if (_currentRoleUnlockedList.Count == 0) return;

        currentCarouselIndex = (currentCarouselIndex + 1) % _currentRoleUnlockedList.Count;
        ShowCurrentCarousel();
    }

    public void PreviousCarousel() //切換到該角色的上一個 CarouselData（由大箭頭呼叫）
    {
        if (_currentRoleUnlockedList.Count == 0) return;

        currentCarouselIndex = (currentCarouselIndex - 1 + _currentRoleUnlockedList.Count) % _currentRoleUnlockedList.Count;
        ShowCurrentCarousel();
    }

    private void ShowCurrentCarousel() //顯示當前的 CarouselData（更新圖片與文字）
    {
        if (_currentRoleUnlockedList.Count == 0)
        {
            Debug.Log("[RPM] ShowCurrentCarousel: 已解鎖列表為空 (Count=0)，隱藏 rightInformation。");

            rightInformation.SetActive(false); // 沒有資料 → 隱藏右側
            return;
        }

        rightInformation.SetActive(true); // 有資料 → 顯示右側

        // [修改] 從「已解鎖列表」中獲取資料
        var carouselData = _currentRoleUnlockedList[currentCarouselIndex];

        // 更新圖片
        carouselController.SetCarousel(carouselData.images);

        // 更新文本
        text1.text = carouselData.texts.Length > 0 ? carouselData.texts[0] : "";
        text2.text = carouselData.texts.Length > 1 ? carouselData.texts[1] : "";

        // [新] 更新大箭頭的顯示 (如果只有 1 個，就隱藏)
        bool showArrows = _currentRoleUnlockedList.Count > 1;
        if (leftArrow != null) leftArrow.gameObject.SetActive(showArrows);
        if (rightArrow != null) rightArrow.gameObject.SetActive(showArrows);
    }

    /// <summary> 當互動物體解鎖新的 Carousel 時呼叫 </summary>
    public void AddCarouselToRole(RoleData role, CarouselData newCarousel)
    {
        // [!!] 偵錯 1 [!!]
        Debug.Log($"[RPM] AddCarouselToRole: 正在為 '{role.name}' (InstanceID: {role.GetInstanceID()}) 添加回憶。");

        // [!!] 移除 [!!] 
        // role.AddCarousel(newCarousel); // 絕對不要在執行期修改 SO！

        // 1. [新] 檢查 Role 鍵是否存在
        if (!unlockedMemories.ContainsKey(role))
        {
            unlockedMemories[role] = new HashSet<CarouselData>();
        }

        // 2. [新] 嘗試加入 Carousel。
        // (HashSet.Add 會自動處理重複，如果已存在，會返回 false)
        bool isNewCarousel = unlockedMemories[role].Add(newCarousel);

        // [!!] 偵錯 2 [!!]
        Debug.Log($"[RPM] AddCarouselToRole: 字典中目前所有 Keys: {string.Join(", ", unlockedMemories.Keys.Select(k => k.name + " (ID: " + k.GetInstanceID() + ")"))}");

        // 3. [新] 如果這是一個「新解鎖」的回憶...
        if (isNewCarousel)
        {
            // [!!] 在這裡通知 ClueCombinationManager 獲得案件物品 [!!]
            ClueCombinationManager.Instance?.CheckForNewPuzzleUnlocks();
        }

        // [修改] 如果剛好是當前顯示的角色，刷新 UI
        if (role == currentRole)
        {
            // [新] 刷新列表
            UpdateUnlockedListForCurrentRole();
            // [修改] 顯示剛解鎖的最後一個
            currentCarouselIndex = _currentRoleUnlockedList.Count - 1;
            ShowCurrentCarousel();
        }
    }
}
