using UnityEngine;

// --- 轉換器 (Wrappers) ---
// 這些 class 負責將你現有的數據 "翻譯" 成 IClue 介面

/// <summary>
/// [已更新] 物品 (ItemData) 的轉換器
/// </summary>
public class ItemClueWrapper : IClue
{
    private ItemData _item;
    public ItemClueWrapper(ItemData item) { _item = item; }

    // !!重要!! 物品的 ClueID 使用 ItemData.itemID
    public string ClueID => _item.itemID;
    public string ClueName => _item.itemName;
    public string ClueDescription => _item.description;
    public Sprite ClueIcon => _item.icon;
    public EClueType ClueType => EClueType.Item;
    public object OriginalData => _item;
}

/// <summary>
/// [已更新] 回憶 (RoleData + CarouselData) 的轉換器
/// </summary>
public class MemoryClueWrapper : IClue
{
    private RoleData _role;
    private CarouselData _memory;
    private int _index;

    public MemoryClueWrapper(RoleData role, CarouselData memory, int index)
    {
        _role = role;
        _memory = memory;
        _index = index;
    }

    // !!重要!! 回憶的 ClueID 現在使用 CarouselData 的 .name 屬性 (例如: "R101")
    public string ClueID => _memory.name;

    // 物品標題 (使用 CarouselData 的 texts[0])
    public string ClueName
    {
        get
        {
            // 優先使用 texts[0] 作為標題
            if (_memory.texts != null && _memory.texts.Length > 0 && !string.IsNullOrEmpty(_memory.texts[0]))
            {
                return _memory.texts[0]; // 範例: "他前幾天吃了水果"
            }
            // 如果 texts[0] 為空，提供一個備用標題 (使用您舊的邏輯)
            return $"[{_role.roleName}的回憶 {_index + 1}]";
        }
    }

    // 物品描述 (例如 "他前幾天吃了水果")
    // 假設 (使用 CarouselData 的 texts[1])
    public string ClueDescription
    {
        get
        {
            // 優先使用 texts[1] 作為描述
            if (_memory.texts != null && _memory.texts.Length > 1 && !string.IsNullOrEmpty(_memory.texts[1]))
            {
                return _memory.texts[1]; // 範例: "舌頭流血了"
            }
            // 如果 texts[1] 為空，不提供描述
            return ""; // 返回空字串
        }
    }

    // 物品圖標 (使用 CarouselData 的第一張圖)
    public Sprite ClueIcon
    {
        get
        {
            if (_memory.images.Length > 0) return _memory.images[0];
            return null; // 或一個默認圖標
        }
    }

    public EClueType ClueType => EClueType.Memory;
    public object OriginalData => _memory;
}

/// <summary>
/// [已更新] 聲音 (VoiceItemData) 的轉換器
/// </summary>
public class SoundClueWrapper : IClue
{
    private VoiceItemData _sound;
    public SoundClueWrapper(VoiceItemData sound) { _sound = sound; }

    // !!重要!! 聲音的 ClueID 使用 VoiceItemData.voiceItemID
    public string ClueID => _sound.voiceItemID;
    public string ClueName => _sound.itemName;

    // 描述：優先使用「使用後」的文本，因為那才是線索
    public string ClueDescription
    {
        get
        {
            if (!string.IsNullOrEmpty(_sound.descText_After))
                return _sound.descText_After;
            return _sound.descText_Before; // 備用
        }
    }
    public Sprite ClueIcon => _sound.voiceIcon;
    public EClueType ClueType => EClueType.Sound;
    public object OriginalData => _sound;
}

