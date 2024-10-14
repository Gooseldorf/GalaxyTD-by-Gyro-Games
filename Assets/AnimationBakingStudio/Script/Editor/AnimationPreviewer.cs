using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABS
{
    public class AnimationPreviewer : ScriptableWizard
    {
        public static AnimationPreviewer instance;

        private Studio studio = null;

        private bool isPlaying = false;
        private bool isLooping = true;
        private int indexOfselectedFrames = 0;
        private int frameNumber = 0;
        private float nextFrameTime = 0f;
        private float nextSpriteTime = 0f;

        const float MIN_EDITOR_WIDTH = 220f;

        public void OnEnable()
        {
            instance = this;

            EditorApplication.update -= UpdateState;
            EditorApplication.update += UpdateState;

            Reset();
            isPlaying = true;
        }

        public void OnDisable()
        {
            instance = null;

            EditorApplication.update -= UpdateState;

            if (FrameSelector.instance != null)
                FrameSelector.instance.Close();
        }

        public void SetInfo(Studio studio)
        {
            this.studio = studio;
        }

        void OnGUI()
        {
            try
            {
                if (studio == null)
                    return;

                Sampling sampling = studio.sampling;
                if (sampling == null)
                    return;

                Texture2D[] sampledTextures = sampling.trimMainTextures;
                float[] frameTimes = sampling.frameTimes;
                List<Frame> selectedFrames = sampling.selectedFrames;

                if (selectedFrames.Count == 0)
                    return;

                const float BUTTOM_HEIGHT = 18f;
                const float MENU_HEIGHT = 60f;
                const float MARGIN = 2f;

                int spriteWidth = sampledTextures[0].width;
                int spriteHeight = sampledTextures[0].height;

                const float FRAME_LABEL_WIDTH = 30f;

                float editorWidth = Mathf.Max(spriteWidth + (MARGIN + FRAME_LABEL_WIDTH) * 2f, MIN_EDITOR_WIDTH);

                position = new Rect(position.x, position.y, editorWidth, MENU_HEIGHT + (float)spriteHeight + MARGIN);

                EditorGUI.BeginChangeCheck();
                {
                    DrawFrameRateField(ref studio.output.frameRate);
                    DrawIntervalField(ref studio.output.frameInterval);
                }
                if (EditorGUI.EndChangeCheck())
                    Reset();

                const float BUTTONS_Y = MENU_HEIGHT - 20f;

                Rect playRect = new Rect(MARGIN, BUTTONS_Y, editorWidth / 2f - MARGIN, BUTTOM_HEIGHT);
                EditorGUI.BeginChangeCheck();
                isPlaying = GUI.Toggle(playRect, isPlaying, "Play", GUI.skin.button);
                if (EditorGUI.EndChangeCheck())
                {
                    if (isPlaying)
                    {
                        Reset();
                        isPlaying = true;
                    }
                    else
                    {
                        isPlaying = false;
                    }
                }

                Rect loopRect = new Rect(MARGIN + editorWidth / 2f, BUTTONS_Y, editorWidth / 2f - MARGIN, BUTTOM_HEIGHT);
                isLooping = GUI.Toggle(loopRect, isLooping, "Loop", GUI.skin.button);

                float spritePosX = MARGIN + FRAME_LABEL_WIDTH;
                if (spriteWidth < editorWidth)
                    spritePosX = (editorWidth - spriteWidth) / 2f;
                float spritePosY = MENU_HEIGHT;

                Rect spriteRect = new Rect(spritePosX, spritePosY, spriteWidth, spriteHeight);
                EditorGUI.DrawTextureTransparent(spriteRect, sampledTextures[frameNumber]);
                DrawingHelper.StrokeRect(spriteRect, Color.black, 1f);

                Rect frameLabelRect = new Rect(1.0f, spritePosY, FRAME_LABEL_WIDTH, 15f);
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                EditorGUI.LabelField(frameLabelRect, frameNumber.ToString(), labelStyle);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.update -= UpdateState;
                Close();
            }
        }

        static public void DrawFrameRateField(ref int frameRate)
        {
            frameRate = EditorGUILayout.IntField("Frame Rate", frameRate, GUILayout.Width(MIN_EDITOR_WIDTH - 10f));
            if (frameRate <= 0)
                frameRate = 1;
        }

        static public void DrawIntervalField(ref int frameInterval)
        {
            frameInterval = EditorGUILayout.IntField("Interval", frameInterval);
            if (frameInterval <= 0)
                frameInterval = 1;
        }

        public void UpdateState()
        {
            try
            {
                if (studio == null)
                    return;

                Sampling sampling = studio.sampling;
                if (sampling == null)
                    return;

                List<Frame> selectedFrames = sampling.selectedFrames;

                if (!isPlaying)
                    return;

                if (Time.realtimeSinceStartup > nextFrameTime)
                {
                    float unitTime = 1f / studio.output.frameRate;
                    nextFrameTime += unitTime;

                    if (Time.realtimeSinceStartup > nextSpriteTime && selectedFrames.Count > 0)
                    {
                        indexOfselectedFrames = (indexOfselectedFrames + 1) % selectedFrames.Count;
                        frameNumber = selectedFrames[indexOfselectedFrames].index;
                        Repaint();

                        if (indexOfselectedFrames >= selectedFrames.Count - 1)
                        {
                            if (!isLooping)
                            {
                                isPlaying = false;
                                return;
                            }
                        }

                        nextSpriteTime += unitTime * studio.output.frameInterval;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.update -= UpdateState;
                Close();
            }
        }

        private void Reset()
        {
            indexOfselectedFrames = 0;
            frameNumber = 0;
            nextFrameTime = Time.realtimeSinceStartup;
            nextSpriteTime = Time.realtimeSinceStartup;
        }
    }
}
