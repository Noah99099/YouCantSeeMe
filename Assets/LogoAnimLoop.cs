using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;

public class LogoAnimLoop : MonoBehaviour
{
    public SkeletonGraphic logoAnim;
    public float minTime;
    public float maxTime;

    private string[] moveAnimations = { "ballMove_L", "ballMove_R", "ballMove_U", "ballMove_D" }; //左右上下
    private int currentGroup;
    private int currentStep = 0;
    private Coroutine idleCoroutine;

    private void Start()
    {
        currentGroup = Random.Range(1, 4); // 1~3 隨機選擇一組
        print("第" + currentGroup + "組動畫");

        PlayNextInGroup();

        logoAnim.AnimationState.Complete += OnAnimationComplete;
    }
    private void OnDestroy()
    {
        logoAnim.AnimationState.Complete -= OnAnimationComplete;
    }

    private void OnAnimationComplete(TrackEntry trackEntry)
    {
        // 只有非 idle 動畫會經由這裡進下一步
        if (trackEntry.Animation.Name != "idle")
        {
            PlayNextInGroup();
        }
    }

    private void PlayNextInGroup()
    {
        string nextAnim = "";

        switch (currentGroup)
        {
            case 1:
                switch (currentStep % 4)
                {
                    case 0: nextAnim = "idle"; break;
                    case 1: nextAnim = moveAnimations[Random.Range(0, moveAnimations.Length)]; break;
                    case 2: nextAnim = "idle"; break;
                    case 3: nextAnim = "blink_Ball"; break;
                }
                break;

            case 2:
                switch (currentStep % 4)
                {
                    case 0: nextAnim = "blink_Ball"; break;
                    case 1: nextAnim = "idle"; break;
                    case 2: nextAnim = moveAnimations[Random.Range(0, moveAnimations.Length)]; break;
                    case 3: nextAnim = "idle"; break;
                }
                break;

            case 3:
                switch (currentStep % 4)
                {
                    case 0: nextAnim = moveAnimations[Random.Range(0, moveAnimations.Length)]; break;
                    case 1: nextAnim = "idle"; break;
                    case 2: nextAnim = "blink_Ball"; break;
                    case 3: nextAnim = "idle"; break;
                }
                break;
        }

        logoAnim.AnimationState.SetAnimation(0, nextAnim, false);
        currentStep++;

        if (nextAnim == "idle")
        {
            logoAnim.AnimationState.SetAnimation(0, "idle", true);
            if (idleCoroutine != null)
                StopCoroutine(idleCoroutine);
            idleCoroutine = StartCoroutine(WaitIdleAndNext());
        }
        else
        {
            logoAnim.AnimationState.SetAnimation(0, nextAnim, false);
        }
    }

    IEnumerator WaitIdleAndNext()
    {
        float waitTime = Random.Range(minTime, maxTime);
        yield return new WaitForSeconds(waitTime);
        PlayNextInGroup();
    }
}
