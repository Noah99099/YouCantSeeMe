using UnityEngine;

public class ItemUseEffect : MonoBehaviour
{
    public GameObject spawnPrefab; // 要生成的物件：碟子
    public Transform spawnPoint; // 可以指定生成位置

    // 這個方法會連到 onCorrectItemUsed
    public void SpawnItem()
    {
        if (spawnPrefab != null)
        {
            Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;
            Instantiate(spawnPrefab, pos, rot);
        }
    }
}
