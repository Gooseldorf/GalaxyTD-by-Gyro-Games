using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using DevToDev.Analytics;

namespace DTDEditor {
    [Serializable]
    [XmlRoot("DTDEditorSettings")]
    public class DTDEditorModel {
        #region EditorProperties
        [XmlAttribute("activeWindow")]
        public DTDEditorWindow ActiveWindow { get; set; }
        #endregion

        #region AnalyticsProperties
        [XmlAttribute("LogLevel")]
        public DTDLogLevel LogLevel { get; set; }

        [XmlAttribute("isAnalyticsEnabled")]
        public bool IsAnalyticsEnabled { get; set; }

        [XmlAttribute("isPushMessagesEnabled")]
        public bool IsPushMessagesEnabled { get; set; }

        [XmlAttribute("activePlatform")]
        public DTDPlatform ActivePlatform { get; set; }

        [XmlArray("Credentials")]
        [XmlArrayItem("Credential")]
        public List<DTDCredentials> Credentials { get; set; }
        #endregion

        #region NotificationProperties
        [XmlAttribute("pushGameObjectName")]
        public string PushGameObjectName { get; set; }

        [XmlAttribute("pushGameObjectScriptIndex")]
        public int PushGameObjectScriptIndex { get; set; }

        [XmlAttribute("pushTokenFunctionIndex")]
        public int PushTokenFunctionIndex { get; set; }

        [XmlAttribute("pushTokenErrorFunctionIndex")]
        public int PushTokenErrorFunctionIndex { get; set; }

        [XmlAttribute("pushReceivedFunctionIndex")]
        public int PushReceivedFunctionIndex { get; set; }

        [XmlAttribute("pushOpenedFunctionIndex")]
        public int PushOpenedFunctionIndex { get; set; }
        
        #endregion

        public DTDEditorModel() {
            ActiveWindow = DTDEditorWindow.Choice;
            LogLevel = DTDLogLevel.No;
            IsAnalyticsEnabled = false;
            IsPushMessagesEnabled = false;
            ActivePlatform = DTDPlatform.Android;
            Credentials = new List<DTDCredentials>();
            PushGameObjectName = string.Empty;
            PushGameObjectScriptIndex = 0;
            PushTokenFunctionIndex = 0;
            PushTokenErrorFunctionIndex = 0;
            PushReceivedFunctionIndex = 0;
            PushOpenedFunctionIndex = 0;
        }

        public override string ToString() {
            var output = new StringBuilder($"ActivePlatform: {ActivePlatform} Platforms count: {Credentials.Count}");
            foreach (var info in Credentials) {
                output.AppendLine($"Platform: {info.platform}, Key: {info.key}");
            }
            return output.ToString();
        }
    }

    [Serializable]
    public class DTDCredentials {

        [XmlAttribute("platform")]
        public DTDPlatform platform;

        [XmlAttribute("key")]
        public string key;

        public DTDCredentials() {
            platform = DTDPlatform.Android;
            key = string.Empty;

        }

        public DTDCredentials(DTDPlatform platform, string key) {
            this.platform = platform;
            this.key = key;
        }
    }

    [Serializable]
    public enum DTDPlatform {
        Android,
        IOS,
        MacOS,
        WindowsStandalone,
        UWP
    }

    [Serializable]
    public enum DTDEditorWindow {
        Choice,
        Analytics,
        Config
    }
}