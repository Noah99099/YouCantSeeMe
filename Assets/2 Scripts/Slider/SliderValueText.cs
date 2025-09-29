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
            valueText.text = value.ToString("F0");
        }
    }
}
