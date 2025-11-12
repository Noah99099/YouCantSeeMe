using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderValueText : MonoBehaviour
{
    [SerializeField] private Slider targetSlider;
    [SerializeField] private TMP_Text valueText;

    private void Start()
    {
        if (targetSlider != null)
        {
            targetSlider.onValueChanged.AddListener(UpdateValueText);
            UpdateValueText(targetSlider.value); // 初始化顯示
        }
    }

    private void UpdateValueText(float value)
    {
        if (valueText != null)
        {
            // 邏輯：你的 Slider 最大是 10000，但你想顯示 100
            // 所以我們要除以 100 (10000 / 100 = 100)
            //float displayValue = value / 100.0f;
            float displayValue = value;

            // 四捨五入取整數 (例如 99.9 顯示為 100)
            valueText.text = Mathf.RoundToInt(displayValue).ToString();
        }
    }

    // 記得移除監聽
    private void OnDestroy()
    {
        if (targetSlider != null)
        {
            targetSlider.onValueChanged.RemoveListener(UpdateValueText);
        }
    }
}
