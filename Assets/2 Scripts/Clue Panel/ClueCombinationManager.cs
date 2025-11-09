using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// [已更新] 組合線索功能 總控制器
/// </summary>
public class ClueCombinationManager : MonoBehaviour
{
    // [新] Singleton (單例)
    public static ClueCombinationManager Instance { get; private set; }

    [Header("所有謎題")]
    public List<ClueCombinationPuzzle> allPuzzles;
    //private int currentPuzzleIndex = 0;

    //[Header("玩家持有的回憶")]
    //// [!!重要!!] 你需要將玩家獲得的 RoleData 實例 (ScriptableObject) 加入到這個 List
    //public List<RoleData> playerCollectedRoles;

    //[Header("管理器引用")]
    //// [!!] 新增 [!!] 您可以在 Inspector 中拖入，或者讓它自動尋找
    //public RolePastManager rolePastManager;

    [Header("UI 引用")]
    public InventoryClueGrid inventoryGrid;  // 左側格子
    public ClueDetailsPanel detailsPanel;    // 左下詳細資訊

    [Header("導航按鈕")]
    public Button nextButton;
    public Button prevButton;

    [Header("Puzzle UI Prefab (您的新架構)")]
    public GameObject puzzleContainerPrefab; // [!!] 拖入您的 PuzzleContainer_Prefab
    public Transform puzzleContainerParent;  // [!!] 拖入您場景中的 "PuzzlePanel" (容器)

    // --- 狀態變數 ---
    private CombinationSlotUI _currentSelectedSlot; // 當前點擊的「右側」填入格
    private IClue _currentSelectedClue;             // 當前點擊的「左側」物品

    // 用於保存當前謎題進度 (key: slot索引, value: 填入的ClueID)
    // Key: 謎題的 .name (或 ID), Value: 該謎題的狀態 (Key: slot索引, Value: ClueID)
    private Dictionary<string, Dictionary<int, string>> _allPuzzleStates;

    // [新] 儲存「所有」謎題的 UI 實例
    private List<PuzzleContainerUI> _instantiatedPuzzles = new List<PuzzleContainerUI>();
    private PuzzleContainerUI _activePuzzleUI; // [新] 對當前活動 UI 的引用

    // [新] 儲存「已解鎖」謎題在 allPuzzles 中的「索引」
    // (例如 [1, 0] 表示 test2 (索引1) 和 test1 (索引0) 被解鎖了)
    private List<int> _unlockedPuzzleMasterIndices = new List<int>();

    // [新] 當前顯示的謎題在 _unlockedPuzzleMasterIndices 列表中的索引
    // (例如 0, 1)
    private int _currentUnlockedListIndex = -1;

    // [新] 當前顯示的謎題在 allPuzzles 總表中的索引
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
            Debug.LogError("[ClueCombinationManager] UI 引用未設置！");
            return;
        }

        // [新] 初始化所有謎題的狀態儲存
        _allPuzzleStates = new Dictionary<string, Dictionary<int, string>>();
        // 在遊戲開始時，預先生成所有的 Puzzle UI
        _instantiatedPuzzles.Clear();
        foreach (var puzzle in allPuzzles)
        {
            // 1. 初始化數據字典
            // TODO: 這裡未來可以替換為「從存檔讀取」
            _allPuzzleStates[puzzle.name] = new Dictionary<int, string>();
            Dictionary<int, string> puzzleState = _allPuzzleStates[puzzle.name];

            // 2. 生成 UI Prefab
            GameObject go = Instantiate(puzzleContainerPrefab, puzzleContainerParent);
            PuzzleContainerUI ui = go.GetComponent<PuzzleContainerUI>();

            if (ui != null)
            {
                // 3. 初始化這個 UI 頁面 (只執行一次)
                // (注意：SetupPuzzle 是舊的 DisplayPuzzle)
                ui.SetupPuzzle(puzzle, puzzleState, this, OnSlotClicked);

                // 4. 預設隱藏
                go.SetActive(false);
                _instantiatedPuzzles.Add(ui);
            }
        }

        // [新] 由 Manager 直接綁定導航按鈕
        if (nextButton != null) nextButton.onClick.AddListener(NextPuzzle);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousPuzzle);

        // 3. [新] 遊戲一開始時，檢查解鎖狀態 (例如讀取存檔後)
        _unlockedPuzzleMasterIndices.Clear();
        CheckForNewPuzzleUnlocks(true); // true = 這是第一次載入
    }

    /// <summary>
    /// 根據「已解鎖列表的索引」來載入謎題
    /// </summary>
    public void LoadPuzzleByUnlockedIndex(int unlockedListIndex)
    {
        // 檢查是否有任何已解鎖的謎題
        if (_unlockedPuzzleMasterIndices.Count == 0 || unlockedListIndex < 0 || unlockedListIndex >= _unlockedPuzzleMasterIndices.Count)
        {
            // [新] 沒有謎題可顯示，隱藏所有內容
            foreach (var ui in _instantiatedPuzzles) ui.gameObject.SetActive(false);
            _activePuzzleUI = null;
            _currentUnlockedListIndex = -1;
            _currentActiveMasterIndex = -1;

            // 隱藏左側面板 (或顯示提示)
            inventoryGrid.Hide(); // 假設有 Hide()
            detailsPanel.Hide();
            return;
        }

        _currentUnlockedListIndex = unlockedListIndex;
        // [新] 獲取謎題的「總表索引」
        _currentActiveMasterIndex = _unlockedPuzzleMasterIndices[_currentUnlockedListIndex];

        // 循環所有 UI 實例，只激活當前的
        for (int i = 0; i < _instantiatedPuzzles.Count; i++)
        {
            bool isActive = (i == _currentActiveMasterIndex);
            _instantiatedPuzzles[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                // [新] 保存對當前活動 UI 的引用
                _activePuzzleUI = _instantiatedPuzzles[i];
            }
        }

        // 顯示一個「空」的左側網格
        // (InventoryClueGrid.cs 的 Show 邏輯會處理 clues 列表為空的情況，只顯示面板，不顯示格子)
        inventoryGrid.Show(new List<IClue>(), EClueType.Item, OnGridItemClicked);
        // 隱藏詳細資訊面板
        detailsPanel.Hide();
    }

    /// <summary>
    /// (Callback) 當玩家點擊「右側」的填入格子時
    /// </summary>
    public void OnSlotClicked(CombinationSlotUI slot)
    {
        // [!!] 偵錯 1 [!!]
        Debug.Log($"[CCM] OnSlotClicked: 點擊了格子 {slot.SlotIndex}。格子需要的類型是: {slot.RequiredClueType}");

        if (slot.IsLocked) return;

        _currentSelectedSlot = slot;
        detailsPanel.Hide();

        // [!!] 偵錯 2 [!!]
        Debug.Log("[CCM] OnSlotClicked: 正在呼叫 GetEligibleClues...");
        // [正常邏輯] 點擊後，用符合條件的線索「重新填充」左側網格
        List<IClue> eligibleClues = GetEligibleClues(slot.RequiredClueType);

        // [!!] 偵錯 3 [!!]
        Debug.Log($"[CCM] OnSlotClicked: GetEligibleClues 執行完畢。獲取到 {eligibleClues.Count} 個線索。正在呼叫 inventoryGrid.Show...");
        inventoryGrid.Show(eligibleClues, slot.RequiredClueType, OnGridItemClicked);
    }

    /// <summary>
    /// (Callback) 當玩家點擊「左側」格子中的線索時
    /// </summary>
    public void OnGridItemClicked(IClue clue)
    {
        _currentSelectedClue = clue;
        detailsPanel.Show(clue.ClueName, clue.ClueDescription, OnUseItemClicked);
    }

    /// <summary>
    /// (Callback) 當玩家點擊「使用物品」按鈕時
    /// </summary>
    public void OnUseItemClicked()
    {
        if (_currentSelectedSlot == null || _currentSelectedClue == null) return;

        _currentSelectedSlot.FillSlot(_currentSelectedClue);

        // [修改] 必須使用 _currentActiveMasterIndex
        if (_currentActiveMasterIndex == -1) return; // 安全檢查

        // 獲取當前謎題和其專屬狀態
        ClueCombinationPuzzle puzzle = allPuzzles[_currentActiveMasterIndex];
        Dictionary<int, string> currentPuzzleState = _allPuzzleStates[puzzle.name];

        // 將狀態寫入「正確的」字典中
        currentPuzzleState[_currentSelectedSlot.SlotIndex] = _currentSelectedClue.ClueID;
        // TODO: SaveStateForPuzzle(puzzle.name, currentPuzzleState);

        // [!!] 修正 #3 (補充)
        // 填入物品後，左側網格恢復為「空」狀態，而不是隱藏
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
        // [修改] 必須使用 _currentActiveMasterIndex
        if (_currentActiveMasterIndex == -1) return; // 安全檢查

        ClueCombinationPuzzle puzzle = allPuzzles[_currentActiveMasterIndex];
        // [新] 獲取「正確的」狀態字典
        Dictionary<int, string> currentPuzzleState = _allPuzzleStates[puzzle.name];
        int totalSlots = puzzle.clueSlots.Count; // [新] 獲取右側格子總數

        // 1. 檢查是否所有格子都填滿了
        bool allFilled = true;
        for (int i = 0; i < totalSlots; i++)
        {
            // [新] 檢查「正確的」狀態字典
            if (!currentPuzzleState.ContainsKey(i) || string.IsNullOrEmpty(currentPuzzleState[i]))
            {
                allFilled = false;
                break;
            }
        }

        // [新] 確保我們有活動的 UI 引用
        if (_activePuzzleUI == null) return;

        if (!allFilled)
        {
            _activePuzzleUI.SetResultMessage("", Color.white); // 尚未填滿，不顯示提示
            return;
        }

        // 2. [新] 計算錯誤的數量
        // (不再使用 allCorrect bool，而是用 incorrectCount)
        int incorrectCount = 0;
        foreach (var slotDef in puzzle.clueSlots.Select((value, i) => new { i, value }))
        {
            // 既然 allFilled 為 true, currentPuzzleState 必定包含 key
            if (currentPuzzleState[slotDef.i] != slotDef.value.correctClueID)
            {
                incorrectCount++;
            }
        }

        // 3. [新] 根據錯誤數量顯示訊息
        if (incorrectCount == 0)
        {
            // 組合正確
            _activePuzzleUI.SetResultMessage(puzzle.successMessage, Color.green); // 可以再調顏色
            _activePuzzleUI.LockAllSlots();
            _activePuzzleUI.ShowConnectionLine();
        }
        else
        {
            // 組合錯誤
            string message;

            // 檢查使用者是否有在 Inspector 中填入 failureMessages
            if (puzzle.failureMessages == null || puzzle.failureMessages.Count == 0)
            {
                message = "組合不正確"; // 預設的備用訊息
            }
            else
            {
                // 1 個錯誤 -> 索引 0
                // 2 個錯誤 -> 索引 1
                int messageIndex = incorrectCount - 1;

                // 防止索引超出範圍 (例如 4 個全錯，但只定義了 3 條訊息)
                if (messageIndex >= puzzle.failureMessages.Count)
                {
                    // 就使用最後一條可用的錯誤訊息
                    messageIndex = puzzle.failureMessages.Count - 1;
                }

                message = puzzle.failureMessages[messageIndex];
            }

            // 顯示對應的錯誤訊息
            // [新] 決定錯誤訊息的顏色
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

            // 顯示對應的錯誤訊息和 [新] 顏色
            _activePuzzleUI.SetResultMessage(message, failureColor);
            // --- [!!] 修改結束 [!!] ---
        }
    }

    /// <summary>
    /// [已更新] 獲取所有「符合條件」的線索 (核心過濾邏輯)
    /// </summary>
    private List<IClue> GetEligibleClues(EClueType requiredType)
    {
        // [!!] 偵錯 4 [!!]
        Debug.Log($"[CCM] GetEligibleClues: 開始尋找類型 {requiredType} 的線索。");

        List<IClue> clues = new List<IClue>();

        switch (requiredType)
        {
            case EClueType.Item:
                // 使用 InventoryManager 單例
                foreach (ItemData item in InventoryManager.Instance.items)
                {
                    // [!!] 只添加 [物品:案件] (isClueItem == true)
                    if (item.isClueItem)
                    {
                        clues.Add(new ItemClueWrapper(item));
                    }
                }
                break;

            case EClueType.Memory:
                // [!!] 這是最關鍵的偵錯 [!!]
                Debug.Log("[CCM] GetEligibleClues: 進入 Memory 區塊...");

                if (RolePastManager.Instance == null)
                {
                    Debug.LogError("[CCM] GetEligibleClues: [致命錯誤!] RolePastManager.Instance 是 null！");
                    break; // 立刻跳出
                }

                if (RolePastManager.Instance.unlockedMemories == null)
                {
                    Debug.LogError("[CCM] GetEligIBLEClues: [致命錯誤!] RolePastManager.Instance.unlockedMemories 字典是 null！");
                    break; // 立刻跳出
                }

                // [!!] 偵錯 5 [!!]
                Debug.Log($"[CCM] GetEligibleClues: RPM.Instance.unlockedMemories 字典中有 {RolePastManager.Instance.unlockedMemories.Count} 個 Role Key。");

                foreach (var entry in RolePastManager.Instance.unlockedMemories)
                {
                    // [!!] 偵錯 6 [!!]
                    Debug.Log($"[CCM] GetEligibleClues: 正在遍歷 Role '{entry.Key.name}' (ID: {entry.Key.GetInstanceID()})... 該 Role 有 {entry.Value.Count} 個已解鎖回憶。");

                    foreach (CarouselData memory in entry.Value)
                    {
                        if (memory != null)
                        {
                            int index = Array.IndexOf(entry.Key.carousels, memory);
                            if (index > -1)
                            {
                                // [!!] 偵錯 7 [!!]
                                Debug.Log($"[CCM] GetEligibleClues: [成功!] 找到並添加回憶 '{memory.name}' 到列表。");
                                clues.Add(new MemoryClueWrapper(entry.Key, memory, index));
                            }
                            else
                            {
                                Debug.LogWarning($"[CCM] GetEligibleClues: 找到回憶 '{memory.name}'，但 Array.IndexOf 在 Role '{entry.Key.name}' 中失敗 (返回 -1)！");
                            }
                        }
                    }
                }
                break;

            case EClueType.Sound:
                // 使用 VoiceItemManager 單例
                foreach (VoiceItemData sound in VoiceItemManager.Instance.items)
                {
                    // [!!] 只添加 [已使用成功] 的聲音
                    if (VoiceItemManager.Instance.IsItemUsed(sound))
                    {
                        clues.Add(new SoundClueWrapper(sound));
                    }
                }
                break;
        }

        // [!!] 偵錯 8 [!!]
        Debug.Log($"[CCM] GetEligibleClues: 尋找完畢。總共返回 {clues.Count} 個線索。");
        return clues;
    }

    /// <summary>
    /// [新增] 根據 ClueID 查找 IClue (用於讀取存檔)
    /// </summary>
    public IClue GetClueFromID(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return null;

        // 1. 搜尋物品 (ID = itemID)
        ItemData item = InventoryManager.Instance.items.FirstOrDefault(x => x.itemID == clueID);
        if (item != null && item.isClueItem) return new ItemClueWrapper(item);

        // 2. 搜尋聲音 (ID = voiceItemID)
        VoiceItemData sound = VoiceItemManager.Instance.items.FirstOrDefault(x => x.voiceItemID == clueID);
        if (sound != null && VoiceItemManager.Instance.IsItemUsed(sound)) return new SoundClueWrapper(sound);

        // 3. 搜尋回憶 (ID = CarouselData.name, 例如: "R101", "R401")
        // (我們假設所有回憶 ID 都以 'R' 開頭作為快速過濾)
        if (clueID.StartsWith("R"))
        {
            // [!!] 修改 [!!]
            // 直接使用 RolePastManager.Instance
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
    /// [新] 更新導航按鈕的顯示 (例如：只有1個謎題時隱藏按鈕)
    /// </summary>
    private void UpdateNavigationButtons()
    {
        bool showButtons = _unlockedPuzzleMasterIndices.Count > 1;
        if (nextButton != null) nextButton.gameObject.SetActive(showButtons);
        if (prevButton != null) prevButton.gameObject.SetActive(showButtons);
    }

    /// <summary>
    /// [新] 獲取玩家當前持有的「所有」線索 ID (用於檢查)
    /// </summary>
    private HashSet<string> GetAllPlayerClueIDs()
    {
        HashSet<string> ids = new HashSet<string>();

        // 1. 物品 (Items)
        if (InventoryManager.Instance != null)
        {
            foreach (ItemData item in InventoryManager.Instance.items)
            {
                // 我們假設所有 isClueItem 的物品都是「原料」
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

        // 3. 回憶 (Memories)
        // 直接使用 RolePastManager.Instance
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
    /// [新] 檢查特定謎題是否滿足解鎖條件
    /// </summary>
    private bool CheckIfPuzzleIsReady(ClueCombinationPuzzle puzzle, HashSet<string> playerClueIDs)
    {
        // 檢查玩家是否擁有「所有」正確答案 (原料)
        foreach (ClueSlotDefinition slot in puzzle.clueSlots)
        {
            if (!playerClueIDs.Contains(slot.correctClueID))
            {
                // 缺少任何一個原料
                return false;
            }
        }
        // 所有原料都齊了
        return true;
    }

    /// <summary>
    /// [新] 核心函式：檢查並解鎖新的謎題
    /// </summary>
    /// <param name="isFirstLoad">是否為遊戲/場景第一次載入</param>
    public void CheckForNewPuzzleUnlocks(bool isFirstLoad = false, EClueType unlockType = EClueType.Item)
    {
        HashSet<string> playerClueIDs = GetAllPlayerClueIDs();

        // 我們不再用 bool aNewPuzzleWasUnlocked，
        // 而是建立一個「新解鎖列表」
        List<ClueCombinationPuzzle> newlyUnlockedPuzzles = new List<ClueCombinationPuzzle>();

        // 遍歷「總表」，檢查是否有新解鎖
        for (int i = 0; i < allPuzzles.Count; i++)
        {
            // 如果這個索引 i 已經在「已解鎖表」中，跳過
            if (_unlockedPuzzleMasterIndices.Contains(i)) continue;

            ClueCombinationPuzzle puzzle = allPuzzles[i];

            // 檢查是否滿足解鎖條件
            if (CheckIfPuzzleIsReady(puzzle, playerClueIDs))
            {
                // [!!] 解鎖 [!!]
                _unlockedPuzzleMasterIndices.Add(i);

                // [!!] 將新解鎖的謎題加入列表 [!!]
                // 因為 i 總是從小到大，這自動保證了順序
                newlyUnlockedPuzzles.Add(puzzle);
                Debug.Log($"[ClueCombinationManager] 謎題已解鎖: {puzzle.puzzleTitle}");
            }
        }

        // --- 檢查動畫觸發 ---
        // 如果列表「不」為空
        if (newlyUnlockedPuzzles.Count > 0)
        {
            if (PuzzleUnlockAnimator.Instance == null)
            {
                Debug.LogWarning("[ClueCombinationManager] 偵測到新謎題，但找不到 PuzzleUnlockAnimator.Instance！");
            }
            else
            {
                // [!!] 核心修改 [!!]
                // 將「整個列表」和「觸發類型」交給 PUA 處理
                Debug.Log($"[ClueCombinationManager] 發現 {newlyUnlockedPuzzles.Count} 個新謎題，正在傳送給 PUA...");
                PuzzleUnlockAnimator.Instance.QueueNewUnlocks(newlyUnlockedPuzzles, unlockType);
            }
        }

        // 更新導航按鈕的可見性
        UpdateNavigationButtons();

        // 如果這是第一次載入，或這是遊戲中第一個被解鎖的謎題
        if ((isFirstLoad || newlyUnlockedPuzzles.Count > 0) && _currentUnlockedListIndex == -1 && _unlockedPuzzleMasterIndices.Count > 0)
        {
            LoadPuzzleByUnlockedIndex(0);
        }
    }
}

