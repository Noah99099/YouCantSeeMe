using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;

public class DialogueGraph : EditorWindow
{
    private DialogueGraphView _graphView;
    private DialogueContainerSO _currentDialogueContainer; // 將原本的 _fileName 替換成直接儲存容器物件

    [MenuItem("Graph/Dialogue Graph")]
    public static void Open()
    {
        GetWindow<DialogueGraph>("Dialogue Graph");
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var dialogue = EditorUtility.InstanceIDToObject(instanceID) as DialogueContainerSO;
        if (dialogue != null)
        {
            var window = GetWindow<DialogueGraph>("Dialogue Graph");
            
            // --- 修改點 1: 我們不再直接呼叫 LoadGraph ---
            // 而是將要載入的容器物件暫存起來
            window._currentDialogueContainer = dialogue;
            // 接著讓 OnEnable 自己去處理後續的載入
            
            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        ConstructGraph();
        GenerateToolbar();

        // --- 修改點 2: 在 UI 建構完畢後，檢查是否有待載入的檔案 ---
        if (_currentDialogueContainer != null)
        {
            LoadGraph(_currentDialogueContainer);
        }
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

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();
        
        // --- 修改點 3: 工具列按鈕邏輯更新 ---
        // 儲存按鈕現在會儲存到當前開啟的檔案
        toolbar.Add(new Button(() => SaveData()) { text = "儲存資料" });

        rootVisualElement.Add(toolbar);
    }
    
    private void SaveData()
    {
        // 如果沒有載入任何對話檔，就提示使用者先建立或選取一個
        if (_currentDialogueContainer == null)
        {
            EditorUtility.DisplayDialog("錯誤", "沒有選取任何對話檔案！", "確定");
            return;
        }

        // 將 GraphView 中的資料儲存到當前開啟的 ScriptableObject 中
        _graphView.Save(_currentDialogueContainer);
        
        EditorUtility.SetDirty(_currentDialogueContainer);
        AssetDatabase.SaveAssets();
    }
    
    private void LoadGraph(DialogueContainerSO dialogueContainer)
    {
        _currentDialogueContainer = dialogueContainer;

        // 如果 _graphView 因為某些原因還是 null，確保它被建立
        if (_graphView == null)
        {
            ConstructGraph();
            GenerateToolbar();
        }

        _graphView.ClearGraph();
        _graphView.Load(_currentDialogueContainer);

        // 更新視窗標題以顯示正在編輯的檔案名稱
        titleContent.text = _currentDialogueContainer.name;
    }
}