#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevToDev;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;
using DevToDev.Analytics;
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

#endif

namespace DTDEditor
{
    public class DTDEditorViewModel
    {
        private static readonly string OldFileStoragePath = Path.Combine(Directory.GetCurrentDirectory(), ".devtodev");

        private static readonly string NewFileStoragePath =
            Path.Combine(Directory.GetCurrentDirectory(), ".devtodev_services.xml");

        private const string DTD_GAME_OBJECT_OLD_NAME = "[devtodev_initialize]";
        private const string DTD_GAME_OBJECT_NAME = "[devtodev]";
        private readonly DTDEditorModel Model;
        private GameObject DTDGameObject;
        private DevToDevSDK DTDScriptObject;

        internal DTDEditorWindow ActiveWindow
        {
            get => Model.ActiveWindow;
            set => Model.ActiveWindow = value;
        }

        internal DTDPlatform ActivePlatform
        {
            get => Model.ActivePlatform;
            set => Model.ActivePlatform = value;
        }

        internal bool IsAnalyticsEnabled
        {
            get => Model.IsAnalyticsEnabled;
            set
            {
                if (!CanModifySettings()) return;
                Model.IsAnalyticsEnabled = value;
                if (value)
                {
                    AddDTDGameObject();
                }
                else
                {
                    RemoveDTDGameObject();
                }

                MakeSceneDirty();
            }
        }

        internal DTDLogLevel LogLevel
        {
            get => Model.LogLevel;
            set
            {
                if (Model.LogLevel != value && CanModifySettings())
                {
                    Model.LogLevel = value;
                    if (DTDScriptObject == null) return;
                    DTDScriptObject.logLevel = value;
                    MakeSceneDirty();
                }
            }
        }

        /*internal string[] PushScriptsNames;
        internal string[] PushTokenMethods;
        internal string[] PushReceivedMethods;
        internal string[] PushOpenedMethods;
        private MonoBehaviour[] PushScripts;
        private GameObject pushGameObject;
*/
        /*
        internal GameObject PushGameObject
        {
            get { return pushGameObject; }
            set
            {
                if (pushGameObject != value && CanModifySettings())
                {
                    pushGameObject = value;
                    if (value != null)
                    {
                        PushScripts = pushGameObject.GetComponents<MonoBehaviour>();
                        PushScriptsNames = PushScripts.Select(x => x.GetType().Name).ToArray();
                        PushTokenMethods = GetMethodNames(PushTokenMethodSignature);
                        PushReceivedMethods = GetMethodNames(PushReceivedMethodSignature);
                        PushOpenedMethods = GetMethodNames(PushOpenedMethodSignature);
                    }
                    else
                    {
                        PushScripts = new MonoBehaviour[] { };
                        PushScriptsNames = new string[] { };
                        PushTokenMethods = new string[] { };
                        PushReceivedMethods = new string[] { };
                        PushOpenedMethods = new string[] { };
                    }

                    Model.PushGameObjectName = value ? value.name : null;
                    UpdateGameObject();
                }
            }
        }
*/
        /*internal int PushGameObjectScriptIndex
        {
            get { return Model.PushGameObjectScriptIndex; }
            set
            {
                if (CanModifySettings())
                {
                    Model.PushGameObjectScriptIndex = value;
                    if (DTDScriptObject != null)
                    {
                        DTDScriptObject.PushListeners = GetSafeFromArray(PushScripts, value, null);
                        MakeSceneDirty();
                    }
                }
            }
        }

        internal int PushTokenFunctionIndex
        {
            get { return Model.PushTokenFunctionIndex; }
            set
            {
                if (CanModifySettings())
                {
                    Model.PushTokenFunctionIndex = value;
                    if (DTDScriptObject != null)
                    {
                        DTDScriptObject.OnTokenReceived = GetSafeFromArray(PushTokenMethods, value, string.Empty);
                        MakeSceneDirty();
                    }
                }
            }
        }

        internal int PushTokenErrorFunctionIndex
        {
            get { return Model.PushTokenErrorFunctionIndex; }
            set
            {
                if (CanModifySettings())
                {
                    Model.PushTokenErrorFunctionIndex = value;
                    if (DTDScriptObject != null)
                    {
                        DTDScriptObject.OnTokenFailed = GetSafeFromArray(PushTokenMethods, value, string.Empty);
                        MakeSceneDirty();
                    }
                }
            }
        }

        internal int PushReceivedFunctionIndex
        {
            get { return Model.PushReceivedFunctionIndex; }
            set
            {
                if (CanModifySettings())
                {
                    Model.PushReceivedFunctionIndex = value;
                    if (DTDScriptObject != null)
                    {
                        DTDScriptObject.OnPushReceived = GetSafeFromArray(PushReceivedMethods, value, string.Empty);
                        MakeSceneDirty();
                    }
                }
            }
        }

        internal int PushOpenedFunctionIndex
        {
            get { return Model.PushOpenedFunctionIndex; }
            set
            {
                if (CanModifySettings())
                {
                    Model.PushOpenedFunctionIndex = value;
                    if (DTDScriptObject != null)
                    {
                        DTDScriptObject.OnPushOpened = GetSafeFromArray(PushOpenedMethods, value, string.Empty);
                        MakeSceneDirty();
                    }
                }
            }
        }
*/
        private static string CurrentSceneName => SceneManager.GetActiveScene().path;

        public DTDEditorViewModel()
        {
            Model = LoadModel();
        }

        internal void UpdateGameObject()
        {
            if (Model.IsAnalyticsEnabled)
            {
                AddDTDGameObject();
            }
            else
            {
                RemoveDTDGameObject();
            }

            MakeSceneDirty();
        }

        internal void UpdateActivePlatformCredentials(string key)
        {
            var credentials = new DTDCredentials(Model.ActivePlatform, key);
            var index = -1;
            for (var i = 0; i < Model.Credentials.Count; i++)
            {
                if (Model.Credentials[i].platform == Model.ActivePlatform)
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                Model.Credentials.RemoveAt(index);
                Model.Credentials.Insert(index, credentials);
            }
            else
            {
                Model.Credentials.Add(credentials);
            }

            if (DTDScriptObject != null)
            {
                DTDScriptObject.credentials = Model.Credentials.ToArray();
            }

            MakeSceneDirty();
        }

        internal DTDCredentials GetPlatformInfo(DTDPlatform platform)
        {
            foreach (var credential in Model.Credentials)
            {
                if (credential.platform == platform)
                {
                    return credential;
                }
            }

            var credentials = new DTDCredentials(Model.ActivePlatform, "");
            Model.Credentials.Add(credentials);
            return credentials;
        }

        private void AddDTDGameObject()
        {
            DTDGameObject = FindCurrentGameObjectIfExist();
            if (DTDGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(DTDGameObject);
            }

            DTDGameObject = new GameObject {name = DTD_GAME_OBJECT_NAME};
            DTDScriptObject = DTDGameObject.AddComponent<DevToDevSDK>();
            DTDScriptObject.isAnalyticsEnabled = Model.IsAnalyticsEnabled;
            DTDScriptObject.logLevel = Model.LogLevel;
            DTDScriptObject.credentials = Model.Credentials.ToArray();
        }

        private void RemoveDTDGameObject()
        {
            DTDGameObject = FindCurrentGameObjectIfExist();
            if (DTDGameObject == null) return;
            UnityEngine.Object.DestroyImmediate(DTDGameObject);
            DTDGameObject = null;
        }

        private GameObject FindCurrentGameObjectIfExist()
        {
            if (DTDGameObject != null)
            {
                return DTDGameObject;
            }

            DTDGameObject = GameObject.Find(DTD_GAME_OBJECT_OLD_NAME);
            if (DTDGameObject != null)
            {
                return DTDGameObject;
            }

            DTDGameObject = GameObject.Find(DTD_GAME_OBJECT_NAME);
            if (DTDGameObject != null)
            {
                return DTDGameObject;
            }

            return null;
        }

        private bool CanModifySettings()
        {
            var firstScene = EditorBuildSettings.scenes.FirstOrDefault();
            if (firstScene == null)
            {
                ShowDialog("Scenes in Scenes in Build not found (File -> Build Settings...)");
                return false;
            }

            if (firstScene.path.ToLower().Equals(CurrentSceneName.ToLower())) return true;
            ShowDialog(
                "Any changes and integration can be made only on the first scene of the application. Open '" +
                firstScene.path + "' scene to make changes.");
            return false;
        }

        private static void MakeSceneDirty()
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void ShowDialog(string title)
        {
            EditorUtility.DisplayDialog("devtodev", title, "OK");
        }

        private DTDEditorModel LoadModel()
        {
            if (!File.Exists(OldFileStoragePath))
                return File.Exists(NewFileStoragePath) ? LoadNewFormat() : new DTDEditorModel();
            var model = LoadOldFormat();
            File.Delete(OldFileStoragePath);
            return model;
        }

        private static DTDEditorModel LoadOldFormat()
        {
            try
            {
                var resultModel = new DTDEditorModel();
                using (var saveFile = File.Open(OldFileStoragePath, FileMode.Open))
                {
                    using (var streamReader = new StreamReader(saveFile))
                    {
                        streamReader.ReadLine();
                        resultModel.Credentials.Add(new DTDCredentials(DTDPlatform.IOS, streamReader.ReadLine()));
                        resultModel.Credentials.Add(new DTDCredentials(DTDPlatform.Android, streamReader.ReadLine()));
                        resultModel.Credentials.Add(new DTDCredentials(DTDPlatform.MacOS, streamReader.ReadLine()));
                        resultModel.Credentials.Add(new DTDCredentials(DTDPlatform.WindowsStandalone,
                            streamReader.ReadLine()));
                        resultModel.ActiveWindow =
                            (DTDEditorWindow) Enum.Parse(typeof(DTDEditorWindow), streamReader.ReadLine());
                        resultModel.ActivePlatform =
                            (DTDPlatform) Enum.Parse(typeof(DTDPlatform), streamReader.ReadLine());
                        resultModel.IsPushMessagesEnabled = bool.Parse(streamReader.ReadLine());
                        resultModel.LogLevel = (DTDLogLevel) Enum.Parse(typeof(DTDLogLevel), streamReader.ReadLine());
                        resultModel.IsAnalyticsEnabled = bool.Parse(streamReader.ReadLine());
                        resultModel.PushGameObjectName = streamReader.ReadLine();
                        resultModel.PushReceivedFunctionIndex = int.Parse(streamReader.ReadLine());
                        resultModel.PushOpenedFunctionIndex = int.Parse(streamReader.ReadLine());
                        resultModel.PushTokenFunctionIndex = int.Parse(streamReader.ReadLine());
                        resultModel.PushTokenErrorFunctionIndex = int.Parse(streamReader.ReadLine());
                    }
                }

                return resultModel;
            }
            catch (Exception)
            {
                return new DTDEditorModel();
            }
        }

        private DTDEditorModel LoadNewFormat()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(DTDEditorModel));
                using (var reader = new FileStream(NewFileStoragePath, FileMode.Open))
                {
                    return (DTDEditorModel) serializer.Deserialize(reader);
                }
            }
            catch (Exception)
            {
                return new DTDEditorModel();
            }
        }

        public void SaveData()
        {
            if (File.Exists(NewFileStoragePath))
            {
                File.Delete(NewFileStoragePath);
            }

            var serializer = new XmlSerializer(typeof(DTDEditorModel));
            using (var writer = new FileStream(NewFileStoragePath, FileMode.Create))
            {
                serializer.Serialize(writer, Model);
            }
        }
    }
}
#endif