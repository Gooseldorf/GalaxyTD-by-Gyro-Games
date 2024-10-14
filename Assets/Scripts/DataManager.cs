using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DataManager : ScriptableObjSingleton<DataManager>
{
    [field: SerializeField] public GameData GameData { get; set; }
    [FoldoutGroup("Missions availability")]
    [InfoBox("Each threshold implies mission COUNT! Not mission index!\n To disable, set to -1")]
    [SerializeField] private int hardModeMissionCountThreshold;
    [FoldoutGroup("Missions availability")][SerializeField] private int comingSoonCountThreshold;
    [FoldoutGroup("Missions availability")][SerializeField] private int comingSoonHardCountThreshold;
    [SerializeField] private List<ScriptableObject> systems;
    
    public int HardModeMissionCountThreshold => hardModeMissionCountThreshold;
    public int ComingSoonCountThreshold => comingSoonCountThreshold;
    public int ComingSoonHardCountThreshold => comingSoonHardCountThreshold;

    public const int TicketHardPrice = 5; 

    public T Get<T>()
    {
        for (int i = 0, count = this.systems.Count; i < count; i++)
        {
            if (this.systems[i] is T result)
            {
                return result;
            }
        }

        throw new Exception($"Element of type {typeof(T).Name} is not found!");
    }

#if UNITY_EDITOR
    [FoldoutGroup("PredictedCurrencyParsing")][SerializeField, TextArea] private string parseArea;
    [FoldoutGroup("PredictedCurrencyParsing")][SerializeField] private List<int> predictedSoft;
    [FoldoutGroup("PredictedCurrencyParsing")] [SerializeField] private List<int> predictedScrap;
    [FoldoutGroup("PredictedCurrencyParsing")] [SerializeField] private List<int> predictedHard;

    public List<int> PredictedSoft => predictedSoft;
    public List<int> PredictedScrap => predictedScrap;
    public List<int> PredictedHard => predictedHard;



    [FoldoutGroup("PredictedCurrencyParsing")] 
    [Button]
    private void ParsePredictedSoft()
    {
        EditorUtility.SetDirty(this);
        string[] data = parseArea.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        predictedSoft.Clear();

        foreach (string value in data)
        {
            predictedSoft.Add(float.TryParse(value, out float result)? (int)result : 0);
        }
        AssetDatabase.SaveAssetIfDirty(this);
    }
    [FoldoutGroup("PredictedCurrencyParsing")] 
    [Button]
    private void ParsePredictedScrap()
    {
        EditorUtility.SetDirty(this);
        string[] data = parseArea.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        predictedScrap.Clear();

        foreach (string value in data)
        {
            predictedScrap.Add(float.TryParse(value, out float result)? (int)result : 0);
        }
        AssetDatabase.SaveAssetIfDirty(this);
    }
    
    [FoldoutGroup("PredictedCurrencyParsing")] 
    [Button]
    private void ParsePredictedHard()
    {
        EditorUtility.SetDirty(this);
        string[] data = parseArea.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        predictedHard.Clear();

        foreach (string value in data)
        {
            predictedHard.Add(float.TryParse(value, out float result)? (int)result : 0);
        }
        AssetDatabase.SaveAssetIfDirty(this);
    }

    [FoldoutGroup("PredictedCurrencyParsing")] [InfoBox("Paste a table with 3 columns in parse area: Soft, Hard, Scrap")]
    [Button]
    private void ParseAllCurrencies()
    {
        EditorUtility.SetDirty(this);
        string[] lines = parseArea.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        predictedSoft.Clear();
        predictedHard.Clear();
        predictedScrap.Clear();

        foreach (string line in lines)
        {
            string[] data = line.Split('\t');
            if (data.Length == 3) 
            {
                predictedSoft.Add(float.TryParse(data[0], out float softResult)? (int)softResult : 0);
                predictedHard.Add(float.TryParse(data[1], out float hardResult)? (int)hardResult : 0);
                predictedScrap.Add(float.TryParse(data[2], out float scrapResult)? (int)scrapResult : 0);
            }
        }
        AssetDatabase.SaveAssetIfDirty(this);
    }
#endif
}
