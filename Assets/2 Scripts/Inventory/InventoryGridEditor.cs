using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class InventoryGridEditor : MonoBehaviour
{
    [Header("背包格子設置")]
    public GameObject slotPrefab; // 格子預製體
    public int slotCount = 40; // 格子數量
    public int columns = 8; // 每行列數
    public float spacing = 10f; // 間距

    [Header("佈局設置")]
    public GridLayoutGroup gridLayout; // 網格佈局組件

    private List<GameObject> currentSlots = new List<GameObject>(); // 當前格子列表
    private int lastSlotCount = 0; // 上一次的格子數量

    void Awake()
    {
        if (Application.isPlaying)
        {
            // 遊戲模式不要清除編輯器生成的格子
            // 只要數量不對，再更新一次即可
            if (transform.childCount != slotCount)
            {
                UpdateGrid();
                lastSlotCount = slotCount;
            }
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
                int index = transform.childCount;
                GameObject slot = Instantiate(slotPrefab, transform);
                slot.name = $"Slot_{index}";

                // 設置為空狀態
                Image itemIcon = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
                if (itemIcon != null)
                {
                    itemIcon.enabled = true;
                }
            }
        }

        Debug.Log($"更新背包格子數量: {slotCount}");
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