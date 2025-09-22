using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class InventoryGridEditor : MonoBehaviour
{
    [Header("背包格子設置")]
    public GameObject slotPrefab; // 格子預製體
    public int slotCount = 40; // 格子數量
    public int columns = 8; // 每橫行數
    public float spacing = 10f; // 間距

    [Header("參考組件")]
    public RectTransform contentRect; // Content 的 RectTransform
    public GridLayoutGroup gridLayout; // 網格佈局組件

    [Header("布局設置")]
    public bool autoSetupAnchors = true; // 自動設置Anchor

    private List<GameObject> currentSlots = new List<GameObject>(); // 當前格子列表
    private int lastSlotCount = 0; // 上一次的格子數量

    void Awake()
    {
        // 自動獲取必要的組件
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        //if (contentRect == null && transform.parent != null)
        //    contentRect = transform.parent.GetComponent<RectTransform>();

        if (Application.isPlaying && transform.childCount != slotCount)
        {
            UpdateGrid();
            lastSlotCount = slotCount;
        }
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && lastSlotCount != slotCount)
        {
            UpdateGrid();
            lastSlotCount = slotCount;
        }
#endif
    }

    void Update()
    {
        // 編輯模式下持續檢查是否需要更新
#if UNITY_EDITOR
        if (!Application.isPlaying && lastSlotCount != slotCount)
        {
            UpdateGrid();
            lastSlotCount = slotCount;
        }
#endif
    }

    private void ClearAllSlots()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 延遲到下一幀再刪，避免 OnValidate 直接 DestroyImmediate 報錯
            UnityEditor.EditorApplication.delayCall += () =>
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    if (transform.GetChild(i) != null)
                        DestroyImmediate(transform.GetChild(i).gameObject);
                }
            };
        }
        else
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (transform.GetChild(i) != null)
                    Destroy(transform.GetChild(i).gameObject);
            }
        }
#else
    for (int i = transform.childCount - 1; i >= 0; i--)
    {
        if (transform.GetChild(i) != null)
            Destroy(transform.GetChild(i).gameObject);
    }
#endif

        currentSlots.Clear();
    }

    void UpdateGrid()
    {
        // 確保有網格佈局組件
        if (gridLayout == null)
        {
            gridLayout = GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = gameObject.AddComponent<GridLayoutGroup>();
            }
        }

        // 設置網格佈局
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;

        // --- 自動設置Anchor ---
        if (autoSetupAnchors)
        {
            SetupContentAnchors();
        }

        // --- 調整格子數量 ---
        // 如果子物件比 slotCount 多，刪掉多的
        while (transform.childCount > slotCount)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(transform.childCount - 1).gameObject);
            else
                Destroy(transform.GetChild(transform.childCount - 1).gameObject);
#else
        Destroy(transform.GetChild(transform.childCount - 1).gameObject);
#endif
        }

        // 如果子物件比 slotCount 少，補齊
        while (transform.childCount < slotCount)
        {
            if (slotPrefab != null)
            {
                GameObject slot = Instantiate(slotPrefab, transform);
                slot.name = $"Slot_{transform.childCount -1}";

                // 設置為空狀態
                Image itemIcon = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
                if (itemIcon != null)
                {
                    itemIcon.enabled = true;
                }
            }
        }

        // 更新 Content 大小
        UpdateContentSize();
        // 重置滾動位置到頂部
        ResetScrollPosition();

        Debug.Log($"更新背包格子數量: {slotCount}");
    }

    /// <summary>
    /// 設置Content的Anchor以確保正確布局
    /// </summary>
    private void SetupContentAnchors()
    {
        //if (contentRect == null) return;

        //// 設置Content的Anchor為Top-Left
        //contentRect.anchorMin = new Vector2(0, 1); // Top-Left
        //contentRect.anchorMax = new Vector2(0, 1); // Top-Left
        //contentRect.pivot = new Vector2(0, 1);     // Top-Left
        //contentRect.anchoredPosition = Vector2.zero;

        // 設置InventoryGrid的Anchor
        RectTransform gridRect = GetComponent<RectTransform>();
        if (gridRect != null)
        {
            gridRect.anchorMin = new Vector2(0, 1); // Top-Left
            gridRect.anchorMax = new Vector2(0, 1); // Top-Left
            gridRect.pivot = new Vector2(0, 1);     // Top-Left
        }

        Debug.Log("已自動設置Anchor為Top-Left布局");
    }

    /// <summary>
    /// 更新 Content 的大小以適應所有格子
    /// </summary>
    private void UpdateContentSize()
    {
        if (contentRect == null || gridLayout == null) return;

        // 計算需要的行數
        int rows = Mathf.CeilToInt((float)slotCount / columns);

        // 計算 Content 的高度
        float cellHeight = gridLayout.cellSize.y;
        float totalHeight = rows * cellHeight + (rows - 1) * gridLayout.spacing.y;

        // 設置 Content 的大小
        //contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);

        //// 確保 Content 的錨點設置正確
        //contentRect.anchorMin = new Vector2(0, 1); // 左上角
        //contentRect.anchorMax = new Vector2(0, 1); // 左上角
        //contentRect.pivot = new Vector2(0, 1); // 左上角
    }

    // 添加重置滾動位置的方法
    private void ResetScrollPosition()
    {
        if (contentRect != null && contentRect.parent != null)
        {
            ScrollRect scrollRect = contentRect.parent.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f; // 設置為頂部
            }
        }
    }

    // 在Inspector中添加一個按鈕來手動更新網格
    [ContextMenu("更新背包格子")]
    public void UpdateGridManual() // 將此方法改為 public
    {
        UpdateGrid();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(InventoryGridEditor))]
public class InventoryGridEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InventoryGridEditor editor = (InventoryGridEditor)target;

        // 自動分配按鈕
        if (GUILayout.Button("自動分配組件"))
        {
            if (editor.gridLayout == null)
                editor.gridLayout = editor.GetComponent<GridLayoutGroup>();

            //if (editor.contentRect == null && editor.transform.parent != null)
            //    editor.contentRect = editor.transform.parent.GetComponent<RectTransform>();

            EditorUtility.SetDirty(editor);
        }

        if (GUILayout.Button("更新背包格子"))
        {
            editor.UpdateGridManual();
        }

        if (GUILayout.Button("清除所有格子"))
        {
            editor.slotCount = 0;
            editor.UpdateGridManual();
        }
    }
}
#endif