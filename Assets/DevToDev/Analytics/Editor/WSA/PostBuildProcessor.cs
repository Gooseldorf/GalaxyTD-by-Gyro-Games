using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace DevToDev.Editor.WSA
{
    public static class PostBuildProcessor
    {
        private const string PROJECT_FILE_EXTENSION = ".vcxproj";

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.WSAPlayer) return;
#if UNITY_2019_2_OR_NEWER
            if (EditorUserBuildSettings.wsaUWPBuildType == WSAUWPBuildType.ExecutableOnly)
            {
                Debug.LogError(
                    $"[{nameof(PostBuildProcessor)}] Can't include DevToDev.Background service to project. Please change build type in build settings.");

                return;
            }
#endif
            try
            {
                var pathToProjectFile = Path.Combine(
                    pathToBuiltProject,
                    PlayerSettings.productName,
                    PlayerSettings.productName + PROJECT_FILE_EXTENSION);
                var doc = XDocument.Load(pathToProjectFile);
                XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
                var projectElement = doc.Descendants(ns + "Project").First();
                var itemGroup = new XElement(ns + "ItemGroup");
                var reference = new XElement(ns + "Reference");
                reference.SetAttributeValue("Include", "DevToDev.Background");
                reference.Add(
                    new XElement(ns + "HintPath", @"Managed\DevToDev.Background.winmd"),
                    new XElement(ns + "IsWinMDFile", "true"));
                itemGroup.Add(reference);
                projectElement.Add(itemGroup);
                doc.Save(pathToProjectFile);
                Debug.Log($"[{nameof(PostBuildProcessor)}] Project file has been edited successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(PostBuildProcessor)}] {ex.GetType()}\n{ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}