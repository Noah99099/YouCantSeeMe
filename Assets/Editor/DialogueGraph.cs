using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;

public class DialogueGraph : EditorWindow
{
    private DialogueGraphView _graphView;
    private DialogueContainerSO _currentDialogueContainer;

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
            // --- 核心修改 ---
            // 1. 取得視窗。這會自動觸發一次 OnEnable，建立一個空白的 UI 介面。
            var window = GetWindow<DialogueGraph>("Dialogue Graph");

            // 2. 在視窗準備好之後，我們再手動命令它載入我們的資料。
            window.PopulateView(dialogue);

            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        // OnEnable 的唯一職責：建立 UI 的「殼」。
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

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        var clearButton = new Button(() => {
            if (_currentDialogueContainer != null)
            {
                _currentDialogueContainer.Blocks.Clear();
                EditorUtility.SetDirty(_currentDialogueContainer);
                AssetDatabase.SaveAssets();
                Debug.Log($"已強制清除 '{_currentDialogueContainer.name}' 的所有資料！");
                
                // 【修正】在清除後，呼叫 PopulateView 來重新生成 Entry Block
                PopulateView(_currentDialogueContainer); 
            }
        }) { text = "強制清除資料" };
        toolbar.Add(clearButton);

        toolbar.Add(new Button(() => SaveData()) { text = "儲存資料" });
        toolbar.Add(new Button(() => _graphView.FrameAll()) { text = "置中全部節點" });
        rootVisualElement.Add(toolbar);
    }

    private void SaveData()
    {
        if (_currentDialogueContainer == null)
        {
            EditorUtility.DisplayDialog("錯誤", "沒有選取任何對話檔案！請先在專案中建立或雙擊一個對話檔。", "確定");
            return;
        }
        _graphView.Save(_currentDialogueContainer);

        // 【關鍵修正】在儲存資產後，強制 Unity 刷新資產資料庫
        // 這會確保 Play Mode 使用的是我們剛剛存檔的最新版本，而不是記憶體中的舊快取
        AssetDatabase.Refresh(); 
    }

    // 這個方法現在變成了公開的，以便 OnOpenAsset 可以呼叫它
    public void PopulateView(DialogueContainerSO dialogueContainer)
    {
        // 儲存對當前對話檔的引用
        _currentDialogueContainer = dialogueContainer;

        // 呼叫 DialogueGraphView 的 LoadGraph 方法來載入資料
        _graphView.PopulateView(dialogueContainer);
    }

}