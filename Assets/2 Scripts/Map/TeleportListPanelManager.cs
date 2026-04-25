using UnityEngine;
using System.Collections.Generic;

public class TeleportListPanelManager : MonoBehaviour
{
    public static TeleportListPanelManager Instance;
    public List<TeleportListButton> teleportButtons; // 拖入面板下所有的傳送按鈕

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false); // 預設關閉
    }

    // 切換面板顯示/隱藏
    public void TogglePanel()
    {
        bool isActive = !gameObject.activeSelf;
        gameObject.SetActive(isActive);

        if (isActive)
        {
            UpdateAllButtons();
        }
    }

    public void UpdateAllButtons()
    {
        foreach (var btn in teleportButtons)
        {
            btn.RefreshStatus();
        }
    }
}