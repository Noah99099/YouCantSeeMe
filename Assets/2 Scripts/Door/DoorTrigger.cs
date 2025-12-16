using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public BidirectionalDoor door;
    public int openDirection; // +1 or -1

    private void OnTriggerEnter(Collider other)
    {
        // 只有在腳本啟用的時候才執行 (處理門正在關閉時的禁用)
        if (enabled && other.CompareTag("Player"))
        {
            // 新增：將自己 (this) 作為參數傳遞
            door.OpenToSide(openDirection, this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (enabled && other.CompareTag("Player"))
        {
            // 新增：將自己 (this) 作為參數傳遞
            door.Close(this);
        }
    }
}