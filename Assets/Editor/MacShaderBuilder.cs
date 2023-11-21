using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

 public class MacShaderPicker : EditorWindow
{
    private Dictionary<Shader, bool> ShaderList = new Dictionary<Shader, bool>();
    private string FolderPath;
    public bool HasBuilt = false;
    public MacShaderPrefs shaderPrefs;

    Vector2 scrollPosition;
    List<Shader> SceneShaders = new List<Shader>();

    public void Init(List<Shader> shaders, string path, GameObject background)
    {
        ShaderList.Clear();
        shaderPrefs = AssetDatabase.LoadAssetAtPath<MacShaderPrefs>("Assets/Resources/MacShaderPrefs.asset");
        if (shaderPrefs == null)
        {
            shaderPrefs = CreateInstance<MacShaderPrefs>();
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateAsset(shaderPrefs, "Assets/Resources/MacShaderPrefs.asset");
            AssetDatabase.SaveAssets();
        }

        CheckScene(background);

        foreach (var shader in shaders)
        {
            if (shaderPrefs.blocklist.Contains(shader.name))
            {
                Debug.Log($"Shader {shader.name} is blocked");
                ShaderList[shader] = false;
            }
            else if (shaderPrefs.allowlist.Contains(shader.name) || SceneShaders.Contains(shader))
            {
                Debug.Log($"Shader {shader.name} is allowed");
                ShaderList[shader] = true;
            }
            else
            {
                Debug.Log($"Default behaviour for {shader.name}");
                ShaderList[shader] = false;
            }
        }
        FolderPath = path;
    }

    public void OnGUI()
    {
        GUILayout.Label("Shaders to include:");

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        var style = GUI.skin.GetStyle("Toggle");

        for (int i = 0; i < ShaderList.Count; i++)
        {
            var shader = ShaderList.ElementAt(i).Key;
            var active = ShaderList.ElementAt(i).Value;
            if (SceneShaders.Contains(shader))
            {
                style.fontStyle = FontStyle.Bold;
            }
            else
            {
                style.fontStyle = FontStyle.Normal;
                
            }
            ShaderList[shader] = GUILayout.Toggle(active, shader.name, style);
        }

        GUILayout.EndScrollView();

        GUILayout.Box(
            "To make sure your background appears correctly on macOS we need to build a bundle containing Mac-compatible shaders. " + 
            "To save on disk space, you can exclude shaders you know you aren't using in your scene. " +
            "If you're not sure what to exclude, you can just leave everything selected at the expense of a slightly larger shader bundle." +
            "\n\nShaders that were found within the scene in its current state are highlighted in bold and are selected by default, " +
            "but this may not include all of the shaders that are used in your project."
        );

        string btnName;

        if (ShaderList.ContainsValue(true))
        {
            btnName = "Build Them Shaders!";
        }
        else
        {
            btnName = "Continue Without Building Shaders";
        }

        if (GUILayout.Button(btnName))
        {
            shaderPrefs.blocklist = new List<string>();
            foreach(var item in ShaderList)
            {
                if (item.Value)
                {
                    shaderPrefs.allowlist.Add(item.Key.name);
                }
                else
                {
                    shaderPrefs.blocklist.Add(item.Key.name);
                }
            }

            AssetDatabase.SaveAssets();
            
            if (ShaderList.ContainsValue(true))
            {
                BuildShaders();
                HasBuilt = true;
            }
            else
            {
                HasBuilt = false;
            }
            Close();
        }
    }

    void CheckScene(GameObject background)
    {
        SceneShaders.Clear();
        
        var materials = new[]
        {
            background.GetComponentsInChildren<Renderer>(true).SelectMany(renderer => renderer.sharedMaterials),
            background.GetComponentsInChildren<TMP_Text>(true).Select(textMesh => textMesh.fontSharedMaterial),
            background.GetComponentsInChildren<Graphic>(true).Select(graphics => graphics.material)
        }.SelectMany(x => x);

        foreach (var material in materials)
        {
            SceneShaders.Add(material.shader);
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