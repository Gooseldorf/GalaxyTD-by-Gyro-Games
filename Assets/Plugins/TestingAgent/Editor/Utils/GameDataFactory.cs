using UnityEngine;

namespace TestingAgent.Editor.Utils
{
    public static class GameDataFactory
    {
        public static GameData CreateGameData(int softCurrency)
        {
            GameData data = ScriptableObject.CreateInstance<GameData>();
            data.SetFieldValue("softCurrency", softCurrency);
            return data;
        }
    }
}