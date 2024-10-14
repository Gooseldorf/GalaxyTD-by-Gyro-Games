using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DialogHolder : ScriptableObject
{
    [SerializeField]
    private List<Dialog> dialogs;
    
    public Dialog GetDialog(int index) => dialogs.Find(x => x.MissionIndex == index);

#if UNITY_EDITOR
    [ShowInInspector, TextArea]
    private string parseArea;

    [Button]
    private void Parse()
    {
        dialogs = new List<Dialog>();
        Dialog dialog = null;
        bool isBefore = true;
        string[] data = parseArea.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        try
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].Contains("Level"))
                {
                    if (dialog != null)
                        dialogs.Add(dialog);
                    dialog = new Dialog()
                    {
                        MissionIndex = int.Parse(data[i].Substring(data[i].IndexOf(" ") + 1)),
                        AfterMission = new List<DialogLine>(),
                        BeforeMission = new List<DialogLine>()
                    };
                    isBefore = true;
                    continue;
                }

                if (string.IsNullOrEmpty(data[i]))
                    continue;

                if (data[i].Contains("["))
                {
                    if (data[i].Contains("side"))
                        isBefore = false;
                    continue;
                }

                string characterName = data[i].Replace("'", "").Replace(".", "").Replace(" ", "").Replace(",", "");
                if (characterName.Contains("("))
                {
                    characterName = characterName[..characterName.IndexOf("(")];
                }

                DialogLine dialogLine = new DialogLine()
                {
                    CharacterKey = (AllEnums.DialogCharacter)Enum.Parse(typeof(AllEnums.DialogCharacter), characterName),
                    CharacterPosition = AllEnums.DialogPosition.Any
                };
                if (isBefore)
                    dialog.BeforeMission.Add(dialogLine);
                else
                    dialog.AfterMission.Add(dialogLine);

            }
        }
        catch (Exception e)
        {
            Debug.LogError("Something went wrong on parsing: " + e);
        }
        UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
    }

    [Button]
    private void CheckEnums()
    {
        string[] data = parseArea.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        Dictionary<string, int> values = new();
        for (int i = 0; i < data.Length; i++)
        {
            if (string.IsNullOrEmpty(data[i]) || data[i].Contains("[") || data[i].Contains("Level"))
                continue;

            string characterName = data[i].Replace("'", "").Replace(".", "").Replace(" ", "").Replace(",", "");
            if (characterName.Contains("("))
                characterName = characterName[..characterName.IndexOf("(")];

            if (values.ContainsKey(characterName))
            {
                values[characterName]++;
            }
            else
            {
                values.Add(characterName, 1);
                Debug.Log(characterName);
            }
        }
    }
    //[Button]
    private void SetImagesToNull()
    {
        for (int i = 0; i < dialogs.Count; i++)
        {
            dialogs[i].BeforeBg = dialogs[i].AfterBg = null;
        }
        UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
    }

    //private const string path = "/Tests/Dialogs/TestDialog.json";

    //[Button]
    //public void DeserializeDialogs()
    //{
    //    string fullPath = Application.dataPath + path;
    //    if (File.Exists(fullPath))
    //    {
    //        try
    //        {
    //            EditorUtility.SetDirty(this);
    //            string fileContent = File.ReadAllText(fullPath);
    //            dialogs = JsonConvert.DeserializeObject<DialogCollection>(fileContent);
    //            AssetDatabase.SaveAssets();
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.Log($"Error while deserializing the file at path {fullPath}. Error: {e.Message}");
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log($"No file found at path {fullPath}");
    //    }
    //}
#endif
}
[Serializable]
public class Dialog
{
    public int MissionIndex;
    public Sprite BeforeBg;
    public List<DialogLine> BeforeMission;
    public Sprite AfterBg;
    public List<DialogLine> AfterMission;
}


[Serializable]
public class DialogLine
{
    public AllEnums.DialogCharacter CharacterKey;
    public AllEnums.DialogPosition CharacterPosition;
}


/* json parser
[Serializable]
public class Dialog
{
    [JsonProperty("DialogLines")]
    public List<DialogLine> DialogLines;
}

[Serializable]
public class DialogLine
{
    [JsonProperty("CharacterKey")]
    public string CharacterKey;

    [JsonProperty("LineKey")]
    public string LineKey;
}

[Serializable]
public class DialogCollection
{
    [JsonProperty("Dialogs")]
    public List<Dialog> Dialogs;
}
*/