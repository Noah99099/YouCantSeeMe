using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TeleportListPanelManager : MonoBehaviour
{
    public static TeleportListPanelManager Instance;
    
    [Header("UI 參照")]
    public List<TeleportListButton> teleportButtons; 
    public Image mapImage;

    private void Awake()
    {
        Instance = this;
    }

    // 【魔法在這裡】只要這個面板被 SetActive(true) 打開，Unity 就會自動執行這裡！
    private void OnEnable()
    {
        // 1. 關閉地圖的拖曳判定，讓點擊能 100% 穿透到按鈕上
        if (mapImage != null)
        {
            mapImage.raycastTarget = false;
        }

        // 2. 自動刷新底下所有按鈕的文字與狀態
        UpdateAllButtons();
    }

    // 只要這個面板被 SetActive(false) 關閉，Unity 就會自動執行這裡！
    private void OnDisable()
    {
        // 恢復地圖的拖曳判定，讓玩家可以繼續看地圖
        if (mapImage != null)
        {
            mapImage.raycastTarget = true;
        }
    }

    public void UpdateAllButtons()
    {
        foreach (var btn in teleportButtons)
        {
            if (btn != null)
            {
                btn.RefreshStatus();
            }
        }
    }
}