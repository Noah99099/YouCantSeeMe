using UnityEngine;
using TMPro; // 引用 TextMeshPro

/// <summary>
/// 【新腳本】
/// 一個 ScriptableObject，用於定義對話框的視覺樣式。
/// </summary>
[CreateAssetMenu(fileName = "New Dialogue Box Style", menuName = "Dialogue/Dialogue Box Style")]
public class DialogueBoxStyle : ScriptableObject
{
    [Header("對話框圖片")]
    [Tooltip("對話框的背景圖 (建議使用 9-Sliced Sprite)")]
    public Sprite boxSprite;

    [Header("文字樣式")]
    [Tooltip("說話者名稱的顏色")]
    public Color nameColor = Color.white;
    
    [Tooltip("說話者名稱的字體 (可選)")]
    public TMP_FontAsset nameFont; //

    [Tooltip("對話內容的顏色")]
    public Color contentColor = Color.white;

    [Tooltip("對話內容的字體 (可選)")]
    public TMP_FontAsset contentFont; //
}