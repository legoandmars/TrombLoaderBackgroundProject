using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using TrombLoader.Data;

namespace TrombLoader 
{
    [CustomEditor(typeof(Camera))]
    public class BackgroundEditor : Editor 
    {
        Camera tromboneBackground;

        private void OnEnable() 
        {
            tromboneBackground = (Camera)target;
        }

        public override void OnInspectorGUI() 
        {
            DrawDefaultInspector();

            GUILayout.Space(20);

            if (GUILayout.Button("Export for TrombLoader")) 
            {
                string path = EditorUtility.SaveFilePanel("Save Trombone Background", string.Empty,  "bg.trombackground", "trombackground");

                BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

                GameObject clonedTromboneBackground = null;

                try
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        clonedTromboneBackground = Instantiate(tromboneBackground.gameObject);

                        // serialize
                        foreach (var manager in clonedTromboneBackground.gameObject.GetComponentsInChildren<TromboneEventManager>())
                        {
                            manager.SerializeAllGenericEvents();
                        }

                        string fileName = Path.GetFileName(path);
                        string folderPath = Path.GetDirectoryName(path);

                        int serializedCount = 0;
                        // serialize video clips because unity REALLY does not like making them work in assetbundles
                        foreach (var videoPlayer in clonedTromboneBackground.gameObject.GetComponentsInChildren<VideoPlayer>())
                        {
                            if (videoPlayer.clip != null)
                            {
                                // handle VideoClip.originalClip returning an invalid file extension sometimes
                                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(videoPlayer.clip.originalPath)))
                                {
                                    if (videoPlayer.clip == null) break;

                                    if (!file.EndsWith(".meta") && Path.GetFileNameWithoutExtension(file) == Path.GetFileNameWithoutExtension(videoPlayer.clip.originalPath))
                                    {
                                        videoPlayer.clip = null;
                                        videoPlayer.url = $"SERIALIZED_OUTSIDE_BUNDLE/SERIALIZED_{serializedCount}_" + Path.GetFileName(file);

                                        var newVideoPath = Path.Combine(folderPath, $"SERIALIZED_{serializedCount}_" + Path.GetFileName(file));
                                        
                                        if (File.Exists(newVideoPath)) File.Delete(newVideoPath);
                                        File.Copy(file, newVideoPath);

                                        serializedCount++;
                                        break;
                                    }
                                }
                            }
                        }

                        // serialize tromboners (this one is not unity's fault, it's base game weirdness)
                        List<string> trombonePaths = new List<string>(){"Assets/_Background.prefab"};
                        
                        int instanceID = 0;
                        
                        foreach (var tromboner in clonedTromboneBackground.gameObject.GetComponentsInChildren<TrombonerPlaceholder>())
                        {
                            tromboner.InstanceID = instanceID;
                            tromboner.gameObject.name = $"_{instanceID}";
                            
                            instanceID++;
                        }
                        
                        PrefabUtility.SaveAsPrefabAsset(clonedTromboneBackground.gameObject, "Assets/_Background.prefab");
                        AssetBundleBuild assetBundleBuild = default;
                        assetBundleBuild.assetBundleName = fileName;
                        assetBundleBuild.assetNames = trombonePaths.ToArray();

                        BuildPipeline.BuildAssetBundles(Application.temporaryCachePath,
                            new AssetBundleBuild[] {assetBundleBuild}, BuildAssetBundleOptions.ForceRebuildAssetBundle,
                            EditorUserBuildSettings.activeBuildTarget);
                        EditorPrefs.SetString("currentBuildingAssetBundlePath", folderPath);
                        EditorUserBuildSettings.SwitchActiveBuildTarget(selectedBuildTargetGroup, activeBuildTarget);
                        
                        foreach (var asset in trombonePaths)
                        {
                            AssetDatabase.DeleteAsset(asset);
                        }

                        if (File.Exists(path)) File.Delete(path);

                        // Unity seems to save the file in lower case, which is a problem on Linux, as file systems are case sensitive there
                        File.Move(Path.Combine(Application.temporaryCachePath, fileName.ToLowerInvariant()), path);

                        AssetDatabase.Refresh();

                        EditorUtility.DisplayDialog("Exportation Successful!", "Exportation Successful!", "OK");

                        if (clonedTromboneBackground != null) DestroyImmediate(clonedTromboneBackground);

                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Exportation Failed!", "Path is invalid.", "OK");

                        GUIUtility.ExitGUI();
                    }
                }
                catch
                {
                    if(clonedTromboneBackground != null) DestroyImmediate(clonedTromboneBackground);
                }
            }
        }
    }
}
