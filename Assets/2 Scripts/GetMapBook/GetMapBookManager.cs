using UnityEngine;

public class GetMapBookManager : MonoBehaviour
{
    // 單例模式，方便 Scene 2 的腳本找到它
    public static GetMapBookManager Instance;

    [Header("要跨場景的兩個 UI (請在 Scene 1 拖曳賦值)")]
    [Tooltip("B")] public GameObject uiElement1;
    [Tooltip("M")] public GameObject uiElement2;

    [Header("收集進度確認")]
    public bool hasMap = false;
    public bool hasBook = false;

    private void Awake()
    {
        // 設定單例並確保跨場景不被銷毀
        if (Instance == null)
        {
            Instance = this;
            // 如果你的 Canvas/Player 已經有其他的 DontDestroyOnLoad 管理，這行可以依情況註解掉
            DontDestroyOnLoad(this.transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject); // 防止回到 Scene 1 時產生重複的管理器
        }
    }

    private void OnEnable()
    {
        // 訂閱事件 (在 Scene 1 或 Scene 2 觸發都能收到)
        Map.GetMap += OnMapCollected;
        CaseRecordBook.OnCollected += OnCaseRecordBookCollected;
    }

    private void OnDisable()
    {
        Map.GetMap -= OnMapCollected;
        CaseRecordBook.OnCollected -= OnCaseRecordBookCollected;
    }

    private void OnMapCollected()
    {
        hasMap = true;
        NotifyScene2();
    }

    private void OnCaseRecordBookCollected()
    {
        hasBook = true;
        NotifyScene2();
    }

    // 嘗試通知 Scene 2 的 GetTwoThings
    public void NotifyScene2()
    {
        if (hasMap && hasBook)
        {
            // 在當前場景尋找 GetTwoThings
            GetTwoThings scene2Controller = FindObjectOfType<GetTwoThings>();

            // 如果找到了 (代表玩家現在位於 Scene 2)，就把 UI 傳過去讓它打開
            if (scene2Controller != null)
            {
                // 呼叫 Scene 2 腳本的執行方法
                scene2Controller.ExecuteActivation(uiElement1, uiElement2);
            }
        }
    }
}
