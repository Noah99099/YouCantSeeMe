using UnityEngine;
using UnityEngine.InputSystem;

public class MapPanelUIController : MonoBehaviour
{
    [Tooltip("平面圖面板")]
    public GameObject mapPanel;
    [Tooltip("右下角的提示視野圖標")]
    public GameObject titleUI;
    [Header("準心")]
    public GameObject crossHair;
    [Header("按鍵提示")]
    public GameObject keyHint;

    private void OnEnable()
    {
        // *** 關鍵修改: 使用來自 Level1UIController 的共享實例 ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Map.CloseMap.performed += OnCloseMapPanel;
    }

    private void OnDisable()
    {
        // *** 關鍵修改: 移除 playerControls.Setting.Disable(); ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Map.CloseMap.performed -= OnCloseMapPanel;
    }

    private void OnCloseMapPanel(InputAction.CallbackContext context) //註冊方法
    {
        CloseMap();
    }

    /// <summary>
    /// 從外部呼叫此方法來打開平面圖。
    /// </summary>
    public void OpenMap() // 打開平面圖
    {
        mapPanel.SetActive(true); // 打開平面圖
        titleUI.SetActive(false); // 關掉右下提示
        crossHair.SetActive(false); // 關掉準心
        keyHint.SetActive(false); // 關掉按鍵提示

        // ***** 新增：在這裡集中呼叫 PushMap *****
        // 這確保了只要這個面板被打開，它就一定會正確地 Push Map
        InputStackManager.Instance.PushMap(InputActionMaps._Map);

        Debug.Log("[MapPanelUIController] OpenMap() 執行。");
    }

    public void CloseMap() // 關掉平面圖
    {
        mapPanel.SetActive(false); // 關掉平面圖
        titleUI.SetActive(true); // 打開右下提示
        crossHair.SetActive(true); // 打開準心
        keyHint.SetActive(true); // 打開按鍵提示
        // ***** 新增：在這裡集中呼叫 PopMap *****
        InputStackManager.Instance.PopMap();
    }
}