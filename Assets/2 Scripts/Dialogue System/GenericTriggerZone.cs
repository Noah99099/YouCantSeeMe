using UnityEngine;

// 掛載在任何觸發區域上
[RequireComponent(typeof(Collider))]
public class GenericTriggerZone : MonoBehaviour
{
    private Collider _zoneCollider;

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider>();
        _zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 通知 DialogueManager
            DialogueManager.Instance?.HandleZoneEnter(_zoneCollider);
        }
    }
}