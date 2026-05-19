using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement; // 必須引用場景管理

public class RightHintManager : MonoBehaviour
{
    public static RightHintManager Instance;

    [Header("UI 參照 (Scene 1 專用)")]

    [Header("=基本生成設定=")]
    [SerializeField] private GameObject hintPrefab; // 放入掛有 SelfDestroyHint 的 Prefab
    [SerializeField] private Transform uiCanvas;    // 必須生在 Canvas 底下才能顯示

    [Header("=提示文本=")]
    public string text_1; // 使用左Shift切換視野

    private void Awake()
    {
        // 單純的單例模式，移除跨場景保留，避免到 Scene 2 報錯
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    // 將第二個參數加上預設值 -1，代表「如果沒特別指定，就用 Inspector 設定的時間」
    public void ShowHint()
    {
        if (hintPrefab == null || uiCanvas == null)
        {
            Debug.LogError("HintSpawner: Prefab 或 Canvas 尚未綁定！");
            return;
        }

        // 1. 在指定的 Canvas 下生成 Prefab 複製品
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);

        // 2. 獲取它身上的 SelfDestroyHint 腳本
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();

        if (hintScript != null)
        {
            // 3. 傳入文字並啟動動畫與銷毀流程 (預設 2 秒)
            hintScript.InitAndShow(text_1);
        }
    }
}
