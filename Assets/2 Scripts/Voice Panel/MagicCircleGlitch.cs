using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MagicCircleGlitch : MonoBehaviour
{
    [Header("法陣交互設定")]
    [Tooltip("觸發的雜音 AudioSource")]
    [SerializeField] private AudioSource staticNoiseSource;

    // 將原本的 public 改為 private，因為我們將透過程式自動抓取
    // [暫時註解] 先不使用花屏特效
    // private ScreenGlitchEffect glitchController;

    [Tooltip("法陣對應的鬼魂 ")]
    [SerializeField] public GameObject ghost;

    // ----- [!! 新增 !!] -----
    // 我們需要快取 (Cache) 這兩個效果參數
    // [暫時註解] 先不使用花屏特效
    // private FilmGrain filmGrainEffect;
    // private ChromaticAberration chromaticAberrationEffect;

    private void Start()
    {
        ghost.SetActive(false);

        // 動態尋找 Tag 為 "PlayerCamera" 的物件，並獲取上面的 ScreenGlitchEffect 腳本
        // [暫時註解] 先不使用花屏特效
        /*
        GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (playerCamera != null)
        {
            glitchController = playerCamera.GetComponent<ScreenGlitchEffect>();
        }
        else
        {
            Debug.LogWarning("找不到 Tag 為 'PlayerCamera' 的物件！請確認 Main Camera 的 Tag 設定是否正確。");
        }
        */
    }

    public void InteractMagicCircle() 
    {
        StartCoroutine(PlayGlitchEffectOnce());
    }

    /// <summary>
    /// [新] 拾取聲音物品時觸發一次花屏
    /// </summary>
    public IEnumerator PlayGlitchEffectOnce()
    {
        // 防呆：如果沒抓到腳本，就不執行後續特效
        // [暫時註解] 避免因為花屏腳本被註解而觸發 yield break，導致後續的聲音和鬼魂出不來
        // if (glitchController == null) yield break;

        Debug.Log("播放花屏特效 (1秒)(目前花屏特效已註解)");
        // ----- [!! 修改 !!] -----
        // 1. 手動將強度設為 1.0 (或你想要的最大值)
        // [暫時註解]
        //if (filmGrainEffect != null) filmGrainEffect.intensity.value = 1.0f;
        //if (chromaticAberrationEffect != null) chromaticAberrationEffect.intensity.value = 1.0f;

        // 2. 開啟 Volume (Weight = 1)
        // [暫時註解]
        //glitchController.PlayOneShotGlitch();

        // 3. 播放聲音
        StartCoroutine(PlayNoiseForDuration(1f));

        // 新增: 鬼魂打開
        ghost.SetActive(true);

        // 4. 等待
        yield return new WaitForSeconds(1.0f);

        // 5. 關閉 Volume (Weight = 0)
        // [暫時註解]
        //glitchController.StopGlitch();

        // 6. 手動將強度歸零 (清理狀態)
        // [暫時註解]
        //if (filmGrainEffect != null) filmGrainEffect.intensity.value = 0.0f;
        //if (chromaticAberrationEffect != null) chromaticAberrationEffect.intensity.value = 0.0f;
        // ----- [!! 結束修改 !!] -----
        Debug.Log("花屏特效結束");
    }

    /// <summary>
    /// 觸發雜音
    /// </summary>
    /// <param name="duration">時長</param>
    /// <returns></returns>
    private IEnumerator PlayNoiseForDuration(float duration)
    {
        staticNoiseSource.volume = 1;
        staticNoiseSource.Play();
        yield return new WaitForSeconds(duration);
        staticNoiseSource.Stop();
        staticNoiseSource.volume = 0;
    }
}
