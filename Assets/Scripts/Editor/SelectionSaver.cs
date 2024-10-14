using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using Newtonsoft.Json;

public class SelectionSaver 
{
    const string SELECTION_PATH = "Main Menu/Edit/Selection/";

    //[InitializeOnLoadMethod]
    //static void SaveHotkeys()
    //{
    //    foreach (var item in ShortcutManager.instance.GetAvailableShortcutIds())
    //    {
    //        if (item.StartsWith(SELECTION_PATH))
    //        {
    //            ShortcutManager.instance.RebindShortcut(item, new ShortcutBinding());
    //        }
    //    }
    //}

    [Shortcut("save_selection_1", KeyCode.Alpha1, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 1")]
    static void SaveSelection1()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("1", json);
    }

    [Shortcut("load_selection_1", KeyCode.Alpha1, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 1")]
    static void LoadSelection1()
    {
        string json = EditorPrefs.GetString("1");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("1", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }



    [Shortcut("save_selection_2", KeyCode.Alpha2, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 2")]
    static void SaveSelection2()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("2", json);
    }

    [Shortcut("load_selection_2", KeyCode.Alpha2, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 2")]
    static void LoadSelection2()
    {
        string json = EditorPrefs.GetString("2");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("2", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }



    [Shortcut("save_selection_3", KeyCode.Alpha3, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 3")]
    static void SaveSelection3()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("3", json);
    }

    [Shortcut("load_selection_3", KeyCode.Alpha3, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 3")]
    static void LoadSelection3()
    {
        string json = EditorPrefs.GetString("3");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("3", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }




    [Shortcut("save_selection_4", KeyCode.Alpha4, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 4")]
    static void SaveSelection4()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("4", json);
    }

    [Shortcut("load_selection_4", KeyCode.Alpha4, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 4")]
    static void LoadSelection4()
    {
        string json = EditorPrefs.GetString("4");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("4", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }



    [Shortcut("save_selection_5", KeyCode.Alpha5, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 5")]
    static void SaveSelection5()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("5", json);
    }

    [Shortcut("load_selection_5", KeyCode.Alpha5, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 5")]
    static void LoadSelection5()
    {
        string json = EditorPrefs.GetString("5");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("5", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }




    [Shortcut("save_selection_6", KeyCode.Alpha6, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 6")]
    static void SaveSelection6()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("6", json);
    }

    [Shortcut("load_selection_6", KeyCode.Alpha6, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 6")]
    static void LoadSelection6()
    {
        string json = EditorPrefs.GetString("6");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("6", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }




    [Shortcut("save_selection_7", KeyCode.Alpha7, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 7")]
    static void SaveSelection7()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("7", json);
    }

    [Shortcut("load_selection_7", KeyCode.Alpha7, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 7")]
    static void LoadSelection7()
    {
        string json = EditorPrefs.GetString("7");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("7", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }




    [Shortcut("save_selection_8", KeyCode.Alpha8, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 8")]
    static void SaveSelection8()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("8", json);
    }

    [Shortcut("load_selection_8", KeyCode.Alpha8, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 8")]
    static void LoadSelection8()
    {
        string json = EditorPrefs.GetString("8");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("8", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }




    [Shortcut("save_selection_9", KeyCode.Alpha9, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 9")]
    static void SaveSelection9()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("9", json);
    }

    [Shortcut("load_selection_9", KeyCode.Alpha9, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 9")]
    static void LoadSelection9()
    {
        string json = EditorPrefs.GetString("9");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("9", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }





    [Shortcut("save_selection_0", KeyCode.Alpha0, ShortcutModifiers.Control | ShortcutModifiers.Alt, displayName = "Save Selection 0")]
    static void SaveSelection0()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return;

        string json = JsonConvert.SerializeObject(Selection.assetGUIDs);
        if (string.IsNullOrEmpty(json)) return;

        EditorPrefs.SetString("0", json);
    }

    [Shortcut("load_selection_0", KeyCode.Alpha0, ShortcutModifiers.Control | ShortcutModifiers.Shift, displayName = "Load Selection 0")]
    static void LoadSelection0()
    {
        string json = EditorPrefs.GetString("0");
        if (string.IsNullOrEmpty(json)) return;

        string[] assetGuids = JsonConvert.DeserializeObject<string[]>(json);
        if (assetGuids == null || assetGuids.Length == 0) return;

        bool resave = false;
        List<Object> objects = new List<Object>();
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Object with guid ({guid}) no longer exists. It will be resaved");
                resave = true;
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) continue;

            objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        // resave the list of guids without the missing objects, if any
        if (resave)
        {
            EditorPrefs.SetString("0", JsonConvert.SerializeObject(Selection.assetGUIDs));
        }
    }
}
