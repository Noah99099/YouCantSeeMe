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

        inventoryGrid.Hide();
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

        inventoryGrid.Hide();
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

        // 檢查是否所有格子都填滿了
        bool allFilled = true;
        for (int i = 0; i < puzzle.clueSlots.Count; i++)
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

        bool allCorrect = true;
        foreach (var slotDef in puzzle.clueSlots.Select((value, i) => new { i, value }))
        {
            if (!_currentPuzzleState.ContainsKey(slotDef.i) ||
                _currentPuzzleState[slotDef.i] != slotDef.value.correctClueID)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            puzzleUI.SetResultMessage(puzzle.successMessage, Color.green);
            puzzleUI.LockAllSlots();
            puzzleUI.ShowConnectionLine();
        }
        else
        {
            puzzleUI.SetResultMessage(puzzle.failureMessage, Color.yellow);
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

        // 3. 搜尋回憶 (ID = "MEM_角色名_索引")
        if (clueID.StartsWith("MEM_"))
        {
            foreach (RoleData role in playerCollectedRoles)
            {
                for (int i = 0; i < role.carousels.Length; i++)
                {
                    string memID = $"MEM_{role.roleName}_{i + 1}";
                    if (memID == clueID)
                    {
                        return new MemoryClueWrapper(role, role.carousels[i], i);
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

