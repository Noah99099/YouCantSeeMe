using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks; // 需要引用這個才能使用 OnOpenAsset
using UnityEditor.UIElements;//引用才可以使用toolbar


public class DialogueGraph : EditorWindow
{
    private DialogueGraphView _graphView;
    private string _fileName = "新的對話";

    // 這個屬性讓 Unity 在頂部選單中新增一個選項
    [MenuItem("Graph/Dialogue Graph")]
    public static void Open()
    {
        GetWindow<DialogueGraph>("Dialogue Graph");
    }

    // --- 新增: OnOpenAsset 回呼 ---
    // 當你在專案中雙擊一個 DialogueContainerSO 類型的資源時，這個方法會被自動呼叫
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        // 根據 instanceID 獲取被雙擊的物件
        var dialogue = EditorUtility.InstanceIDToObject(instanceID) as DialogueContainerSO;
        if (dialogue != null)
        {
            // 如果物件是我們想要的類型，就打開我們的編輯器視窗
            var window = GetWindow<DialogueGraph>("Dialogue Graph");
            // 並將這個對話資源傳遞給視窗
            window.LoadGraph(dialogue);
            return true; // 返回 true 表示我們已經處理了這個雙擊事件
        }
        return false; // 返回 false 表示讓 Unity 用預設方式處理
    }

    private void OnEnable()
    {
        ConstructGraph();
        GenerateToolbar();
    }

    private void ConstructGraph()
    {
        _graphView = new DialogueGraphView
        {
            name = "Dialogue Graph"
        };
        
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    // --- 新增: 產生工具列 ---
    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        // 檔案名稱輸入框
        var fileNameTextField = new TextField("檔案名稱:");
        fileNameTextField.SetValueWithoutNotify(_fileName);
        fileNameTextField.MarkDirtyRepaint();
        fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
        toolbar.Add(fileNameTextField);
        
        // 儲存按鈕
        toolbar.Add(new Button(() => SaveData()) { text = "儲存資料" });
        
        // 讀取按鈕 (未來可擴充)
        // toolbar.Add(new Button(() => LoadData()) { text = "讀取資料" });

        rootVisualElement.Add(toolbar);
    }
    
    // --- 新增: 儲存資料的方法 ---
    private void SaveData()
    {
        // 讓使用者選擇儲存路徑和檔案名稱
        string path = EditorUtility.SaveFilePanelInProject("儲存對話", _fileName, "asset", "請輸入儲存的檔案名稱");

        if (string.IsNullOrEmpty(path)) return; // 如果使用者取消，就不執行任何事

        // 嘗試載入該路徑的現有資源，如果不存在就建立一個新的
        var dialogueContainer = AssetDatabase.LoadAssetAtPath<DialogueContainerSO>(path);
        if (dialogueContainer == null)
        {
            dialogueContainer = CreateInstance<DialogueContainerSO>();
            AssetDatabase.CreateAsset(dialogueContainer, path);
        }

        // 將 GraphView 中的資料儲存到 ScriptableObject 中
        _graphView.Save(dialogueContainer);
        
        // 標記資源為 "Dirty" (已修改)，這樣 Unity 才會知道要儲存變更
        EditorUtility.SetDirty(dialogueContainer);
        // 儲存所有變更的資源到磁碟
        AssetDatabase.SaveAssets();
    }
    
    // --- 新增: 載入資料的方法 ---
    private void LoadGraph(DialogueContainerSO dialogueContainer)
    {
        // 清空現有圖表
        _graphView.ClearGraph();
        // 從 ScriptableObject 載入資料來產生圖表
        _graphView.Load(dialogueContainer);
        // 更新檔案名稱
        _fileName = dialogueContainer.name;
        rootVisualElement.Q<TextField>("檔案名稱:").SetValueWithoutNotify(_fileName);
    }
}