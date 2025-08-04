using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Tooltip("玩家的 Tag，需與 Player 物件相同")]
    public string playerTag = "Player";

    [Tooltip("場景加載器（SceneLoader）物件")]
    public SceneLoader sceneLoader;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // 關閉 PlayerMovement 腳本
            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            // 呼叫 SceneLoader 載入場景
            if (sceneLoader != null)
            {
                sceneLoader.LoadScene();
            }
            else
            {
                Debug.LogWarning("SceneLoader 尚未指定！");
            }
        }
    }
}
