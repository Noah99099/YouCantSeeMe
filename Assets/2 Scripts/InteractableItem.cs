using UnityEngine;

// 需要一個碰撞器來被射線偵測到
[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour
{
    // 在 Inspector 中，將你為這個物件建立的 ItemData 資源檔拖曳到這裡
    public ItemData itemData;
}