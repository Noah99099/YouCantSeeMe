// TutorialData.cs
using UnityEngine;

/// <summary>
/// 代表教學中的單一頁面（一張圖 + 對應的說明文字）。
/// </summary>
[System.Serializable]
public class TutorialSlide
{
    public Sprite image;

    [TextArea(4, 10)]
    public string contentText;
}

/// <summary>
/// ScriptableObject，用來定義一個完整的教學主題（例如「功能教學」）。
/// 包含一個標題和多個教學頁面 (TutorialSlide)。
/// </summary>
[CreateAssetMenu(fileName = "NewTutorialData", menuName = "Tutorial/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Header("教學標題")]
    public string title;

    [Header("教學頁面 (圖片+對應內文)")]
    public TutorialSlide[] slides;
}