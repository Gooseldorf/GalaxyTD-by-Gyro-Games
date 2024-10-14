using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

public class FallbackFontCreator : ScriptableObject
{
    [SerializeField] private TMP_FontAsset mainFontAsset;
    [SerializeField] private Font fallbackFont;
    [Space] 
    //[SerializeField] private string language;
    
    [SerializeField, TextArea] private string textThatWillBeUsedInGame;
    [Space]
    [SerializeField, TextArea] private string hexCodes;
    
    //TODO: There is an error in TMP_FontAsset.CreateFontAsset, that doesn't let you to create texture with 0,0 sizes. Unity says that it was fixed in 3.2.0-pre.4.
    /*[Button]
    private void GenerateFallbackFont()
    {
        string input = RemoveDigitsAndPunctuationMarks(textThatWillBeUsedInGame);
        hexCodes = GetUniqueHexCodes(input);
        
        var newFontAsset = TMP_FontAsset.CreateFontAsset(fallbackFont, 70, 5, GlyphRenderMode.SDFAA, 1024,1024);
        AssetDatabase.CreateAsset(newFontAsset, $"Assets/Visuals/Fonts/DamageTextFallbackFonts/{mainFontAsset.name}_{language}_fallbackFont.asset");
        var charactersToAddToAtlas = hexCodes.Split(',');
        
        foreach(var character in charactersToAddToAtlas)
        {
            // Convert hex code back to char and add to font asset
            var charToAdd = (char)int.Parse(character, System.Globalization.NumberStyles.HexNumber);
            newFontAsset.characterTable[charToAdd] = new TMP_Character(charToAdd, new Glyph());
        }
    }*/
    [Button]
    private string GetUniqueHexCodes(/*string text*/)
    {
        List<string> hexCodes = new();
        StringBuilder result = new StringBuilder();
        foreach (char symbol in textThatWillBeUsedInGame)
        {
            string hexCode = ((int)symbol).ToString("X");
            if (!hexCodes.Contains(hexCode))
            {
                hexCodes.Add(hexCode);
                result.Append(hexCode + ",");
            }
        }

        if (result.Length > 0)
            result.Length--;
        this.hexCodes = result.ToString();
        return result.ToString();
    }
    
    private string RemoveDigitsAndPunctuationMarks(string text)
    {
        StringBuilder result = new StringBuilder();

        foreach (char c in text)
        {
            if (!char.IsPunctuation(c) && !char.IsDigit(c) && !char.IsWhiteSpace(c))
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
