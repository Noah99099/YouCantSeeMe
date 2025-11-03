using UnityEngine;
using UnityEngine.Rendering;

public class ScreenGlitchEffect : MonoBehaviour
{
    [Tooltip("請將此腳本所在的 Volume 物件拖到這裡")]
    public Volume glitchVolume; // 我們現在改用這個公共變數

    void Awake()
    {
        // 優先使用拖進來的
        if (glitchVolume == null)
        {
            glitchVolume = GetComponent<Volume>(); // 如果沒拖，才自己抓
        }
        if (glitchVolume == null)
        {
            Debug.LogError("[ScreenGlitchEffect] 找不到 Volume 元件！", this);
            return;
        }
        StopGlitch(); // 遊戲開始時確保 Weight = 0
    }

    // 將強度 0-1 映射到 Volume 的 Weight 0-1
    public void SetGlitchIntensity(float intensity)
    {
        if (glitchVolume == null) return;
        glitchVolume.weight = Mathf.Clamp01(intensity);
    }

    public void PlayOneShotGlitch()
    {
        SetGlitchIntensity(1.0f); // 將 Weight 設為 1
    }

    public void StopGlitch()
    {
        SetGlitchIntensity(0.0f); // 將 Weight 設為 0
    }
}