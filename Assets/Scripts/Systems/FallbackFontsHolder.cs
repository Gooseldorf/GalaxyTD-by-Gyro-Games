using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FallbackFontsHolder : SerializedScriptableObject
{
    [OdinSerialize, NonSerialized, ShowInInspector]
    private Dictionary<string, Font> fallbackFonts = new ();

    public void SetFallbackFont(VisualElement element)
    {
        string currentLanguage = LocalizationManager.CurrentLanguage;
        
        if (!fallbackFonts.ContainsKey(currentLanguage) || fallbackFonts[currentLanguage] == null)
        {
            Debug.LogError($"{nameof(FallbackFontsHolder)} doesn't contain fallback font for {currentLanguage}");
            return;
        }
        
        if (element.ClassListContains("SecondaryFontFlag"))
        {
            if (currentLanguage is "English" or "Russian" or "Italian" or "German" or "Portuguese")
            {
                if(!element.ClassListContains("SecondaryFontLatin"))
                    element.AddToClassList("SecondaryFontLatin");
            }
            else
                element.RemoveFromClassList("SecondaryFontLatin");
            return;
        }
        
        foreach (var languagePair in fallbackFonts)
            element.RemoveFromClassList(languagePair.Key);
        
        element.AddToClassList(currentLanguage);
    }
    
#if UNITY_EDITOR    
    [Button]
    private void PopulateLanguagesFromI2Loc()
    {
        foreach (string lang in LocalizationManager.GetAllLanguages())
        {
            fallbackFonts.TryAdd(lang, null);
        }   
    }
#endif
}
