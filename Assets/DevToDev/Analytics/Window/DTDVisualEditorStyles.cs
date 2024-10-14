#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DTDEditor
{
    internal class DTDVisualEditorStyles
    {
        private GUIStyle onStateStyle;

        private GUIStyle OnStateStyle =>
            onStateStyle ?? (onStateStyle = CreateStateStyle(new Color(0.098f, 0.8f, 0.97f)));

        private GUIStyle offStateStyle;
        public GUIStyle OffStateStyle => offStateStyle ?? (offStateStyle = CreateStateStyle(Color.black));
        private GUIStyle backButtonStyle;

        public GUIStyle ButtonBackStyle =>
            backButtonStyle ?? (backButtonStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                normal = {textColor = Color.white},
                stretchWidth = false,
                stretchHeight = false,
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                fixedWidth = 130
            });

        private GUIStyle splitterStyle;

        public GUIStyle SplitterStyle =>
            splitterStyle ?? (splitterStyle = new GUIStyle
            {
                normal = {background = MakeTex(1, 1, new Color(197 / 255.0f, 197 / 255.0f, 197 / 255.0f))},
                stretchWidth = true,
                fixedHeight = 2,
                fixedWidth = 0
            });

        private GUIStyle dashboardBackStyle;

        public GUIStyle DashboardBackStyle =>
            dashboardBackStyle ?? (dashboardBackStyle = new GUIStyle("Label")
            {
                normal = {background = MakeTex(1, 1, new Color(0.35f, 0.37f, 0.38f))},
                padding = new RectOffset(0, 0, -1, -1)
            });

        private GUIStyle blockBackStyle;

        public GUIStyle BlockBackStyle =>
            blockBackStyle ?? (blockBackStyle = new GUIStyle("Label")
            {
                normal = {background = MakeTex(1, 1, new Color(0.95f, 0.95f, 0.95f))}
            });

        private GUIStyle blockLogoStyle;

        public GUIStyle BlockLogoStyle =>
            blockLogoStyle ??
            (blockLogoStyle = TextStyle(20, new[] {20, 2, 0, 0}, new Color(0, 0.75f, 0.96f)));

        private GUIStyle mainTextStyle;

        public GUIStyle MainTextStyle =>
            mainTextStyle ?? (mainTextStyle = TextStyle(12, new[] {20, 0, 50, 25}, Color.black));

        private GUIStyle blockTextStyle;

        public GUIStyle BlockTextStyle =>
            blockTextStyle ?? (blockTextStyle = TextStyle(12, new[] {20, -15, 0, 0}, Color.black));

        private GUIStyle logoTextStyle;

        public GUIStyle LogoTextStyle =>
            logoTextStyle ?? (logoTextStyle = TextStyle(26, new[] {20, 0, 0, 0}, Color.black));

        private GUIStyle servicesTextStyle;

        public GUIStyle ServicesStyle => servicesTextStyle ??
                                         (servicesTextStyle = TextStyle(18, new[] {20, -5, 0, 0}, Color.black));

        private GUIStyle sideLogoStyle;

        internal GUIStyle SideLogoStyle =>
            sideLogoStyle ?? (sideLogoStyle = TextStyle(22, new[] {20, 20, 0, 0}, Color.black));

        private GUIStyle dashboardTextStyle;

        public GUIStyle DashboardTextStyle =>
            dashboardTextStyle ?? (dashboardTextStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontStyle = FontStyle.Normal,
                fontSize = 10,
                imagePosition = ImagePosition.ImageLeft,
                normal = {textColor = Color.white},
                alignment = TextAnchor.MiddleRight,
                fixedWidth = 130
            });

        private GUIStyle platformInactiveStyle;

        private GUIStyle PlatformInactiveStyle =>
            platformInactiveStyle ??
            (platformInactiveStyle = BigButtonStyle(false, new[] {0, 0, 5, 2}, -1, 22));

        private GUIStyle platformActiveStyle;

        private GUIStyle PlatformActiveStyle =>
            platformActiveStyle ??
            (platformActiveStyle = BigButtonStyle(true, new[] {0, 0, 5, 2}, -1, 22));

        private GUIStyle toDashboardButtonStyle;

        public GUIStyle ToDashboardButtonStyle =>
            toDashboardButtonStyle ??
            (toDashboardButtonStyle = BigButtonStyle(true, new[] {27, 0, 0, 30}, 140, 35));

        private GUIStyle textFieldStyle;

        private GUIStyle topStyle;

        public GUIStyle TopStyle =>
            topStyle ?? (topStyle = new GUIStyle("TextField")
            {
                normal = {background = MakeTex(1, 1, new Color(0.89f, 0.898f, 0.89f))}
            });

        private GUIStyle blockPlatformStyle;

        public GUIStyle BlockPlatformStyle =>
            blockPlatformStyle ?? (blockPlatformStyle = new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(20, 20, 0, 0),
                fixedHeight = 38,
                normal = {background = MakeTex(1, 1, new Color(0.89f, 0.898f, 0.89f))}
            });

        internal Rect MakeButton(bool isOn, Action onClickAction)
        {
            var button = new GUIStyle(GUIStyle.none) {alignment = TextAnchor.LowerRight, padding = {right = 20}};

            var buttonPositions =
                EditorGUILayout.BeginHorizontal(button, GUILayout.Width(70), GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("On", isOn ? OnStateStyle : OffStateStyle, GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("|", OffStateStyle, GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("Off", !isOn ? OnStateStyle : OffStateStyle, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                buttonPositions.Contains(Event.current.mousePosition))
            {
                onClickAction?.Invoke();
            }

            return buttonPositions;
        }

        internal GUIStyle PlatformButtonStyle(bool isActivePlatform) => isActivePlatform ? PlatformActiveStyle : PlatformInactiveStyle;

        private static GUIStyle CreateStateStyle(Color color)
        {
            var style = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                wordWrap = true,
                fontStyle = FontStyle.Normal,
                fontSize = 10,
                alignment = TextAnchor.UpperLeft,
                normal = {textColor = color}
            };
            return style;
        }

        private GUIStyle BigButtonStyle(bool isActive, IReadOnlyList<int> paddings, int width, int height)
        {
            var buttonStyle = new GUIStyle("Button");

            Texture2D backgroundTexture;
            Color textColor;
            if (isActive)
            {
                backgroundTexture = MakeTex(1, 1, new Color(0.0f, 0.75f, 0.96f));
                textColor = Color.white;
            }
            else
            {
                backgroundTexture = MakeTex(1, 1, Color.white);
                textColor = Color.black;
            }

            buttonStyle.normal.background = backgroundTexture;
            buttonStyle.active.background = backgroundTexture;
            buttonStyle.onActive.background = backgroundTexture;
            buttonStyle.onNormal.background = backgroundTexture;
            buttonStyle.onHover.background = backgroundTexture;
            buttonStyle.hover.background = backgroundTexture;
            buttonStyle.normal.textColor = textColor;
            buttonStyle.active.textColor = textColor;
            buttonStyle.onActive.textColor = textColor;
            buttonStyle.onNormal.textColor = textColor;
            buttonStyle.onHover.textColor = textColor;
            buttonStyle.hover.textColor = textColor;
            buttonStyle.margin.left = paddings[0];
            buttonStyle.margin.top = paddings[1];
            buttonStyle.margin.right = paddings[2];
            buttonStyle.margin.bottom = paddings[3];
            if (width >= 0)
            {
                buttonStyle.fixedWidth = width;
            }

            if (height >= 0)
            {
                buttonStyle.fixedHeight = height;
            }

            return buttonStyle;
        }

        private static GUIStyle TextStyle(int fontSize, IReadOnlyList<int> paddings, Color color)
        {
            var style = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                wordWrap = true,
                fontStyle = FontStyle.Normal,
                fontSize = fontSize,
                padding = {left = paddings[0], top = paddings[1], right = paddings[2], bottom = paddings[3]},
                normal = {textColor = color},
                alignment = TextAnchor.UpperLeft
            };
            return style;
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];

            for (var i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
#endif