using System.IO;
using UnityEditor;
using UnityEngine;
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

                if (!string.IsNullOrEmpty(path)) 
                {
                    // serialize
                    foreach (var manager in tromboneBackground.gameObject.GetComponentsInChildren<TromboneEventManager>())
                    {
                        manager.SerializeAllGenericEvents();
                    }
                    
                    string fileName = Path.GetFileName(path);
                    string folderPath = Path.GetDirectoryName(path);

                    PrefabUtility.SaveAsPrefabAsset(tromboneBackground.gameObject, "Assets/_Background.prefab");
                    AssetBundleBuild assetBundleBuild = default;
                    assetBundleBuild.assetBundleName = fileName;
                    assetBundleBuild.assetNames = new string[] { "Assets/_Background.prefab" };

                    BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { assetBundleBuild }, BuildAssetBundleOptions.ForceRebuildAssetBundle, EditorUserBuildSettings.activeBuildTarget);
                    EditorPrefs.SetString("currentBuildingAssetBundlePath", folderPath);
                    EditorUserBuildSettings.SwitchActiveBuildTarget(selectedBuildTargetGroup, activeBuildTarget);

                    AssetDatabase.DeleteAsset("Assets/_Background.prefab");

                    if (File.Exists(path))
                        File.Delete(path);

                    // Unity seems to save the file in lower case, which is a problem on Linux, as file systems are case sensitive there
                    File.Move(Path.Combine(Application.temporaryCachePath, fileName.ToLowerInvariant()), path);

                    AssetDatabase.Refresh();

                    EditorUtility.DisplayDialog("Exportation Successful!", "Exportation Successful!", "OK");
                    
                    GUIUtility.ExitGUI(); 
                }
                else 
                {
                    EditorUtility.DisplayDialog("Exportation Failed!", "Path is invalid.", "OK");
                    
                    GUIUtility.ExitGUI(); 
                }

            }
        }
    }
}
