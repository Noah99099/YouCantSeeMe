using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// [已更新] 管理右側「組合線索頁」的 UI
/// </summary>
public class CombinationPuzzleUI : MonoBehaviour
{
    [Header("UI 引用")]
    public TMP_Text titleText;
    public TMP_Text resultText;
    public Transform slotsContainer;
    public GameObject connectionLine;
    public Button nextButton;
    public Button prevButton;

    [Header("Prefabs")]
    public GameObject combinationSlotPrefab;

    private List<CombinationSlotUI> _currentSlots = new List<CombinationSlotUI>();
    private ClueCombinationManager _manager; // [新增] 對 Manager 的引用

    void Awake()
    {
        connectionLine.SetActive(false);
        resultText.text = "";

        _manager = FindObjectOfType<ClueCombinationManager>();
        if (_manager == null)
        {
            Debug.LogError("[CombinationPuzzleUI] 找不到 ClueCombinationManager！");
            return;
        }

        nextButton.onClick.AddListener(_manager.NextPuzzle);
        prevButton.onClick.AddListener(_manager.PreviousPuzzle);
    }

    /// <summary>
    /// [已更新] 顯示一個新的謎題
    /// </summary>
    public void DisplayPuzzle(ClueCombinationPuzzle puzzle,
                              Dictionary<int, string> savedState,
                              ClueCombinationManager manager, // [新增]
                              System.Action<CombinationSlotUI> onSlotClickedCallback)
    {
        _manager = manager; // 儲存 manager 引用

        // 1. 清理舊的格子
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        _currentSlots.Clear();
        connectionLine.SetActive(false);

        // 2. 設置標題
        titleText.text = puzzle.puzzleTitle;
        SetResultMessage("", Color.white);

        // 3. 實例化新的格子
        for (int i = 0; i < puzzle.clueSlots.Count; i++)
        {
            GameObject slotGO = Instantiate(combinationSlotPrefab, slotsContainer);
            CombinationSlotUI slotUI = slotGO.GetComponent<CombinationSlotUI>();

            ClueSlotDefinition slotDef = puzzle.clueSlots[i];
            slotUI.Initialize(slotDef, i, onSlotClickedCallback);

            // [已更新] 檢查是否有保存的狀態
            if (savedState.ContainsKey(i))
            {
                string savedClueID = savedState[i];
                // [已更新] 使用 Manager 的 helper 函數來查找 Clue
                IClue filledClue = _manager.GetClueFromID(savedClueID);
                if (filledClue != null)
                {
                    slotUI.FillSlot(filledClue);
                }
            }

            _currentSlots.Add(slotUI);
        }
    }

    public void SetResultMessage(string message, Color color)
    {
        resultText.text = message;
        resultText.color = color;
        resultText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    public void ShowConnectionLine()
    {
        connectionLine.SetActive(true);
    }

    public void LockAllSlots()
    {
        foreach (var slot in _currentSlots)
        {
            slot.Lock();
        }
    }
}

