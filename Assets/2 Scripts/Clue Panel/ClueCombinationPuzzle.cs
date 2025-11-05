using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 用於定義「一個」填入格子的數據
/// (因為這個 class 只被 ClueCombinationPuzzle 使用, 放在一起是OK的)
/// </summary>
[System.Serializable]
public class ClueSlotDefinition
{
    public EClueType requiredClueType; // 此格子需要的線索類型 (物品/回憶/聲音)
    public string hintText;            // 提示文本 (例如 "黃色的水果")

    [Header("!!重要!! 正確答案的 ClueID")]
    public string correctClueID;       // 正確答案的 ClueID (必須與 Wrapper 產生的 ID 一致)
}

/// <summary>
/// 組合線索的謎題數據 (使用 ScriptableObject)
/// 你可以在 Unity 編輯器中創建 "水果組合"、"兇案組合" 等多個謎題
/// </summary>
[CreateAssetMenu(fileName = "ClueCombinationPuzzle", menuName = "Clue System/Combination Puzzle")]
public class ClueCombinationPuzzle : ScriptableObject
{
    public string puzzleTitle; // 組合線索頁標題 (例如 "家裡的水果")

    // 這個謎題包含的線索格子 (例如3個，或2個、4個)
    public List<ClueSlotDefinition> clueSlots = new List<ClueSlotDefinition>();

    // 組合正確時的描述文本
    public string successMessage; // (例如 "C吃掉了鳳梨")

    [Header("組合失敗的訊息")]
    [Tooltip("請依順序填入錯誤訊息：\n[0] = 1 個錯誤\n[1] = 2 個錯誤\n[2] = 3 個錯誤...")]
    public List<string> failureMessages = new List<string>();
}
