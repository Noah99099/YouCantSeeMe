using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(GridLayoutGroup))] // 確保物件上一定有 GridLayoutGroup
public class InventoryGridEditor : MonoBehaviour
{
    [Header("背包格子設置")]
    [SerializeField] private GameObject slotPrefab; // 格子預製體
    [SerializeField, Range(0, 500)] private int slotCount = 40; // 格子數量
    [SerializeField] private int columns = 8; // 每橫行數
    [SerializeField] private float spacing = 10f; // 間距

    // 移除 contentRect，假設此腳本就在 Content 物件上
    private RectTransform gridRect;
    private GridLayoutGroup gridLayout;

    // 不再需要 lastSlotCount，OnValidate 能處理所有變更
    
    // Awake 僅在 Play Mode 中執行一次初始化，確保執行時狀態正確
    void Awake()
    {
        if (Application.isPlaying)
        {
            // 遊戲執行時，以 Inspector 的設定為最終依據，強制更新一次
            UpdateGrid();
        }
    }

    // OnValidate 是處理編輯器中數值變更最理想的地方
    void OnValidate()
    {
        // OnValidate 會在 Awake 前執行，所以用 EditorApplication.delayCall確保組件獲取完成後再更新
        // 這也避免了在 Prefab 編輯模式下的一些問題
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && gameObject != null) // 確保物件未被銷毀
            {
                UpdateGrid();
            }
        };
        #endif
    }

    // 公開方法，供外部或按鈕調用
    public void UpdateGrid()
    {
        // --- 1. 初始化與獲取組件 ---
        if (gridRect == null) gridRect = GetComponent<RectTransform>();
        if (gridLayout == null) gridLayout = GetComponent<GridLayoutGroup>();
        
        if (slotPrefab == null)
        {
            // 如果 slotPrefab 為空，則不執行任何操作，避免報錯
            // 在 Custom Inspector 中可以添加提示
            return;
        }

        // --- 2. 設置網格佈局 ---
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;

        SetupGridAnchors();

        // --- 3. 調整格子數量 ---
        int currentChildCount = transform.childCount;
        int difference = slotCount - currentChildCount;

        if (difference > 0) // 需要增加格子
        {
            for (int i = 0; i < difference; i++)
            {
                GameObject slot = Instantiate(slotPrefab, transform);
                slot.name = $"{slotPrefab.name}_{currentChildCount + i}";
            }
        }
        else if (difference < 0) // 需要刪除格子
        {
            for (int i = 0; i < -difference; i++)
            {
                // 在編輯模式下使用 DestroyImmediate，在執行模式下使用 Destroy
                GameObject toDestroy = transform.GetChild(transform.childCount - 1).gameObject;
                #if UNITY_EDITOR
                    if(!Application.isPlaying)
                        DestroyImmediate(toDestroy);
                    else
                        Destroy(toDestroy);
                #else
                    Destroy(toDestroy);
                #endif
            }
        }
        
        // --- 4. 更新 UI 佈局 ---
        // 在下一幀更新佈局，確保 ContentSizeFitter (如果有) 能正確計算大小
        LayoutRebuilder.MarkLayoutForRebuild(gridRect);
        ResetScrollPosition();
    }
    
    private void SetupGridAnchors()
    {
        // 假設這個腳本掛在 Content 物件上，將其錨點設為左上角對齊
        // 這對 ScrollView 下的 Content 佈局很重要
        gridRect.anchorMin = new Vector2(0, 1);
        gridRect.anchorMax = new Vector2(0, 1);
        gridRect.pivot = new Vector2(0.5f, 1); // 通常 pivot x=0.5, y=1 效果更好
    }

    private void ResetScrollPosition()
    {
        // 嘗試找到父物件的 ScrollRect 並將其滾動位置重設到頂部
        if (transform.parent != null)
        {
            ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(InventoryGridEditor))]
public class InventoryGridEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        // 繪製預設的 Inspector 介面
        DrawDefaultInspector();

        InventoryGridEditor editorScript = (InventoryGridEditor)target;

        // 如果 slotPrefab 未被指派，顯示一個警告框
        SerializedProperty slotPrefabProp = serializedObject.FindProperty("slotPrefab");
        if (slotPrefabProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("請指派 Slot Prefab 以生成格子。", MessageType.Warning);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("強制更新背包格子"))
        {
            editorScript.UpdateGrid();
        }

        if (GUILayout.Button("清除所有格子"))
        {
            // 通過修改 SerializedObject 來觸發 OnValidate 並支持撤銷操作
            SerializedProperty slotCountProp = serializedObject.FindProperty("slotCount");
            slotCountProp.intValue = 0;
            serializedObject.ApplyModifiedProperties(); // 應用修改
        }
    }
}
#endif