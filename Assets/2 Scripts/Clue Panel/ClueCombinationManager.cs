using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [已更新] 組合線索功能 總控制器
/// </summary>
public class ClueCombinationManager : MonoBehaviour
{
    [Header("所有謎題")]
    public List<ClueCombinationPuzzle> allPuzzles;
    private int currentPuzzleIndex = 0;

    [Header("玩家持有的回憶")]
    // [!!重要!!] 你需要將玩家獲得的 RoleData 實例 (ScriptableObject) 加入到這個 List
    public List<RoleData> playerCollectedRoles;

    [Header("UI 引用")]
    public CombinationPuzzleUI puzzleUI;     // 右側組合頁
    public InventoryClueGrid inventoryGrid;  // 左側格子
    public ClueDetailsPanel detailsPanel;    // 左下詳細資訊

    // --- 狀態變數 ---
    private CombinationSlotUI _currentSelectedSlot; // 當前點擊的「右側」填入格
    private IClue _currentSelectedClue;             // 當前點擊的「左側」物品

    // 用於保存當前謎題進度 (key: slot索引, value: 填入的ClueID)
    // [!!] 你需要實現儲存/讀取 _currentPuzzleState 的邏輯 (例如 PlayerPrefs)
    private Dictionary<int, string> _currentPuzzleState;

    void Start()
    {
        // 檢查是否引用了 UI
        if (puzzleUI == null || inventoryGrid == null || detailsPanel == null)
        {
            Debug.LogError("[ClueCombinationManager] UI 引用未設置！");
            return;
        }

        // 載入第一個謎題
        LoadPuzzle(currentPuzzleIndex);
    }

    public void LoadPuzzle(int index)
    {
        if (index < 0 || index >= allPuzzles.Count) return;

        currentPuzzleIndex = index;
        ClueCombinationPuzzle puzzle = allPuzzles[index];

        // TODO: 載入這個謎題的 _currentPuzzleState (例如從存檔)
        // 範例： _currentPuzzleState = LoadStateForPuzzle(puzzle.name);
        if (_currentPuzzleState == null)
            _currentPuzzleState = new Dictionary<int, string>();

        // 顯示謎題UI
        // [更新] 傳入 "this" (manager) 讓 UI 可以回頭查找 ClueID
        puzzleUI.DisplayPuzzle(puzzle, _currentPuzzleState, this, OnSlotClicked);

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
        if (slot.IsLocked) return;

        _currentSelectedSlot = slot;
        detailsPanel.Hide();

        // [正常邏輯] 點擊後，用符合條件的線索「重新填充」左側網格
        List<IClue> eligibleClues = GetEligibleClues(slot.RequiredClueType);
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
        _currentPuzzleState[_currentSelectedSlot.SlotIndex] = _currentSelectedClue.ClueID;
        // TODO: SaveStateForPuzzle(allPuzzles[currentPuzzleIndex].name, _currentPuzzleState);

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
        ClueCombinationPuzzle puzzle = allPuzzles[currentPuzzleIndex];
        int totalSlots = puzzle.clueSlots.Count; // [新] 獲取右側格子總數

        // 1. 檢查是否所有格子都填滿了
        bool allFilled = true;
        for (int i = 0; i < totalSlots; i++)
        {
            if (!_currentPuzzleState.ContainsKey(i) || string.IsNullOrEmpty(_currentPuzzleState[i]))
            {
                allFilled = false;
                break;
            }
        }

        if (!allFilled)
        {
            puzzleUI.SetResultMessage("", Color.white); // 尚未填滿，不顯示提示
            return;
        }

        // 2. [新] 計算錯誤的數量
        // (不再使用 allCorrect bool，而是用 incorrectCount)
        int incorrectCount = 0;
        foreach (var slotDef in puzzle.clueSlots.Select((value, i) => new { i, value }))
        {
            // 既然 allFilled 為 true, _currentPuzzleState 必定包含 key
            if (_currentPuzzleState[slotDef.i] != slotDef.value.correctClueID)
            {
                incorrectCount++;
            }
        }

        // 3. [新] 根據錯誤數量顯示訊息
        if (incorrectCount == 0)
        {
            // 組合正確
            puzzleUI.SetResultMessage(puzzle.successMessage, Color.green); // 可以再調顏色
            puzzleUI.LockAllSlots();
            puzzleUI.ShowConnectionLine();
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
            puzzleUI.SetResultMessage(message, failureColor);
            // --- [!!] 修改結束 [!!] ---
        }
    }

    /// <summary>
    /// [已更新] 獲取所有「符合條件」的線索 (核心過濾邏輯)
    /// </summary>
    private List<IClue> GetEligibleClues(EClueType requiredType)
    {
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
                // 使用在 Inspector 中指派的 playerCollectedRoles 列表
                foreach (RoleData role in playerCollectedRoles)
                {
                    for (int i = 0; i < role.carousels.Length; i++)
                    {
                        CarouselData memory = role.carousels[i];
                        // 確保數據有效
                        if (memory != null && memory.images.Length > 0 && memory.texts.Length > 0)
                        {
                            clues.Add(new MemoryClueWrapper(role, memory, i));
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
            foreach (RoleData role in playerCollectedRoles)
            {
                for (int i = 0; i < role.carousels.Length; i++)
                {
                    CarouselData memory = role.carousels[i];

                    // 檢查 CarouselData (ScriptableObject) 的 .name 屬性
                    if (memory != null && memory.name == clueID)
                    {
                        return new MemoryClueWrapper(role, memory, i);
                    }
                }
            }
        }

        Debug.LogWarning($"[GetClueFromID] 找不到 ID: {clueID} 對應的線索。");
        return null;
    }


    public void NextPuzzle()
    {
        LoadPuzzle((currentPuzzleIndex + 1) % allPuzzles.Count);
    }

    public void PreviousPuzzle()
    {
        int prevIndex = (currentPuzzleIndex - 1 + allPuzzles.Count) % allPuzzles.Count;
        LoadPuzzle(prevIndex);
    }
}

