using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class MusicSlider : MonoBehaviour
{
    private Slider musicSlider;

    private void Start()
    {
        musicSlider = GetComponent<Slider>();

        // 強制設定 Slider 範圍為 0~100，避免在 Inspector 忘記改導致 Bug
        musicSlider.minValue = 0f;
        musicSlider.maxValue = 100f;

        if (AudioManager.Instance != null)
        {
            // 1. 取得當前全局音量
            float currentVol = AudioManager.Instance.GetMasterVolume();

            // 2. 靜默設定 Slider 的值 (不會觸發 OnValueChanged)
            musicSlider.SetValueWithoutNotify(currentVol);

            // 3. 加入監聽器
            musicSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        else
        {
            Debug.LogError("找不到 AudioManager 實例，確保它存在於場景中！");
            musicSlider.interactable = false;
        }
    }

    private void OnSliderValueChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }

    private void OnDestroy()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}