using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
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
                              Dictionary<int, string> existingState,
                              ClueCombinationManager manager, // 用於回頭查找 ClueID
                              Action<CombinationSlotUI> onSlotClickedCallback)
    {
        if (puzzle == null) return;
            _manager = manager; // 儲存 manager 引用

        // 1. 清理舊的格子
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        _currentSlots.Clear();

        // 2. 設置新內容
        titleText.text = puzzle.puzzleTitle;
        resultText.text = ""; // 重置結果
        resultText.gameObject.SetActive(false); // 隱藏結果
        connectionLine.gameObject.SetActive(false);

        // 3. 實例化新的格子
        for (int i = 0; i < puzzle.clueSlots.Count; i++)
        {
            ClueSlotDefinition slotDef = puzzle.clueSlots[i];
            GameObject slotGO = Instantiate(combinationSlotPrefab, slotsContainer);
            CombinationSlotUI slotUI = slotGO.GetComponent<CombinationSlotUI>();

            if (slotUI != null)
            {
                // 檢查是否有已填入的線索 (來自存檔)
                IClue existingClue = null;
                if (existingState.ContainsKey(i) && !string.IsNullOrEmpty(existingState[i]))
                {
                    existingClue = manager.GetClueFromID(existingState[i]);
                }

                // --- [!!] 這就是您要求的修正 [!!] ---
                // 呼叫 CombinationSlotUI.cs 中的 Setup() 方法
                slotUI.Setup(slotDef, i, existingClue, onSlotClickedCallback);
                // --- [!!] 修正結束 [!!] ---

                _currentSlots.Add(slotUI);
            }
        }
    }

    public void SetResultMessage(string message, Color color)
    {
        resultText.text = message;
        resultText.color = color;
        resultText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    public void LockAllSlots()
    {
        foreach (var slot in _currentSlots)
        {
            slot.Lock();
        }
    }

    public void ShowConnectionLine()
    {
        connectionLine.SetActive(true);
    }

    
}

