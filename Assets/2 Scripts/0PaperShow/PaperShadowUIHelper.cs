using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Spine.Unity;
using Spine;

public class PaperShadowUIHelper : MonoBehaviour
{
    [Header("Spine 組件")]
    public SkeletonGraphic spineGraphic;

    // 播放動畫並等待結束 (不循環動畫)
    public IEnumerator PlaySpineAndWait(string animName)
    {
        if (string.IsNullOrEmpty(animName)) yield break;

        var track = spineGraphic.AnimationState.SetAnimation(0, animName, false);
        track.TimeScale = 1f; // 確保正常播放速度

        yield return new WaitForSeconds(track.Animation.Duration);

        // 必定播放到底 -> 動畫暫停 (停留在最後一幀，等待切換過渡動畫)
        track.TimeScale = 0f;
    }

    // 預約下一個動畫
    public void AddAnimation(string animName, bool loop, float delay = 0f)
    {
        if (string.IsNullOrEmpty(animName)) return;
        var track = spineGraphic.AnimationState.AddAnimation(0, animName, loop, delay);
        track.TimeScale = 1f; // 下一個動畫接續時恢復正常速度
    }

    // 單純播放動畫（不等待）
    public void PlaySpine(string animName, bool loop = false)
    {
        if (string.IsNullOrEmpty(animName)) return;
        var track = spineGraphic.AnimationState.SetAnimation(0, animName, loop);
        track.TimeScale = 1f; // 確保正常播放速度
    }

    // 【新增】取消當前動畫的循環，並清空後續尚未播放的預約動畫
    public void StopCurrentLoopAndClearQueue()
    {
        TrackEntry current = spineGraphic.AnimationState.GetCurrent(0);
        if (current != null)
        {
            // 1. 記住當前正在播放的「動畫名稱」與「播放到幾秒 (TrackTime)」
            string currentAnimName = current.Animation.Name;
            float currentTime = current.TrackTime;

            // 2. 重新呼叫 SetAnimation (這動作會自動清空 Track 0 後續所有的排隊)
            // 並將 Loop 設為 false，讓它播完這圈就停
            TrackEntry newEntry = spineGraphic.AnimationState.SetAnimation(0, currentAnimName, false);

            // 3. 瞬間把進度條拉回剛才的時間點！視覺上完全無縫接軌。
            newEntry.TrackTime = currentTime;
        }
    }

    // 【修正】等待當前正在播放的動畫結束 (精準支援循環動畫播完當下這圈)
    public IEnumerator WaitForCurrentAnimation()
    {
        TrackEntry current = spineGraphic.AnimationState.GetCurrent(0);
        if (current != null)
        {
            float remainingTime = current.Loop
                ? current.Animation.Duration - (current.TrackTime % current.Animation.Duration)
                : current.Animation.Duration - current.AnimationTime;

            if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

            // 如果這是不循環的動畫，播到底後暫停
            if (!current.Loop)
            {
                current.TimeScale = 0f;
            }
        }
    }

    // UI 透明度漸變
    public IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    // 【新增】AudioSource 音量漸變
    public IEnumerator FadeAudioSource(AudioSource audioSource, float targetVolume, float duration)
    {
        if (audioSource == null) yield break;
        float startVolume = audioSource.volume;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }
        audioSource.volume = targetVolume;

        // 如果目標音量是 0，則直接停止播放以節省效能
        if (targetVolume <= 0f)
        {
            audioSource.Stop();
        }
    }

    // 延遲一段時間
    public IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
}