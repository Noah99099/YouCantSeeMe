using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSliderController : MonoBehaviour
{
    [Header("控制的組件")]
    [Tooltip("拖入你的 Audio Mixer 資產")]
    public AudioMixer mainMixer;

    [Header("Mixer 參數")]
    [Tooltip("你在 Audio Mixer 中 Exposed 的參數名稱 (例如 'SFXVolume')")]
    public string exposedParameterName = "SFXVolume";

    [Header("PlayerPrefs 鍵值")]
    [Tooltip("用於儲存和讀取設定的鍵值")]
    public string playerPrefsKey = "SFXVolume";

    private Slider _slider;
    private const float MIN_DB = -80.0f; // AudioMixer 的最小分貝值
    private const float MAX_DB = 0.0f;   // AudioMixer 的最大分貝值

    // 設定你的 Slider 最大值是多少 (根據你的設定填寫 100)
    private const float SLIDER_MAX_VALUE = 100.0f;

    [Tooltip("這是【沒有存檔時】的預設音量。\n如果你希望預設是 50%，請填 50。\n如果你希望預設是 30%，請填 30。")]
    public float defaultVolume = 50.0f; // <--- 新增這個變數

    void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    void Start()
    {
        // 【關鍵修改在這裡】
        // PlayerPrefs.GetFloat 的第二個參數就是「如果找不到存檔，要回傳什麼值？」
        // 我們把它改成你的 defaultVolume 變數
        float savedValue = PlayerPrefs.GetFloat(playerPrefsKey, defaultVolume);

        // 設定 Slider 位置
        _slider.value = savedValue;

        // 設定 Mixer 音量
        SetMixerVolume(savedValue);

        _slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    // 當 Slider 數值被使用者改變時呼叫
    public void OnSliderValueChanged(float value)
    {
        // 加入這一行來檢查
        Debug.Log($"Slider 目前數值: {value}, 計算結果: {value / 10000.0f}");

        // 1. 更新 Mixer 的音量
        SetMixerVolume(value);

        // 2. 將新的數值 (0-1) 儲存到 PlayerPrefs
        PlayerPrefs.SetFloat(playerPrefsKey, value);
    }

    // 將線性的 (0.0001 - 1.0) 數值轉換為對數的 (-80 - 0) 分貝
    private void SetMixerVolume(float sliderValue)
    {
        // 【關鍵修改】
        // Slider 傳進來的是 0 ~ 10000
        // 我們要先把它正規化變成 0 ~ 1，才能帶入分貝公式
        float normalizedValue = sliderValue / SLIDER_MAX_VALUE;

        // 注意：AudioMixer 的音量是用分貝(dB)計算的，這是一個對數(Logarithmic)尺度
        // 而 Slider 的數值是線性的 (Linear) 0 到 1。
        // 我們需要一個轉換公式。
        // 我們將 0.0 視為 0.0001 以避免 Log(0) 產生 -infinity (無限大)
        float dbValue = Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20.0f;

        // 箝位(Clamp)數值，確保它在 Mixer 允許的範圍內 (通常是 -80 到 0)
        float clampedDbValue = Mathf.Clamp(dbValue, MIN_DB, MAX_DB);

        mainMixer.SetFloat(exposedParameterName, clampedDbValue);
    }

    // 記得在物件被銷毀時移除監聽，避免記憶體洩漏
    void OnDestroy()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}