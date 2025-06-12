using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraph : EditorWindow
{
    // 這個屬性讓 Unity 在頂部選單中新增一個選項
    [MenuItem("Graph/Dialogue Graph")]
    public static void Open()
    {
        // 獲取或創建一個 DialogueGraph 視窗的實例
        GetWindow<DialogueGraph>("Dialogue Graph");
    }

    // 當視窗被啟用時會呼叫此方法
    private void OnEnable()
    {
        // 這裡將是我們建構圖形化介面的地方
        ConstructGraph();
    }

    private void ConstructGraph()
    {
        // 創建一個 GraphView 的實例 (我們下一步就會建立這個腳本)
        var graphView = new DialogueGraphView
        {
            name = "Dialogue Graph" // 給它一個名字
        };
        
        // 讓 GraphView 填滿整個視窗
        graphView.StretchToParentSize();
        
        // 將 GraphView 加入到視窗的根可視化元素中
        rootVisualElement.Add(graphView);
    }
}