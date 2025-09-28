using UnityEngine;
using System.Collections.Generic;
using System.IO; // 用於檔案讀寫
using System.Linq; // 用於 Linq 查詢

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [Header("要管理的對話圖形")]
    [Tooltip("將所有需要存檔變數的 DialogueGraph Asset 拖到這裡")]
    public List<DialogueGraph> graphsToManage;

    private string savePath;
    private Dictionary<string, DialogueGraph> managedGraphsDict;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "dialogueState.json");
            
            // 將列表轉換為字典，方便快速查找
            managedGraphsDict = new Dictionary<string, DialogueGraph>();
            foreach(var graph in graphsToManage)
            {
                #if UNITY_EDITOR
                string guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(graph));
                if (!managedGraphsDict.ContainsKey(guid))
                {
                    managedGraphsDict.Add(guid, graph);
                }
                #else
                if (!managedGraphsDict.ContainsKey(graph.name))
                {
                    managedGraphsDict.Add(graph.name, graph);
                }
                #endif
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Save()
    {
        Debug.Log("Saving dialogue state to: " + savePath);
        
        AllDialoguesState allStates = new AllDialoguesState { graphStates = new List<DialogueGraphState>() };

        // 從所有管理的圖形中獲取狀態
        foreach (var graph in graphsToManage)
        {
            allStates.graphStates.Add(graph.GetState());
        }

        // 將狀態物件轉換為 JSON 字串
        string json = JsonUtility.ToJson(allStates, true);
        
        // 寫入檔案
        File.WriteAllText(savePath, json);
        Debug.Log("Save successful!");
    }

    public void Load()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("No save file found.");
            return;
        }

        Debug.Log("Loading dialogue state from: " + savePath);
        
        // 從檔案讀取 JSON 字串
        string json = File.ReadAllText(savePath);
        
        // 將 JSON 轉換回狀態物件
        AllDialoguesState allStates = JsonUtility.FromJson<AllDialoguesState>(json);

        // 將讀取的狀態應用到對應的圖形上
        foreach (var state in allStates.graphStates)
        {
            if (managedGraphsDict.ContainsKey(state.graphGUID))
            {
                managedGraphsDict[state.graphGUID].ApplyState(state);
            }
        }
        Debug.Log("Load successful!");
    }

    // --- 用於測試的簡單 UI ---
    /*private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 30), "Save"))
        {
            Save();
        }
        if (GUI.Button(new Rect(10, 50, 100, 30), "Load"))
        {
            Load();
        }
    }
    */
}