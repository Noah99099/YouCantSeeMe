using UnityEngine;

public class DialogueSceneTrigger : MonoBehaviour
{
    [Header("場景對話設定")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private float delayBeforeStart = 1f; // 進入場景後延遲多久開始對話
    [SerializeField] private bool playOnlyOnce = true; // 是否只播放一次
    
    private bool _hasPlayed = false;

    private void Start()
    {
        if (dialogueRunner == null)
        {
            Debug.LogError("DialogueRunner 未設定！", this);
            return;
        }

        if (!_hasPlayed || !playOnlyOnce)
        {
            Invoke(nameof(StartSceneDialogue), delayBeforeStart);
        }
    }

    private void StartSceneDialogue()
    {
        if (!_hasPlayed || !playOnlyOnce)
        {
            dialogueRunner.StartDialogue();
            _hasPlayed = true;
            
            Debug.Log("場景對話已開始");
        }
    }

    // 公開方法，允許其他腳本重新觸發對話
    public void ResetDialogue()
    {
        _hasPlayed = false;
    }
}