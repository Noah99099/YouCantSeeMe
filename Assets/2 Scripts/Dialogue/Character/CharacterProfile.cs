using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Character Profile", menuName = "Dialogue/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    public string characterID; // 角色的唯一識別 ID，例如 "player", "npc_anna"
    public string characterName; // 顯示在對話框上的名字

    public List<CharacterExpression> expressions;

    // 一個輔助方法，方便我們透過關鍵字找到對應的 Sprite
    public Sprite GetSprite(string keyword)
    {
        // 先嘗試找到完全符合關鍵字的表情
        var expression = expressions.Find(e => e.keyword == keyword);
        if (expression != null)
        {
            return expression.sprite;
        }

        // 如果找不到，嘗試返回名為 "default" 的預設表情
        expression = expressions.Find(e => e.keyword == "default");
        if (expression != null)
        {
            return expression.sprite;
        }

        // 如果連 default 都沒有，返回列表中的第一個作為備用
        if (expressions.Count > 0)
        {
            return expressions[0].sprite;
        }

        // 如果列表是空的，返回 null
        return null;
    }
}