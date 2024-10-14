#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using DevToDev.Analytics;

namespace DTDEditor
{
    public class DTDVisualEditorWindow : EditorWindow
    {
        private const string DTD_TITLE = "devtodev";

        private const string DTD_DESCRIPTION =
            "devtodev is a powerful analytical and marketing platform for mobile and web applications. Gather all the data of your application in one simple interface and analyze every bite of it. With devtodev, it is easy to find the weak points, to improve traffic source efficiency and to build strong communications with the customers.";
        private const string DTD_ANALYTICS_TITLE = "Analytics";
        private const string DTD_ANALYTICS_MESSAGE = "\nKey solution to rule your apps.\n";
        private const string DTD_ANALYTICS_DESCRIPTION =
            "devtodev is a powerful all-in-one analytical tool for mobile and web applications. Explore your app metrics in one simple interface that includes teamwork features, game metrics, LTV forecast, and many other cool things.";
        private const string DTD_URL = "https://www.devtodev.com/myapps/";

        [MenuItem("Window/devtodev/Configuration")]
        public static void ShowConfigurationWindow()
        {
            var window =
                (DTDVisualEditorWindow) EditorWindow.GetWindow(typeof(DTDVisualEditorWindow), false, "devtodev", true);
            window.viewModel = new DTDEditorViewModel();
            window.styles = new DTDVisualEditorStyles();
            window.Show();
        }

        private Vector2 scrollPosition;
        private DTDVisualEditorStyles styles;
        private DTDEditorViewModel viewModel;
        private DTDLogLevel logLevel;

        private void OnEnable()
        {
            viewModel = new DTDEditorViewModel();
            styles = new DTDVisualEditorStyles();
        }

        private void OnDisable()
        {
            viewModel?.SaveData();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            switch (viewModel.ActiveWindow)
            {
                case DTDEditorWindow.Choice:
                    DrawChoice();
                    break;
                case DTDEditorWindow.Analytics:
                    DrawAnalytics();
                    break;
                case DTDEditorWindow.Config:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUILayout.EndVertical();
            this.Repaint();
        }

        private void DrawChoice()
        {
            DrawHeader(false);
            DrawSubHeader();
            DrawSplitter();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawAnalyticsBlock();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader(bool isNeedBack)
        {
            EditorGUILayout.BeginVertical(styles.DashboardBackStyle);
            EditorGUILayout.BeginHorizontal(GUIStyle.none);
            if (isNeedBack)
            {
                if (GUILayout.Button("← Back to services", styles.ButtonBackStyle, GUILayout.ExpandWidth(false)))
                {
                    viewModel.ActiveWindow = DTDEditorWindow.Choice;
                }
            }

            EditorGUILayout.LabelField("", styles.DashboardTextStyle, GUILayout.Width(10));

            var content = new GUIContent {text = "Go to Site", image = (Texture) Resources.Load("devtodev/dashboard")};

            if (GUILayout.Button(content, styles.DashboardTextStyle, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL(DTD_URL);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #region Choise

        private void DrawSubHeader()
        {
            EditorGUILayout.BeginVertical(styles.TopStyle);
            EditorGUILayout.LabelField(PlayerSettings.productName, styles.LogoTextStyle);
            EditorGUILayout.LabelField(DTD_TITLE, styles.ServicesStyle);
            EditorGUILayout.LabelField(DTD_DESCRIPTION, styles.MainTextStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();
        }

        private void DrawAnalyticsBlock()
        {
            var blockRect = EditorGUILayout.BeginVertical(styles.BlockBackStyle);

            var content = new GUIContent
            {
                text = DTD_ANALYTICS_TITLE, image = (Texture) Resources.Load("devtodev/analytics")
            };
            EditorGUILayout.LabelField(content, styles.BlockLogoStyle);
            EditorGUILayout.BeginHorizontal(GUIStyle.none);
            EditorGUILayout.LabelField(DTD_ANALYTICS_MESSAGE, styles.BlockTextStyle);
            var switchRect = styles.MakeButton(viewModel.IsAnalyticsEnabled,
                delegate { viewModel.IsAnalyticsEnabled = !viewModel.IsAnalyticsEnabled; });
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (blockRect.Contains(Event.current.mousePosition)
                && !switchRect.Contains(Event.current.mousePosition)
                && Event.current.type == EventType.MouseUp)
            {
                viewModel.ActiveWindow = DTDEditorWindow.Analytics;
            }

            DrawSplitter();
        }

        #endregion

        #region Analytics

        private void DrawAnalytics()
        {
            DrawHeader(true);
            EditorGUILayout.BeginVertical(styles.BlockBackStyle);
            EditorGUILayout.LabelField("");
            var content = new GUIContent {text = "ANALYTICS", image = (Texture) Resources.Load("devtodev/analytics")};
            EditorGUILayout.LabelField(content, styles.BlockLogoStyle);
            EditorGUILayout.BeginHorizontal(GUIStyle.none);
            EditorGUILayout.LabelField(DTD_ANALYTICS_MESSAGE, styles.BlockTextStyle);
            styles.MakeButton(viewModel.IsAnalyticsEnabled,
                delegate { viewModel.IsAnalyticsEnabled = !viewModel.IsAnalyticsEnabled; });
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            DrawSplitter();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.BeginVertical(styles.TopStyle);
            EditorGUILayout.LabelField(DTD_ANALYTICS_TITLE, styles.SideLogoStyle);
            EditorGUILayout.LabelField(DTD_ANALYTICS_DESCRIPTION, styles.MainTextStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Go to Site", styles.ToDashboardButtonStyle))
            {
                Application.OpenURL(DTD_URL);
            }

            EditorGUILayout.EndVertical();

            DrawSplitter();
            DrawPlatformsBlock();
            DrawSplitter();
            DrawLogBlock();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPlatformsBlock()
        {
            DrawPlatformsList();

            var credentials = viewModel.GetPlatformInfo(viewModel.ActivePlatform);

            EditorGUILayout.BeginHorizontal(styles.BlockPlatformStyle);
            EditorGUILayout.LabelField("App key",
                new GUIStyle(styles.MainTextStyle) {padding = new RectOffset(10, 0, 4, 0)}, GUILayout.Width(70));
            var key = EditorGUILayout.TextField(credentials.key, new GUIStyle("textfield"));
            EditorGUILayout.EndHorizontal();

            viewModel.UpdateActivePlatformCredentials(key);
        }

        private void DrawPlatformsList()
        {
            //EditorGUILayout.BeginVertical(Styles.TopStyle);
            EditorGUILayout.LabelField("Supported Platforms", styles.SideLogoStyle);
            EditorGUILayout.BeginHorizontal(new GUIStyle(GUIStyle.none) {padding = new RectOffset(20, 15, 10, 0)});
            var platforms = Enum.GetValues(typeof(DTDPlatform));
            for (var i = 0; i < platforms.Length; i++)
            {
                var platform = (DTDPlatform) platforms.GetValue(i);
                if (GUILayout.Button(platform.ToString(),
                    styles.PlatformButtonStyle(viewModel.ActivePlatform == platform)))
                {
                    viewModel.ActivePlatform = platform;
                    GUI.FocusControl("");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLogBlock()
        {
            EditorGUILayout.BeginVertical(styles.BlockBackStyle);
            EditorGUILayout.BeginHorizontal(GUIStyle.none);
            var logStyle = new GUIStyle(styles.SideLogoStyle);
            logStyle.padding.top -= 25;
            EditorGUILayout.LabelField("Log level", new GUIStyle(styles.MainTextStyle));
            logLevel = (DTDLogLevel) EditorGUILayout.EnumPopup(logLevel);
            viewModel.LogLevel = logLevel;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion
        
        private void DrawSplitter()
        {
            GUILayout.Label(new GUIContent(), styles.SplitterStyle);
        }
    }
}
#endif