using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABS
{
    public class FrameSelector : ScriptableWizard
    {
        public static FrameSelector instance;

        private Studio studio = null;

        Vector2 whatPos = Vector2.zero;

        void OnEnable()
        {
			minSize = new Vector2(400f, 300f);
            instance = this;
        }

        void OnDisable()
        {
            instance = null;

            if (AnimationPreviewer.instance != null)
                AnimationPreviewer.instance.Close();
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

                int texWidth = sampledTextures[0].width;
                int texHeight = sampledTextures[0].height;

                float padding = 10.0f;
                int colSize = Mathf.FloorToInt(Screen.width / (texWidth + padding));
                if (colSize < 1)
                    colSize = 1;

                if (sampledTextures.Length > 1)
                {
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("Select all"))
                    {
                        selectedFrames.Clear();
                        for (int fi = 0; fi < frameTimes.Length; ++fi)
                            selectedFrames.Add(new Frame(fi, frameTimes[fi]));
                    }

                    if (GUILayout.Button("Clear all"))
                        selectedFrames.Clear();

                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(padding);

                whatPos = GUILayout.BeginScrollView(whatPos);
                {
                    const float LABEL_HEIGHT = 20.0f;

                    Rect rect = new Rect(padding, 0, texWidth, texHeight);

                    int col = 0;
                    int rowCount = 0;
                    for (int fi = 0; fi < sampledTextures.Length; ++fi)
                    {
                        Texture2D tex = sampledTextures[fi];

                        if (col >= colSize)
                        {
                            col = 0;
                            rowCount++;

                            rect.x = padding;
                            rect.y += texHeight + padding + LABEL_HEIGHT;

                            GUILayout.EndHorizontal();
                            GUILayout.Space(texHeight + padding);
                        }

                        if (col == 0)
                            GUILayout.BeginHorizontal();

                        if (GUI.Button(rect, ""))
                        {
                            float ftime = frameTimes[fi];

                            if (selectedFrames.Count == 0)
                            {
                                selectedFrames.Add(new Frame(fi, ftime));
                            }
                            else
                            {
                                bool exist = false;
                                foreach (Frame frame in selectedFrames)
                                {
                                    if (fi == frame.index)
                                    {
                                        exist = true;
                                        break;
                                    }
                                }

                                int inserti = 0;
                                for (; inserti < selectedFrames.Count; ++inserti)
                                {
                                    if (fi < selectedFrames[inserti].index)
                                        break;
                                }

                                if (exist)
                                    selectedFrames.Remove(new Frame(fi, 0));
                                else
                                    selectedFrames.Insert(inserti, new Frame(fi, ftime));
                            }
                        }

                        EditorGUI.DrawTextureTransparent(rect, tex);

                        foreach (Frame frame in selectedFrames)
                        {
                            if (frame.index == fi)
                            {
                                DrawingHelper.StrokeRect(rect, Color.red, 2.0f);
                                break;
                            }
                        }

                        GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
                        GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);
                        GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, LABEL_HEIGHT),
                            fi.ToString(), "ProgressBarBack");
                        GUI.contentColor = Color.white;
                        GUI.backgroundColor = Color.white;

                        col++;
                        rect.x += texWidth + padding;
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.Space(texHeight + padding);

                    GUILayout.Space(rowCount * 26);
                }
                GUILayout.EndScrollView();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Close();
            }
        }
    }
}
