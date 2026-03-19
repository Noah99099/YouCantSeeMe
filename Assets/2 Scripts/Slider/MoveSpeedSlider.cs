using UnityEngine;
using UnityEngine.UI;

public class MoveSpeedSlider : MonoBehaviour
{
    [SerializeField] private Slider speedSlider;

    private void Start()
    {
        if (speedSlider != null)
        {
            // 讀取儲存的移速，如果玩家是第一次玩（還沒存過），預設給 4.0f
            float savedSpeed = PlayerPrefs.GetFloat("PlayerMoveSpeed", 4.0f);
            speedSlider.value = savedSpeed;

            // 監聽 Slider 數值的拖曳改變
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }
    }

    private void OnSpeedChanged(float value)
    {
        // 1. 將新數值存到 PlayerPrefs 佈告欄裡 (給跨場景讀取用)
        PlayerPrefs.SetFloat("PlayerMoveSpeed", value);
        PlayerPrefs.Save();

        // 2. [!! 核心修改 !!] 即時更新場景中的玩家 (給場景 2 即時生效用)
        // 尋找場景中是否有 SimpleFirstPersonController
        SimpleFirstPersonController player = Object.FindFirstObjectByType<SimpleFirstPersonController>();

        // 如果找到了 (代表在場景 2)，就立刻把它的 MoveSpeed 換成 Slider 的數值
        if (player != null)
        {
            player.MoveSpeed = value;
            Debug.Log($"[MoveSpeedSlider] 已即時將玩家跑速更新為: {value}");
        }
    }

    private void OnDestroy()
    {
        if (speedSlider != null)
        {
            // 養成好習慣，物件銷毀時移除監聽
            speedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
        }
    }
}