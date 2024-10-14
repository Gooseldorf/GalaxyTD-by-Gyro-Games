#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.Development
{
    public static class SceneMenu
    {
        public const string MAIN_MENU_PATH =  "Assets/Scenes/MainMenu.unity";
        public const string GAME_SCENE_PATH = "Assets/Scenes/ECSScene.unity";
        public const string ROGUELIKE_SCENE_PATH = "Assets/Scenes/ECSSceneRog.unity";

        [MenuItem("Scene/Open Main Menu Scene")]
        private static void OpenMainMenuScene() => OpenScene(MAIN_MENU_PATH);

        [MenuItem("Scene/Open Game Scene")]
        private static void OpenGameScene() => OpenScene(GAME_SCENE_PATH);
        
        [MenuItem("Scene/Open Roguelike Game Scene")]
        private static void OpenRoguelikeGameScene() => OpenScene(ROGUELIKE_SCENE_PATH);

        private static void OpenScene(string sceneName)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(sceneName);
        }
    }
}

#endif
