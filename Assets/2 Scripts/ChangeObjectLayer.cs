// ChangeObjectLayer.cs
using UnityEngine;

public class ChangeObjectLayer : MonoBehaviour
{
    [Header("要切換到的 Layer 名稱")]
    public string targetLayerName = "Default";

    [ContextMenu("Change Layer Recursively")]
    public void ChangeLayer()
    {
        int targetLayer = LayerMask.NameToLayer(targetLayerName);
        if (targetLayer == -1)
        {
            Debug.LogError($"Layer 名稱 \"{targetLayerName}\" 不存在，請確認拼字是否正確！");
            return;
        }

        SetLayerRecursively(gameObject, targetLayer);
        Debug.Log($"已將 {gameObject.name}（及其所有子物件）切換至 Layer：{targetLayerName}");
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
