using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SensitivitySlider : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private GameObject sensitivityManagerPrefab; // 拖進 SensitivityManager prefab

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
        yield return null;

        if (SensitivityManager.Instance != null && sensitivitySlider != null)
        {
            sensitivitySlider.minValue = SensitivityManager.Instance.minSensitivity;
            sensitivitySlider.maxValue = SensitivityManager.Instance.maxSensitivity;

            // 設定初始值
            sensitivitySlider.value = SensitivityManager.Instance.mouseSensitivity;

            // 訂閱事件
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        else
        {
            Debug.LogWarning("無法綁定 SensitivityManager 或 sensitivitySlider 為 null");
        }
    }

    private void OnSensitivityChanged(float value)
    {
        if (SensitivityManager.Instance != null)
        {
            SensitivityManager.Instance.mouseSensitivity = value;
        }
    }
}
