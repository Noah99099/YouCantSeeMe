using UnityEngine;

[CreateAssetMenu(fileName = "NewTeleportPoint", menuName = "MapSystem/TeleportPoint")]
public class TeleportPointData : ScriptableObject
{
    public string pointID;
    public string pointName;        // 傳送點名稱 (例如：1F 大廳)
    public Vector3 targetPosition;  // 傳送的座標
    [Header("狀態 (運行時會被 MapSaveManager 改寫)")]
    public bool isUnlocked = false; // 是否已解鎖
    public int floorLevel;          // 所屬樓層 (對應你的 Y 座標切換邏輯)
}