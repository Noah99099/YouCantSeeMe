using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private Dictionary<string, Dictionary<string, string>> localizedText;
    public string CurrentLanguage { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocalizedText("localization"); // 讀取 Resources/localization.csv
            SetLanguage("zh_TW"); // 設定預設語言
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadLocalizedText(string fileName)
    {
        localizedText = new Dictionary<string, Dictionary<string, string>>();
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);

        if (csvFile == null)
        {
            Debug.LogError($"[LocalizationManager] 找不到本地化檔案: Resources/{fileName}.csv");
            return;
        }

        StringReader reader = new StringReader(csvFile.text);

        string headerLine = reader.ReadLine();
        if (headerLine != null && headerLine.StartsWith("\uFEFF"))
        {
            headerLine = headerLine.Substring(1);
        }
        string[] headers = ParseCsvLine(headerLine);
        
        while (reader.Peek() > -1)
        {
            string[] line = ParseCsvLine(reader.ReadLine());
            if (line == null || line.Length == 0 || string.IsNullOrEmpty(line[0])) continue;
            if (line.Length != headers.Length) continue;

            string key = line[0];
            for (int i = 1; i < headers.Length; i++)
            {
                string lang = headers[i].Trim();
                if (!localizedText.ContainsKey(lang))
                {
                    localizedText[lang] = new Dictionary<string, string>();
                }
                localizedText[lang][key] = line[i];
            }
        }
    }
    
    private string[] ParseCsvLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return null;
        Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        string[] fields = csvParser.Split(line);
        for (int i = 0; i < fields.Length; i++)
        {
            fields[i] = fields[i].TrimStart(' ', '"').TrimEnd('"').Trim();
        }
        return fields;
    }

    public void SetLanguage(string langCode)
    {
        CurrentLanguage = langCode.Trim();
        Debug.Log("語言已切換為: " + CurrentLanguage);
    }

    public string GetLocalizedText(string key)
    {
        if (string.IsNullOrEmpty(key)) return $"<color=red>KEY IS NULL OR EMPTY</color>";

        if (localizedText != null && localizedText.ContainsKey(CurrentLanguage) && localizedText[CurrentLanguage].ContainsKey(key))
        {
            return localizedText[CurrentLanguage][key];
        }

        Debug.LogWarning($"在語言 '{CurrentLanguage}' 中找不到 Key: {key}");
        return $"<color=orange>MISSING_KEY: {key}</color>";
    }
}