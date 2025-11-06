using UnityEngine;

/// <summary>
/// 【自動化版本】
/// 此腳本在遊戲啟動 (Awake) 時，
/// "自動" 載入所有在 "Resources" 資料夾 (及其子資料夾) 中的
/// DialogueGraph 和 GlobalVariableDatabase 資產，並將它們重置。
/// </summary>
public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("--- GameInitializer 正在(自動)執行 ---");
        
        // 1. 【自動】載入所有 "Resources" 資料夾下的 DialogueGraph
        //    並重置它們。
        DialogueGraph[] allGraphs = Resources.LoadAll<DialogueGraph>(""); 
        if (allGraphs.Length > 0)
        {
            Debug.Log($"重置 {allGraphs.Length} 個對話圖形...");
            foreach (var graph in allGraphs)
            {
                graph.ResetVariables();
            }
        }

        // 2. 【自動】載入所有 "Resources" 資料夾下的 GlobalVariableDatabase
        //    並重置它們。
        GlobalVariableDatabase[] allDatabases = Resources.LoadAll<GlobalVariableDatabase>("");
        if (allDatabases.Length > 0)
        {
            Debug.Log($"重置 {allDatabases.Length} 個全域資料庫...");
            foreach (var database in allDatabases)
            {
                database.ResetVariables();
            }
        }
        
        Debug.Log("--- 遊戲變數重置完畢 ---");
    }
}