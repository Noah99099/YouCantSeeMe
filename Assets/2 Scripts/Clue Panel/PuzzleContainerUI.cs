using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// [已更新] 管理右側「組合線索頁」的 UI
/// </summary>
public class PuzzleContainerUI : MonoBehaviour
{
    [Header("UI 引用")]
    public TMP_Text titleText;
    public TMP_Text resultText;
    public Transform slotsContainer;
    public GameObject connectionLine;

    [Header("右側格子Prefab")]
    public GameObject combinationSlotPrefab;

    private List<CombinationSlotUI> _currentSlots = new List<CombinationSlotUI>();

    /// <summary>
    /// [已更新] 顯示一個新的謎題
    /// </summary>
    public void SetupPuzzle(ClueCombinationPuzzle puzzle,
                              Dictionary<int, string> puzzleSpecificState,
                              ClueCombinationManager manager, // 用於回頭查找 ClueID
                              Action<CombinationSlotUI> onSlotClickedCallback)
    {
        if (puzzle == null) return;

        // 1. 清理舊的格子
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
                if (puzzleSpecificState.ContainsKey(i) && !string.IsNullOrEmpty(puzzleSpecificState[i]))
                {
                    // [重要] 仍需 manager 來執行一次性的ID查找
                    existingClue = manager.GetClueFromID(puzzleSpecificState[i]);
                }

                // 呼叫 CombinationSlotUI.cs 中的 Setup() 方法
                slotUI.Setup(slotDef, i, existingClue, onSlotClickedCallback);
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

    /// <summary>
    /// 鎖定所有格子 (在組合成功時)
    /// </summary>
    public void LockAllSlots()
    {
        foreach (var slot in _currentSlots)
        {
            slot.Lock();
        }
    }

    /// <summary>
    /// 顯示連接線 (在組合成功時)
    /// </summary>
    public void ShowConnectionLine()
    {
        //connectionLine.SetActive(true);
        connectionLine.SetActive(false); //目前不用連線所以關掉
    }

    
}

