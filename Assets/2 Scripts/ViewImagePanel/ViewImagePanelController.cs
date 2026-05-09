using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static InputActionMaps; // 使用您定義的常數

public class ViewImagePanelController : MonoBehaviour
{
    public static ViewImagePanelController Instance { get; private set; }

    [Header("UI 參考")]
    [Tooltip("請將包含圖片與按鈕的 Panel 根物件拖曳至此")]
    public GameObject panelRoot;
    public Image displayImage;

    // 【新增】引用 AspectRatioFitter
    [Tooltip("請將掛在 DisplayImage 上的 AspectRatioFitter 拖曳至此")]
    public AspectRatioFitter aspectFitter;

    public Button closeButton;

    [Header("Input 設定")]
    [Tooltip("在 Input Action Asset 中，用來關閉面板的 Action 名稱 (如: Close)")]
    public string closeActionName = "Close";
    private InputAction closeAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 綁定 UI 的關閉按鈕
        closeButton.onClick.AddListener(ClosePanel);
        panelRoot.SetActive(false);

        // 如果忘記拖曳，自動抓取
        if (aspectFitter == null && displayImage != null)
        {
            aspectFitter = displayImage.GetComponent<AspectRatioFitter>();
        }
    }

    /// <summary>
    /// 由可交互物件呼叫，傳入陽與陰兩張圖片
    /// </summary>
    public void OpenPanel(Sprite yangSprite, Sprite yinSprite)
    {
        // 1. 根據 ViewManager 當前視野，決定顯示哪張圖片
        Sprite targetSprite = (ViewManager.Instance.CurrentView == ViewType.Yang) ? yangSprite : yinSprite;

        if (targetSprite != null)
        {
            displayImage.sprite = targetSprite;

            // 【關鍵修改】：動態更新 AspectRatioFitter 的比例
            if (aspectFitter != null)
            {
                // 計算這張 Sprite 的真實寬高比 (寬度 / 高度)
                float ratio = targetSprite.rect.width / targetSprite.rect.height;
                // 將新比例賦值給 Fitter
                aspectFitter.aspectRatio = ratio;
            }
        }

        // 2. 開啟 UI 面板
        panelRoot.SetActive(true);

        // 3. 推入新的 Action Map (這會暫停 _Player 模式的交互)
        InputStackManager.Instance.PushMap(_ViewImagePanel);

        // 4. 動態抓取當前 Map 的退出動作 (ESC)，並綁定事件
        var currentMap = InputStackManager.Instance.GetCurrentActionMap();
        if (currentMap != null)
        {
            closeAction = currentMap.FindAction(closeActionName);
            if (closeAction != null)
            {
                closeAction.performed += OnCloseAction;
                // 確保該 Action 是啟用狀態
                closeAction.Enable();
            }
            else
            {
                Debug.LogWarning($"[ViewImagePanel] 在 {_ViewImagePanel} 中找不到名為 '{closeActionName}' 的 Action！");
            }
        }
    }

    public void ClosePanel()
    {
        // 防呆檢查：確認當前真的是這個面板的 Map 才 Pop
        if (InputStackManager.Instance.CurrentMap == _ViewImagePanel)
        {
            InputStackManager.Instance.PopMap();
        }

        // 解除 ESC 按鍵的綁定，避免記憶體洩漏或重複觸發
        if (closeAction != null)
        {
            closeAction.performed -= OnCloseAction;
            closeAction = null;
        }

        panelRoot.SetActive(false);
    }

    private void OnCloseAction(InputAction.CallbackContext context)
    {
        ClosePanel();
    }
}
