using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum SensitivityType { Mouse, Gamepad }

public class SensitivitySlider : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private GameObject sensitivityManagerPrefab; // 拖進 SensitivityManager prefab
    [SerializeField] private SensitivityType sensitivityType; // 選擇控制 Mouse 或 Gamepad

    private void Start()
    {
        // 檢查並確保 SensitivityManager 存在
        if (SensitivityManager.Instance == null && sensitivityManagerPrefab != null)
        {
            Instantiate(sensitivityManagerPrefab);
        }

        StartCoroutine(InitAfterDelay());
    }

    private IEnumerator InitAfterDelay()
    {
        // 等待一幀確保 SensitivityManager 初始化完成
        yield return null;

        if (SensitivityManager.Instance != null && sensitivitySlider != null)
        {
            // 將 Slider 範圍放大十倍（0~50）
            sensitivitySlider.minValue = SensitivityManager.Instance.minSensitivity * 10f;
            sensitivitySlider.maxValue = SensitivityManager.Instance.maxSensitivity * 10f;
            sensitivitySlider.wholeNumbers = true; // 每格對應 0.1

            // 將初始值也放大十倍
            float initialValue = 0f;
            switch (sensitivityType)
            {
                case SensitivityType.Mouse:
                    initialValue = SensitivityManager.Instance.mouseSensitivity * 10f;
                    break;
                case SensitivityType.Gamepad:
                    initialValue = SensitivityManager.Instance.gamepadSensitivity * 10f;
                    break;
            }

            sensitivitySlider.value = initialValue;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
    }

    private void OnSensitivityChanged(float value)
    {
        if (SensitivityManager.Instance == null) return;

        // 將 slider.value 映射回原始範圍（除以 10）
        float mappedValue = value / 10f;

        // 直接把 Slider 值傳進 SensitivityManager（範圍已對齊）
        switch (sensitivityType)
        {
            case SensitivityType.Mouse:
                SensitivityManager.Instance.SetMouseSensitivity(mappedValue);
                break;
            case SensitivityType.Gamepad:
                SensitivityManager.Instance.SetGamepadSensitivity(mappedValue);
                break;
        }
    }
}
