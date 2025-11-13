using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// [全新] 組合面板的 核心管理器
/// </summary>
public class ClueCombinationManager : MonoBehaviour
{
    // [新] Singleton (單例)
    public static ClueCombinationManager Instance { get; private set; }

    [Header("所有的謎題")]
    public List<ClueCombinationPuzzle> allPuzzles;
    //private int currentPuzzleIndex = 0;

    // 這會根據索引對應謎題，必須按照順序
    // (例如 allPuzzles[0] 會對應 puzzleRewardItems[0])
    [Header("謎題對應的獎勵物品 (必須對應 allPuzzles)")]
    public List<ItemData> puzzleRewardItems;

    //[Header("其他管理器")]
    //// [!!] 移除 [!!] 讓它在 Inspector 拖入，避免未來出錯
    //public RolePastManager rolePastManager;

    [Header("UI 引用")]
    public InventoryClueGrid inventoryGrid;  // 左側線索欄
    public ClueDetailsPanel detailsPanel;    // 右下詳細資訊

    [Header("導覽按鈕")]
    public Button nextButton;
    public Button prevButton;

    [Header("Puzzle UI Prefab (請在此指定)")]
    public GameObject puzzleContainerPrefab; // [!!] 請指定 PuzzleContainer_Prefab
    public Transform puzzleContainerParent;  // [!!] 請指定謎題的 "PuzzlePanel" (父物件)

    // --- 內部狀態 ---
    private CombinationSlotUI _currentSelectedSlot; // 當前點選的待填入空格
    private IClue _currentSelectedClue;             // 當前點選的待使用線索

    // 這會儲存所有謎題的狀態 (key: slot索引, value: 填入的ClueID)
    // Key: 謎題的 .name (當 ID), Value: 該謎題的狀態 (Key: slot索引, Value: ClueID)
    private Dictionary<string, Dictionary<int, string>> _allPuzzleStates;

    // [新] 儲存所有被實例化的謎題 UI 物件
    private List<PuzzleContainerUI> _instantiatedPuzzles = new List<PuzzleContainerUI>();
    private PuzzleContainerUI _activePuzzleUI; // [新] 當前顯示的 UI 管理器

    // [新] 儲存已被解鎖的謎題在 allPuzzles 中的原始索引
    // (例如 [1, 0] 表示 test2 (索引1) 和 test1 (索引0) 被解鎖了)
    private List<int> _unlockedPuzzleMasterIndices = new List<int>();

    // [新] 當前顯示的謎題在 _unlockedPuzzleMasterIndices 清單中的索引
    // (例如 0, 1)
    private int _currentUnlockedListIndex = -1;

    // [新] 當前顯示的謎題在 allPuzzles 總清單中的索引
    // (例如 0, 1) - 用於 CheckCombination
    private int _currentActiveMasterIndex = -1;

    // [新] Awake (用於 Singleton)
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // [!!] 確保 CCM 也是 DontDestroyOnLoad
        }
    }

    void Start()
    {
        // 檢查是否引用了 UI
        if (inventoryGrid == null || detailsPanel == null || puzzleContainerPrefab == null || puzzleContainerParent == null)
        {
            Debug.LogError("[ClueCombinationManager] UI 引用尚未設定！");
            return;
        }

        // [新] 初始化所有謎題的狀態儲存
        _allPuzzleStates = new Dictionary<string, Dictionary<int, string>>();
        // 在遊戲一開始，預先實例化所有的 Puzzle UI
        _instantiatedPuzzles.Clear();
        foreach (var puzzle in allPuzzles)
        {
            // 1. 初始化儲存空間
            // TODO: 這裡未來可以讀取玩家的存檔
            _allPuzzleStates[puzzle.name] = new Dictionary<int, string>();
            Dictionary<int, string> puzzleState = _allPuzzleStates[puzzle.name];

            // 2. 實例化 UI Prefab
            GameObject go = Instantiate(puzzleContainerPrefab, puzzleContainerParent);
            PuzzleContainerUI ui = go.GetComponent<PuzzleContainerUI>();

            if (ui != null)
            {
                // 3. 初始化這個 UI 實例 (僅執行一次)
                // (請注意:SetupPuzzle 不是 DisplayPuzzle)
                ui.SetupPuzzle(puzzle, puzzleState, this, OnSlotClicked);

                // 4. 預設隱藏
                go.SetActive(false);
                _instantiatedPuzzles.Add(ui);
            }
        }

        // [新] 幫 Manager 綁定導覽按鈕
        if (nextButton != null) nextButton.onClick.AddListener(NextPuzzle);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousPuzzle);

        // 3. [新] 遊戲一開始，檢查解鎖狀態 (例如讀取存檔後)
        _unlockedPuzzleMasterIndices.Clear();
        CheckForNewPuzzleUnlocks(true); // true = 這是第一次載入
    }

    /// <summary>
    /// 透過「已解鎖清單」的索引來載入謎題
    /// </summary>
    public void LoadPuzzleByUnlockedIndex(int unlockedListIndex)
    {
        // 檢查是否真的有已解鎖的謎題
        if (_unlockedPuzzleMasterIndices.Count == 0 || unlockedListIndex < 0 || unlockedListIndex >= _unlockedPuzzleMasterIndices.Count)
        {
            // [新] 若沒有謎題可以顯示，隱藏所有內容
            foreach (var ui in _instantiatedPuzzles) ui.gameObject.SetActive(false);
            _activePuzzleUI = null;
            _currentUnlockedListIndex = -1;
            _currentActiveMasterIndex = -1;

            // 關閉相關介面 (避免殘留)
            inventoryGrid.Hide(); // 假設有 Hide()
            detailsPanel.Hide();
            return;
        }

        _currentUnlockedListIndex = unlockedListIndex;
        // [新] 找到謎題的「總索引」
        _currentActiveMasterIndex = _unlockedPuzzleMasterIndices[_currentUnlockedListIndex];

        // 將所有 UI 隱藏，只顯示當前的
        for (int i = 0; i < _instantiatedPuzzles.Count; i++)
        {
            bool isActive = (i == _currentActiveMasterIndex);
            _instantiatedPuzzles[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                // [新] 儲存當前顯示的 UI 管理器
                _activePuzzleUI = _instantiatedPuzzles[i];
            }
        }

        // 刷新一次「線索」清單
        // (InventoryClueGrid.cs 的 Show 方法會清除 clues 清單並重新篩選，只會顯示標題，不會顯示內容)
        inventoryGrid.Show(new List<IClue>(), EClueType.Item, OnGridItemClicked);
        // 隱藏詳細資訊介面
        detailsPanel.Hide();
    }

    /// <summary>
    /// (Callback) 當玩家點選了「待填入」的空格時
    /// </summary>
    public void OnSlotClicked(CombinationSlotUI slot)
    {
        // [!!] 偵錯 1 [!!]
        Debug.Log($"[CCM] OnSlotClicked: 點選了格子 {slot.SlotIndex}。格子需要的類型是: {slot.RequiredClueType}");

        if (slot.IsLocked) return;

        _currentSelectedSlot = slot;
        detailsPanel.Hide();

        // [!!] 偵錯 2 [!!]
        Debug.Log("[CCM] OnSlotClicked: 正在呼叫 GetEligibleClues...");
        // [核心方法] 呼叫並篩選出「符合類型」的「玩家持有」的線索
        List<IClue> eligibleClues = GetEligibleClues(slot.RequiredClueType);

        // [!!] 偵錯 3 [!!]
        Debug.Log($"[CCM] OnSlotClicked: GetEligibleClues 執行完畢。共找到 {eligibleClues.Count} 條線索。正在呼叫 inventoryGrid.Show...");
        inventoryGrid.Show(eligibleClues, slot.RequiredClueType, OnGridItemClicked);
    }

    /// <summary>
    /// (Callback) 當玩家點選了左側「線索欄」中的線索
    /// </summary>
    public void OnGridItemClicked(IClue clue)
    {
        _currentSelectedClue = clue;
        detailsPanel.Show(clue.ClueName, clue.ClueDescription, OnUseItemClicked);
    }

    /// <summary>
    /// (Callback) 當玩家點選了右下角「使用此線索」的按鈕
    /// </summary>
    public void OnUseItemClicked()
    {
        if (_currentSelectedSlot == null || _currentSelectedClue == null) return;

        _currentSelectedSlot.FillSlot(_currentSelectedClue);

        // [修正] 必須使用 _currentActiveMasterIndex
        if (_currentActiveMasterIndex == -1) return; // 安全檢查

        // 找到當前謎題和其狀態
        ClueCombinationPuzzle puzzle = allPuzzles[_currentActiveMasterIndex];
        Dictionary<int, string> currentPuzzleState = _allPuzzleStates[puzzle.name];

        // 將線索存入「儲存系統」中
        currentPuzzleState[_currentSelectedSlot.SlotIndex] = _currentSelectedClue.ClueID;
        // TODO: SaveStateForPuzzle(puzzle.name, currentPuzzleState);

        // [!!] 需求 #3 (隱藏)
        // 填入線索後，立刻清空「線索欄」，避免混淆
        inventoryGrid.Show(new List<IClue>(), EClueType.Item, OnGridItemClicked);
        detailsPanel.Hide();
        _currentSelectedSlot = null;
        _currentSelectedClue = null;

        CheckCombination();
    }

    /// <summary>
    // 檢查當前組合是否正確
    /// </summary>
    private void CheckCombination()
    {
        // [修正] 必須使用 _currentActiveMasterIndex
        if (_currentActiveMasterIndex == -1) return; // 安全檢查

        ClueCombinationPuzzle puzzle = allPuzzles[_currentActiveMasterIndex];
        // [新] 從「儲存系統」讀取狀態
        Dictionary<int, string> currentPuzzleState = _allPuzzleStates[puzzle.name];
        int totalSlots = puzzle.clueSlots.Count; // [新] 獲取總共的格子數

        // 1. 檢查是否所有格子都填滿了
        bool allFilled = true;
        for (int i = 0; i < totalSlots; i++)
        {
            // [新] 檢查「儲存系統」的狀態
            if (!currentPuzzleState.ContainsKey(i) || string.IsNullOrEmpty(currentPuzzleState[i]))
            {
                allFilled = false;
                break;
            }
        }

        // [新] 如果缺少當前 UI 的引用
        if (_activePuzzleUI == null) return;

        if (!allFilled)
        {
            _activePuzzleUI.SetResultMessage("", Color.white); // 尚未填滿，清除訊息
            return;
        }

        // 2. [新] 計算錯誤的數量
        // (我們改用 allCorrect bool，而非 incorrectCount)
        int incorrectCount = 0;
        foreach (var slotDef in puzzle.clueSlots.Select((value, i) => new { i, value }))
        {
            // 既然 allFilled 為 true, currentPuzzleState 必定存在 key
            if (currentPuzzleState[slotDef.i] != slotDef.value.correctClueID)
            {
                incorrectCount++;
            }
        }

        // 3. [新] 根據錯誤數量顯示訊息
        if (incorrectCount == 0)
        {
            // 組合正確
            _activePuzzleUI.SetResultMessage(puzzle.successMessage, Color.green); // 也可以用自訂顏色
            _activePuzzleUI.LockAllSlots();
            _activePuzzleUI.ShowConnectionLine();

            // [!!] 新增：給予獎勵物品 [!!]
            // 僅在組合正確時給予
            GivePuzzleReward(_currentActiveMasterIndex);
        }
        else
        {
            // 組合錯誤
            string message;

            // 檢查是否有在 Inspector 填寫 failureMessages
            if (puzzle.failureMessages == null || puzzle.failureMessages.Count == 0)
            {
                message = "組合不正確"; // 預設的錯誤訊息
            }
            else
            {
                // 1 個錯誤 -> 索引 0
                // 2 個錯誤 -> 索引 1
                int messageIndex = incorrectCount - 1;

                // 防止索引超出範圍 (例如 4 個錯誤，但只設定了 3 條訊息)
                if (messageIndex >= puzzle.failureMessages.Count)
                {
                    // 就使用最後一條可用的錯誤訊息
                    messageIndex = puzzle.failureMessages.Count - 1;
                }

                message = puzzle.failureMessages[messageIndex];
            }

            // 顯示對應的錯誤訊息
            // [新] 並設定錯誤訊息的顏色
            Color failureColor;

            // 檢查錯誤數量是否等於總格子數 (例如 4 錯 / 4 格)
            if (incorrectCount == totalSlots)
            {
                // 全錯
                failureColor = Color.red;
            }
            else
            {
                // 部分錯誤 (例如 1, 2, 3 錯 / 4 格)
                failureColor = Color.yellow;
            }

            // 顯示錯誤訊息與 [新] 顏色
            _activePuzzleUI.SetResultMessage(message, failureColor);
            // --- [!!] 修改結束 [!!] ---
        }
    }

    /// <summary>
    /// [新] 增加：給予此謎題的獎勵
    /// </summary>
    private void GivePuzzleReward(int puzzleIndex)
    {
        if (InventoryManager.Instance == null) return;

        // 檢查是否有設定獎勵
        if (puzzleRewardItems != null && puzzleIndex < puzzleRewardItems.Count)
        {
            ItemData reward = puzzleRewardItems[puzzleIndex];
            if (reward != null)
            {
                // 檢查是否已經擁有 (避免重複給予 Log)
                // InventoryManager.AddItem 內部會處理，但這裡檢查可以避免洗 Debug.Log
                if (!InventoryManager.Instance.HasItem(reward.itemID))
                {
                    Debug.Log($"[CCM] 謎題 {puzzleIndex} 組合正確！獲得獎勵: {reward.itemName}");
                    InventoryManager.Instance.AddItem(reward);
                }
            }
        }
    }

    /// <summary>
    /// [全新] 獲取所有「符合類型」的「線索」 (重構此方法)
    /// </summary>
    private List<IClue> GetEligibleClues(EClueType requiredType)
    {
        // [!!] 偵錯 4 [!!]
        Debug.Log($"[CCM] GetEligibleClues: 開始搜尋 {requiredType} 類型的線索。");

        List<IClue> clues = new List<IClue>();

        switch (requiredType)
        {
            case EClueType.Item:
                // 使用 InventoryManager 單例
                // 【修正】: "items" 列表已不存在，改為呼叫新的 "GetOwnedItemsData()" 方法
                foreach (ItemData item in InventoryManager.Instance.GetOwnedItemsData())
                {
                    // [!!] 只加入 [線索:物品] (isClueItem == true)
                    if (item.isClueItem)
                    {
                        clues.Add(new ItemClueWrapper(item));
                    }
                }
                break;

            case EClueType.Memory:
                // [!!] 這是新系統的關鍵 [!!]
                Debug.Log("[CCM] GetEligibleClues: 進入 Memory 邏輯...");

                if (RolePastManager.Instance == null)
                {
                    Debug.LogError("[CCM] GetEligibleClues: [嚴重錯誤!] RolePastManager.Instance 是 null！");
                    break; // 中斷執行
                }

                if (RolePastManager.Instance.unlockedMemories == null)
                {
                    Debug.LogError("[CCM] GetEligibleClues: [嚴重錯誤!] RolePastManager.Instance.unlockedMemories 竟然是 null！");
                    break; // 中斷執行
                }

                // [!!] 偵錯 5 [!!]
                Debug.Log($"[CCM] GetEligibleClues: RPM.Instance.unlockedMemories 字典中有 {RolePastManager.Instance.unlockedMemories.Count} 個 Role Key。");

                foreach (var entry in RolePastManager.Instance.unlockedMemories)
                {
                    // [!!] 偵錯 6 [!!]
                    Debug.Log($"[CCM] GetEligibleClues: 正在處理 Role '{entry.Key.name}' (ID: {entry.Key.GetInstanceID()})... 該 Role 有 {entry.Value.Count} 條已解鎖記憶。");

                    foreach (CarouselData memory in entry.Value)
                    {
                        if (memory != null)
                        {
                            int index = Array.IndexOf(entry.Key.carousels, memory);
                            if (index > -1)
                            {
                                // [!!] 偵錯 7 [!!]
                                Debug.Log($"[CCM] GetEligibleClues: [成功!] 找到並加入記憶 '{memory.name}' 線索。");
                                clues.Add(new MemoryClueWrapper(entry.Key, memory, index));
                            }
                            else
                            {
                                Debug.LogWarning($"[CCM] GetEligibleClues: 找到記憶 '{memory.name}'，但 Array.IndexOf 在 Role '{entry.Key.name}' 找不到 (返回 -1)！");
                            }
                        }
                    }
                }
                break;

            case EClueType.Sound:
                // 使用 VoiceItemManager 單例
                foreach (VoiceItemData sound in VoiceItemManager.Instance.items)
                {
                    // [!!] 只加入 [已使用過的聲音]
                    if (VoiceItemManager.Instance.IsItemUsed(sound))
                    {
                        clues.Add(new SoundClueWrapper(sound));
                    }
                }
                break;
        }

        // [!!] 偵錯 8 [!!]
        Debug.Log($"[CCM] GetEligibleClues: 搜尋完畢。總共返回 {clues.Count} 條線索。");
        return clues;
    }

    /// <summary>
    /// [新增] 透過 ClueID 找到 IClue (用於讀取存檔)
    /// </summary>
    public IClue GetClueFromID(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return null;

        // 1. 搜尋物品 (ID = itemID)
        // 【修正】: "items" 列表已不存在，改為呼叫新的 "GetOwnedItemsData()" 方法
        ItemData item = InventoryManager.Instance.GetOwnedItemsData().FirstOrDefault(x => x.itemID == clueID);
        if (item != null && item.isClueItem) return new ItemClueWrapper(item);

        // 2. 搜尋聲音 (ID = voiceItemID)
        VoiceItemData sound = VoiceItemManager.Instance.items.FirstOrDefault(x => x.voiceItemID == clueID);
        if (sound != null && VoiceItemManager.Instance.IsItemUsed(sound)) return new SoundClueWrapper(sound);

        // 3. 搜尋記憶 (ID = CarouselData.name, 例如: "R101", "R401")
        // (我們假設所有記憶 ID 都是 'R' 開頭以加速搜尋)
        if (clueID.StartsWith("R"))
        {
            // [!!] 修正 [!!]
            // 必須使用 RolePastManager.Instance
            if (RolePastManager.Instance != null && RolePastManager.Instance.unlockedMemories != null)
            {
                foreach (var entry in RolePastManager.Instance.unlockedMemories)
                {
                    RoleData role = entry.Key;
                    foreach (CarouselData memory in entry.Value)
                    {
                        if (memory != null && memory.name == clueID)
                        {
                            int index = Array.IndexOf(role.carousels, memory);
                            if (index > -1)
                            {
                                return new MemoryClueWrapper(role, memory, index);
                            }
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"[GetClueFromID] 找不到 ID: {clueID} 對應的線索。");
        return null;
    }

    public void NextPuzzle()
    {
        if (_unlockedPuzzleMasterIndices.Count == 0) return;
        int nextUnlockedIndex = (_currentUnlockedListIndex + 1) % _unlockedPuzzleMasterIndices.Count;
        LoadPuzzleByUnlockedIndex(nextUnlockedIndex);
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void PreviousPuzzle()
    {
        if (_unlockedPuzzleMasterIndices.Count == 0) return;
        int prevUnlockedIndex = (_currentUnlockedListIndex - 1 + _unlockedPuzzleMasterIndices.Count) % _unlockedPuzzleMasterIndices.Count;
        LoadPuzzleByUnlockedIndex(prevUnlockedIndex);
        EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// [新] 更新導覽按鈕的顯示 (例如：若只有1個謎題則隱藏按鈕)
    /// </summary>
    private void UpdateNavigationButtons()
    {
        bool showButtons = _unlockedPuzzleMasterIndices.Count > 1;
        if (nextButton != null) nextButton.gameObject.SetActive(showButtons);
        if (prevButton != null) prevButton.gameObject.SetActive(showButtons);
    }

    /// <summary>
    /// [新] 獲取玩家當前擁有的「所有」線索 ID (用於檢查)
    /// </summary>
    private HashSet<string> GetAllPlayerClueIDs()
    {
        HashSet<string> ids = new HashSet<string>();

        // 1. 物品 (Items)
        if (InventoryManager.Instance != null)
        {
            // 【修正】: "items" 列表已不存在，改為呼叫新的 "GetOwnedItemsData()" 方法
            foreach (ItemData item in InventoryManager.Instance.GetOwnedItemsData())
            {
                // 我們假設 isClueItem 的物品才是「線索」
                if (item.isClueItem) ids.Add(item.itemID);
            }
        }

        // 2. 聲音 (Sounds)
        if (VoiceItemManager.Instance != null)
        {
            foreach (VoiceItemData sound in VoiceItemManager.Instance.items)
            {
                if (VoiceItemManager.Instance.IsItemUsed(sound)) ids.Add(sound.voiceItemID);
            }
        }

        // 3. 記憶 (Memories)
        // 必須使用 RolePastManager.Instance
        if (RolePastManager.Instance != null && RolePastManager.Instance.unlockedMemories != null)
        {
            foreach (var entry in RolePastManager.Instance.unlockedMemories)
            {
                foreach (CarouselData memory in entry.Value)
                {
                    if (memory != null) ids.Add(memory.name);
                }
            }
        }

        return ids;
    }

    /// <summary>
    /// [新] 檢查單一謎題是否已滿足「解鎖條件」
    /// </summary>
    private bool CheckIfPuzzleIsReady(ClueCombinationPuzzle puzzle, HashSet<string> playerClueIDs)
    {
        // 檢查玩家是否擁有「所有」正確答案 (的線索)
        foreach (ClueSlotDefinition slot in puzzle.clueSlots)
        {
            if (!playerClueIDs.Contains(slot.correctClueID))
            {
                // 缺少任何一個答案
                return false;
            }
        }
        // 所有答案都齊了
        return true;
    }

    /// <summary>
    /// [新] 核心功能：檢查是否有新的謎題被解鎖
    /// </summary>
    /// <param name="isFirstLoad">是否是遊戲/場景的第一次載入</param>
    public void CheckForNewPuzzleUnlocks(bool isFirstLoad = false, EClueType unlockType = EClueType.Item)
    {
        HashSet<string> playerClueIDs = GetAllPlayerClueIDs();

        // 我們需要一個 bool 來判斷，
        // 稍後是否要播放「新謎題」的動畫
        List<ClueCombinationPuzzle> newlyUnlockedPuzzles = new List<ClueCombinationPuzzle>();

        // 迭代「總清單」，檢查是否可以解鎖
        for (int i = 0; i < allPuzzles.Count; i++)
        {
            // 如果這個索引 i 已經在「已解鎖」列表，就跳過
            if (_unlockedPuzzleMasterIndices.Contains(i)) continue;

            ClueCombinationPuzzle puzzle = allPuzzles[i];

            // 檢查是否滿足解鎖條件
            if (CheckIfPuzzleIsReady(puzzle, playerClueIDs))
            {
                // [!!] 解鎖 [!!]
                _unlockedPuzzleMasterIndices.Add(i);

                // [!!] 將新解鎖的謎題加入列表 [!!]
                // 註： i 只是索引，這裡要傳遞的是 Puzzle 物件
                newlyUnlockedPuzzles.Add(puzzle);
                Debug.Log($"[ClueCombinationManager] 謎題已解鎖: {puzzle.puzzleTitle}");
            }
        }

        // --- 檢查動畫觸發 ---
        // 如果有「新」的謎題
        if (newlyUnlockedPuzzles.Count > 0)
        {
            if (PuzzleUnlockAnimator.Instance == null)
            {
                Debug.LogWarning("[ClueCombinationManager] 有新謎題解鎖，但找不到 PuzzleUnlockAnimator.Instance！");
            }
            else
            {
                // [!!] 核心修改 [!!]
                // 將「新謎題列表」與「觸發類型」傳遞給 PUA 處理
                Debug.Log($"[ClueCombinationManager] 偵測到 {newlyUnlockedPuzzles.Count} 個新謎題，正在提交給 PUA...");
                PuzzleUnlockAnimator.Instance.QueueNewUnlocks(newlyUnlockedPuzzles, unlockType);
            }
        }

        // 更新導覽按鈕的可見度
        UpdateNavigationButtons();

        // 如果是第一次載入，或者這是第一次有謎題被解鎖，就自動顯示第一個可用的謎題
        if ((isFirstLoad || newlyUnlockedPuzzles.Count > 0) && _currentUnlockedListIndex == -1 && _unlockedPuzzleMasterIndices.Count > 0)
        {
            LoadPuzzleByUnlockedIndex(0);
        }
    }
}