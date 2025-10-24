using UnityEngine;
using TMPro;
using System.Text; // 引用 StringBuilder
using System.Collections.Generic;

public class DialogueDebugUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI variableDisplayText;
    private StringBuilder stringBuilder = new StringBuilder();

    void Update()
    {
        // 檢查 DialogueManager 是否存在且對話正在進行
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            // 獲取當前的變數列表
            List<Variable> variables = DialogueManager.Instance.GetCurrentGraphVariables();

            if (variables != null)
            {
                // 如果 Panel 是隱藏的，就顯示它
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }

                // 使用 StringBuilder 來高效地組合字串
                stringBuilder.Clear();
                stringBuilder.AppendLine("--- Dialogue Variables ---");

                foreach (var variable in variables)
                {
                    stringBuilder.AppendLine($"{variable.name}: {variable.value}");
                }

                // 更新文字顯示
                variableDisplayText.text = stringBuilder.ToString();
            }
        }
        else
        {
            // 如果對話未進行，就隱藏 Panel
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}