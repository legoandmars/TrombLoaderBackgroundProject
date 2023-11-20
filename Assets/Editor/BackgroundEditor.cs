using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using TrombLoader.Data;

namespace TrombLoader 
{
    public class MacShaderPicker : EditorWindow
    {
        private Dictionary<Shader, bool> ShaderList = new Dictionary<Shader, bool>();
        private string FolderPath;
        public bool HasBuilt = false;

        Vector2 scrollPosition;

        public void Init(List<Shader> shaders, string path)
        {
            ShaderList.Clear();
            foreach (var shader in shaders)
            {
                // TODO: store the state in a file or something and load from that
                ShaderList[shader] = true;
            }
            FolderPath = path;
        }

        public void OnGUI()
        {
            GUILayout.Label("Shaders to include:");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < ShaderList.Count; i++)
            {
                var shader = ShaderList.ElementAt(i).Key;
                var active = ShaderList.ElementAt(i).Value;
                ShaderList[shader] = GUILayout.Toggle(active, shader.name);
            }

            GUILayout.EndScrollView();

            GUILayout.Box(
                "To make sure your background appears correctly on macOS we need to build a bundle containing Mac-compatible shaders. " + 
                "To save on disk space, you can exclude shaders you know you aren't using in your scene. " +
                "If you're not sure what to exclude, you can just leave everything selected at the expense of a slightly larger shader bundle."
            );

            var btnName = "Continue";

            if (ShaderList.ContainsValue(true))
            {
                btnName = "Build Them Shaders!";
            } else
            {
                btnName = "Continue Without Building Shaders";
            }

            if (GUILayout.Button(btnName))
            {
                // TODO: react when the window is closed by the user
                this.Close();
                if (ShaderList.ContainsValue(true))
                {
                    BuildShaders();
                    HasBuilt = true;
                } else {
                    HasBuilt = false;
                }
            }
        }

        void BuildShaders()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);

            GameObject shaderParent = new GameObject("_Shaders");
            if (!AssetDatabase.IsValidFolder("Assets/SerializedMaterials"))
            {
                AssetDatabase.CreateFolder("Assets", "SerializedMaterials");
            }
            for (int i = 0; i < ShaderList.Count; i++)
            {
                if (ShaderList.ElementAt(i).Value)
                {
                    continue;
                }
                var shader = ShaderList.ElementAt(i).Key;

                var mat = new Material(shader);

                AssetDatabase.CreateAsset(mat, "Assets/SerializedMaterials/" + i + ".mat");

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                
                cube.name = i.ToString();
                cube.transform.SetParent(shaderParent.transform);
                cube.GetComponent<Renderer>().sharedMaterial = mat;
            }
            
            string shaderFileName = Path.GetFileName("ShaderCache_OSX.DONOTDELETE");
            string shaderPath = Path.Combine(FolderPath, shaderFileName);

            PrefabUtility.SaveAsPrefabAsset(shaderParent, "Assets/_Shaders.prefab");
            AssetBundleBuild shaderBundleBuild = default;
            shaderBundleBuild.assetBundleName = shaderFileName;
            shaderBundleBuild.assetNames = new string[] {"Assets/_Shaders.prefab"};

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath,
                new AssetBundleBuild[] {shaderBundleBuild}, BuildAssetBundleOptions.ForceRebuildAssetBundle,
                BuildTarget.StandaloneOSX);

            AssetDatabase.DeleteAsset("Assets/_Shaders.prefab");

            if (File.Exists(shaderPath)) File.Delete(shaderPath);

            File.Move(Path.Combine(Application.temporaryCachePath, shaderFileName), shaderPath);

            DestroyImmediate(shaderParent);

            AssetDatabase.DeleteAsset("Assets/SerializedMaterials");

            AssetDatabase.Refresh();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }
    }
    [CustomEditor(typeof(Camera))]
    public class BackgroundEditor : Editor 
    {
        Camera tromboneBackground;

        // yoinked from https://github.com/tc-mods/TrombLoader/blob/9469e0593896eafb7a927847aa1bd8899ad781d5/Helpers/ShaderHelper.cs#L36
        public List<string> BaseGameShaderNames = new List<string>
        {
            "Custom/WavySpriteLit", "Custom/WavySpriteUnlit", "FX/Flare", "FX/Gem", "GUI/Text Shader",
            "Hidden/BlitCopy", "Hidden/BlitCopyDepth", "Hidden/BlitCopyWithDepth", "Hidden/BlitToDepth",
            "Hidden/BlitToDepth/MSAA", "Hidden/Compositing", "Hidden/ConvertTexture", "Hidden/CubeBlend",
            "Hidden/CubeBlur", "Hidden/CubeCopy", "Hidden/FrameDebuggerRenderTargetDisplay", "Hidden/Internal-Colored",
            "Hidden/Internal-CombineDepthNormals", "Hidden/Internal-CubemapToEquirect",
            "Hidden/Internal-DeferredReflections", "Hidden/Internal-DeferredShading",
            "Hidden/Internal-DepthNormalsTexture", "Hidden/Internal-Flare", "Hidden/Internal-GUIRoundedRect",
            "Hidden/Internal-GUIRoundedRectWithColorPerBorder", "Hidden/Internal-GUITexture",
            "Hidden/Internal-GUITextureBlit", "Hidden/Internal-GUITextureClip", "Hidden/Internal-GUITextureClipText",
            "Hidden/Internal-Halo", "Hidden/Internal-MotionVectors", "Hidden/Internal-ODSWorldTexture",
            "Hidden/Internal-PrePassLighting", "Hidden/Internal-ScreenSpaceShadows", "Hidden/Internal-StencilWrite",
            "Hidden/Internal-UIRAtlasBlitCopy", "Hidden/Internal-UIRDefault", "Hidden/InternalClear",
            "Hidden/InternalErrorShader", "Hidden/Post FX/Ambient Occlusion", "Hidden/Post FX/Blit",
            "Hidden/Post FX/Bloom", "Hidden/Post FX/Builtin Debug Views", "Hidden/Post FX/Depth Of Field",
            "Hidden/Post FX/Eye Adaptation", "Hidden/Post FX/Fog", "Hidden/Post FX/FXAA",
            "Hidden/Post FX/Grain Generator", "Hidden/Post FX/Lut Generator", "Hidden/Post FX/Motion Blur",
            "Hidden/Post FX/Screen Space Reflection", "Hidden/Post FX/Temporal Anti-aliasing",
            "hidden/SuperSystems/Wireframe-Global", "hidden/SuperSystems/Wireframe-Shaded-Unlit-Global",
            "hidden/SuperSystems/Wireframe-Transparent-Culled-Global",
            "hidden/SuperSystems/Wireframe-Transparent-Global", "Hidden/TextCore/Distance Field SSD",
            "Hidden/VideoComposite", "Hidden/VideoDecode", "Hidden/VideoDecodeOSX",
            "Hidden/VR/BlitFromTex2DToTexArraySlice", "Hidden/VR/BlitTexArraySlice", "Legacy Shaders/Diffuse",
            "Legacy Shaders/Particles/Additive", "Legacy Shaders/Particles/Alpha Blended Premultiply",
            "Legacy Shaders/Particles/Alpha Blended", "Legacy Shaders/Transparent/VertexLit",
            "Legacy Shaders/VertexLit", "Mobile/Unlit (Supports Lightmap)", "Particles/Standard Unlit",
            "Skybox/Procedural", "Spaventacorvi/Glitter/Glitter F - Bumped Specular",
            "Spaventacorvi/Holographic/Holo D - Specular Textured", "Sprites/Default", "Sprites/Diffuse",
            "Sprites/Mask", "Standard (Specular setup)", "Standard", "SuperSystems/Wireframe-Transparent-Culled",
            "TextMeshPro/Bitmap Custom Atlas", "TextMeshPro/Bitmap", "TextMeshPro/Distance Field (Surface)",
            "TextMeshPro/Distance Field Overlay", "TextMeshPro/Distance Field", "TextMeshPro/Mobile/Bitmap",
            "TextMeshPro/Mobile/Distance Field (Surface)", "TextMeshPro/Mobile/Distance Field - Masking",
            "TextMeshPro/Mobile/Distance Field Overlay", "TextMeshPro/Mobile/Distance Field", "TextMeshPro/Sprite",
            "UI/Default", "Hidden/Post FX/Uber"
        };

        // just as cursed as above, but hey it still saves on disk space
        public List<string> TrombLoaderShaderNames = new List<string>
        {
            "Hidden/Internal-DeferredShading", "Hidden/Internal-DeferredReflections", "Hidden/Internal-ScreenSpaceShadows",
            "Hidden/Internal-PrePassLighting", "Hidden/Internal-DepthNormalsTexture", "Hidden/Internal-MotionVectors",
            "Hidden/Internal-Halo", "Hidden/Internal-Flare", "Hidden/CubeBlur", "Hidden/CubeCopy", "Hidden/CubeBlend",
            "Hidden/BlitCopy", "Legacy Shaders/Self-Illumin/VertexLit", "Legacy Shaders/Self-Illumin/Diffuse",
            "Legacy Shaders/Reflective/VertexLit", "Legacy Shaders/Reflective/Specular", "Legacy Shaders/Transparent/Diffuse",
            "Legacy Shaders/Transparent/Bumped Diffuse", "Legacy Shaders/Transparent/Cutout/VertexLit",
            "Legacy Shaders/Transparent/Cutout/Diffuse", "Legacy Shaders/Particles/~Additive-Multiply",
            "Legacy Shaders/Particles/Additive (Soft)", "Particles/Standard Surface", "Hidden/Nature/Terrain/Utilities",
            "Hidden/TerrainEngine/Details/Vertexlit", "Hidden/TerrainEngine/Details/WavingDoublePass",
            "Hidden/TerrainEngine/Details/BillboardWavingDoublePass", "Hidden/TerrainEngine/BillboardTree",
            "Hidden/TerrainEngine/Splatmap/Diffuse-Base", "Hidden/TerrainEngine/Splatmap/Diffuse-BaseGen",
            "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass", "Nature/Terrain/Diffuse", "Hidden/Internal-GUITextureClip",
            "Hidden/Internal-GUITextureClipText", "Hidden/Internal-GUITexture", "Hidden/Internal-GUITextureBlit",
            "Hidden/Internal-GUIRoundedRect", "Hidden/Internal-UIRDefault", "Hidden/Internal-UIRAtlasBlitCopy",
            "Hidden/Internal-GUIRoundedRectWithColorPerBorder", "Mobile/VertexLit", "Mobile/Diffuse", "Mobile/Particles/Additive",
            "Unlit/Transparent", "Unlit/Transparent Cutout", "Unlit/Texture", "Unlit/Color", "Hidden/VideoComposite",
            "Hidden/VideoDecode", "Hidden/VideoDecodeOSX", "Hidden/VideoDecodeAndroid", "Hidden/VideoDecodeML", "Hidden/Compositing",
            "Hidden/Shader Forge/SFN_Blend_Divide", "Hidden/Shader Forge/SFN_Time", "Hidden/Shader Forge/SFN_Blend_Subtract",
            "Hidden/Shader Forge/SFN_UVTile", "Hidden/Shader Forge/SFN_Blend_PinLight", "Hidden/Shader Forge/FillColor",
            "Hidden/Shader Forge/SFN_Blend_LinearDodge", "Hidden/Shader Forge/SFN_Noise", "Hidden/Shader Forge/SFN_TexCoord",
            "Hidden/Shader Forge/SFN_ArcTan2_ZTO", "Hidden/Shader Forge/SFN_Blend_Multiply", "Hidden/Shader Forge/SFN_Blend_Overlay",
            "Hidden/Shader Forge/SFN_Add_2", "Hidden/Shader Forge/SFN_ArcTan2_ZTOW", "Hidden/Shader Forge/SFN_Tex2d_UV",
            "Hidden/Shader Forge/ExtractChannel", "Hidden/Shader Forge/SFN_Tex2d_NoInputs", "Hidden/Shader Forge/SFN_Blend_ColorDodge",
            "Hidden/Shader Forge/SFN_ArcTan2_NOTO", "Hidden/Shader Forge/SFN_Distance", "Hidden/Shader Forge/SFN_Posterize",
            "Hidden/Shader Forge/SFN_ComponentMask_CC2", "Hidden/Shader Forge/SFN_ComponentMask_CC3",
            "Hidden/Shader Forge/SFN_ComponentMask_CC1", "Hidden/Shader Forge/SFN_Blend_ColorBurn", "Hidden/Shader Forge/SFN_ArcTan",
            "Hidden/Shader Forge/SFN_Blend_Difference", "Hidden/Shader Forge/SFN_Blend_Screen", "Hidden/Shader Forge/SFN_Append",
            "Hidden/Shader Forge/SFN_Blend_LinearBurn", "Hidden/Shader Forge/SFN_Blend_Darken", "Hidden/Shader Forge/SFN_Multiply_2",
            "Hidden/Shader Forge/SFN_Lerp", "Hidden/Shader Forge/SFN_Abs", "Shader Forge/TransparentControl",
            "Shader Forge/SpiralClock", "Shader Forge/Spiral", "Shader Forge/ClockSpiralFinal"
        };

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

                BuildTargetGroup selectedBuildTargetGroup = BuildTargetGroup.Standalone;
                BuildTarget activeBuildTarget = BuildTarget.StandaloneWindows64;

                GameObject clonedTromboneBackground = null;

                try
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        string fileName = Path.GetFileName(path);
                        string folderPath = Path.GetDirectoryName(path);

                        // macOS Shader compiling
                        var shaders = Resources.FindObjectsOfTypeAll<Shader>();

                        var filteredShaders = new List<Shader>();

                        foreach (var shader in shaders)
                        {
                            // probably don't need to check for null here but just to be safe
                            if (shader == null || shader.name == "Standard") continue;

                            if (BaseGameShaderNames.Contains(shader.name) || TrombLoaderShaderNames.Contains(shader.name)) continue;

                            if (filteredShaders.Contains(shader)) continue;

                            if (shader.hideFlags.HasFlag(HideFlags.DontSave) || shader.hideFlags.HasFlag(HideFlags.HideAndDontSave)) continue;

                            Debug.Log($"Found shader {shader.name} to build for macOS");
                            filteredShaders.Add(shader);
                        }

                        var macShadersBuilt = true;

                        if (filteredShaders.Any())
                        {
                            MacShaderPicker window = CreateInstance<MacShaderPicker>();
                            window.titleContent = new GUIContent("macOS Shader Bundle Builder");
                            window.Init(filteredShaders, folderPath);
                            window.ShowModalUtility();
                            macShadersBuilt = window.HasBuilt;
                        }

                        clonedTromboneBackground = Instantiate(tromboneBackground.gameObject);

                        // serialize
                        foreach (var manager in clonedTromboneBackground.gameObject.GetComponentsInChildren<TromboneEventManager>())
                        {
                            manager.SerializeAllGenericEvents();
                        }

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

                        if (macShadersBuilt)
                        {
                            EditorUtility.DisplayDialog("Exportation Successful!", "Exportation Successful!", "OK");
                        } else
                        {
                            EditorUtility.DisplayDialog("Exportation Successful!", "No macOS shaders were built.", "OK");
                        }

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
