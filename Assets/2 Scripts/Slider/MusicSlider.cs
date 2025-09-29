using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MusicSlider : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;


    void Start()
    {
        // 獲取自身的 Slider 元件
        musicSlider = GetComponent<Slider>();

        // 檢查 MusicManager 是否存在
        if (MusicManager.Instance != null)
        {
            // 1. 初始化滑桿的值，讓它顯示當前儲存的音量
            musicSlider.value = MusicManager.Instance.masterVolume;

            // 2. 添加監聽器，當滑桿數值被改變時，呼叫 OnSliderValueChanged 方法
            musicSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        else
        {
            Debug.LogError("場景中找不到 MusicManager 的實例！");
            // 如果找不到管理器，可以選擇禁用滑桿
            musicSlider.interactable = false;
        }
    }

    /// <summary>
    /// 當滑桿值改變時觸發此方法
    /// </summary>
    /// <param name="value">滑桿的新值</param>
    private void OnSliderValueChanged(float value)
    {
        // 呼叫 MusicManager 的方法來設定全局音量
        MusicManager.Instance.SetMasterVolume(value);
    }

    // 好習慣：當物件被銷毀時，移除監聽器
    private void OnDestroy()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}
