// 檔案名稱: ScreenGlitchEffect.cs
using UnityEngine;
using UnityEngine.Rendering;

public class ScreenGlitchEffect : MonoBehaviour
{
    // 拖入掛載此腳本的 "Glitch" Volume 物件
    [Tooltip("請將此腳本所在的 Volume 物件拖到這裡")]
    public Volume glitchVolume;

    void Awake()
    {
        if (glitchVolume == null)
        {
            glitchVolume = GetComponent<Volume>(); // 嘗試自動獲取
        }
        if (glitchVolume == null)
        {
            Debug.LogError("[ScreenGlitchEffect] 找不到 Volume 元件！", this);
            return;
        }

        // 遊戲開始時確保特效 (Volume) 是關閉的
        StopGlitch();
    }

    // 將強度 0-1 映射到 Volume 的 Weight 0-1
    public void SetGlitchIntensity(float intensity)
    {
        if (glitchVolume == null) return;
        glitchVolume.weight = Mathf.Clamp01(intensity);
    }

    // 呼叫時，將 Weight 設為 1 (開啟)
    public void PlayOneShotGlitch()
    {
        SetGlitchIntensity(1.0f);
    }

    // 呼叫時，將 Weight 設為 0 (關閉)
    public void StopGlitch()
    {
        SetGlitchIntensity(0.0f);
    }
}