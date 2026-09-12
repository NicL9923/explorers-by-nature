using System;
using System.IO;
using ExplorersByNature;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PrototypeProject
{
    public const string ScenePath = "Assets/Scenes/Pinewatch.unity";

    [InitializeOnLoadMethod]
    static void CreateOnFirstOpen()
    {
        if (!Application.isBatchMode && !File.Exists(ScenePath))
            EditorApplication.delayCall += () => { if (!File.Exists(ScenePath)) CreateScene(); };
    }

    [MenuItem("Explorers/Create prototype scene")]
    public static void CreateScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var low = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        var high = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        low.renderScale = 1; low.msaaSampleCount = 1; low.shadowDistance = 45; low.shadowCascadeCount = 1;
        low.mainLightShadowmapResolution = 1024; low.supportsHDR = false;
        high.renderScale = 1; high.msaaSampleCount = 2; high.shadowDistance = 180; high.shadowCascadeCount = 4;
        high.mainLightShadowmapResolution = 2048; high.supportsHDR = true;
        EditorUtility.SetDirty(low); EditorUtility.SetDirty(high);
        GraphicsSettings.defaultRenderPipeline = high;
        QualitySettings.renderPipeline = high;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.58f, .69f, .8f);
        RenderSettings.ambientEquatorColor = new Color(.42f, .46f, .38f);
        RenderSettings.ambientGroundColor = new Color(.18f, .22f, .16f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.64f, .74f, .8f); RenderSettings.fogDensity = .0017f;
        Material sky = Material("Sky", "Skybox/Procedural", Color.white);
        sky.SetColor("_SkyTint", new Color(.48f, .56f, .65f)); sky.SetFloat("_AtmosphereThickness", .85f);
        RenderSettings.skybox = sky;
        var sun = new GameObject("Late afternoon sun", typeof(Light)).GetComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.5f; sun.color = new Color(1, .91f, .76f);
        sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(34, -35, 0); RenderSettings.sun = sun;

        var player = new GameObject("Explorer", typeof(CharacterController));
        player.transform.position = ValleyShape.Spawn;
        var controller = player.GetComponent<CharacterController>();
        controller.height = 1.8f; controller.center = Vector3.up * .9f; controller.radius = .3f; controller.stepOffset = .4f; controller.slopeLimit = 55;
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.SetParent(player.transform, false); camera.transform.localPosition = Vector3.up * 1.65f;
        camera.nearClipPlane = .1f; camera.farClipPlane = 1200; camera.fieldOfView = 75;
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        FirstPersonWalker walker = player.AddComponent<FirstPersonWalker>(); walker.view = camera;

        ValleyWorld world = new GameObject("Pinewatch Valley").AddComponent<ValleyWorld>();
        world.walker = walker;
        world.grassTexture = Texture("aerial_grass_rock"); world.earthTexture = Texture("brown_mud_leaves_01"); world.rockTexture = Texture("rock_boulder_dry");
        world.stoneMeshes = new Mesh[3];
        for (int i = 0; i < world.stoneMeshes.Length; i++)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/RiverStone" + (i + 1) + ".fbx");
            if (model == null) throw new BuildFailedException("Missing Blender river-stone model " + (i + 1));
            world.stoneMeshes[i] = model.GetComponentInChildren<MeshFilter>().sharedMesh;
        }
        world.terrainMaterial = Material("Terrain", "Universal Render Pipeline/Terrain/Lit", Color.white);
        world.grassMaterial = Material("Grass", "Explorers/MeadowGrass", Color.white);
        world.waterMaterial = Material("Water", "Explorers/River", new Color(.07f, .23f, .25f));
        world.barkMaterial = Material("Bark", "Universal Render Pipeline/Lit", new Color(.23f, .17f, .1f));
        world.leavesMaterial = Material("Pine needles", "Universal Render Pipeline/Lit", new Color(.17f, .27f, .13f));
        world.leavesMaterial.SetFloat("_Cull", 0);
        world.rockMaterial = Material("River stone", "Universal Render Pipeline/Lit", new Color(.52f, .53f, .46f));
        world.rockMaterial.SetTexture("_BaseMap", world.rockTexture);
        world.deerMaterial = Material("Deer", "Universal Render Pipeline/Lit", new Color(.48f, .29f, .15f));
        WalkSession session = new GameObject("Walk session").AddComponent<WalkSession>();
        session.world = world; session.walker = walker; session.lowPipeline = low; session.highPipeline = high;
        if (UnityEngine.Object.FindFirstObjectByType<RanchSession>() == null) new GameObject("Ranch session").AddComponent<RanchSession>();
        new GameObject("Nature details").AddComponent<NatureDetails>();
        ExportTerrain();
        PlayerSettings.companyName = "Explorers by Nature"; PlayerSettings.productName = "Explorers by Nature";
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("PROTOTYPE_SCENE_READY " + ScenePath);
    }

    static Texture2D Texture(string name)
    {
        string path = "Assets/Textures/" + name + ".jpg";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) throw new BuildFailedException("Missing texture " + name);
        return texture;
    }

    static Material Material(string name, string shaderName, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new BuildFailedException("Missing shader " + shaderName);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.enableInstancing = true;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void ExportTerrain()
    {
        Directory.CreateDirectory("Assets/Resources");
        using (var writer = new BinaryWriter(File.Create("Assets/Resources/terrain.bytes")))
            for (int z = -450; z <= 450; z++)
                for (int x = -450; x <= 450; x++) writer.Write(ValleyShape.Height(x, z));
        AssetDatabase.ImportAsset("Assets/Resources/terrain.bytes");
    }

    public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "Builds/Linux/ExplorersByNature");
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/ExplorersByNature.exe");

    static void Build(BuildTarget target, string output)
    {
        if (!File.Exists(ScenePath)) CreateScene();
        string authoringScene = File.ReadAllText(ScenePath);
        try
        {
            EditorSceneManager.OpenScene(ScenePath);
            WalkSession session = UnityEngine.Object.FindFirstObjectByType<WalkSession>();
            session.buildRevision = Environment.GetEnvironmentVariable("EXPLORERS_BUILD_REVISION") ?? "unrecorded";
            EditorUtility.SetDirty(session);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, target = target, locationPathName = output, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException(report.summary.result.ToString());
            Debug.Log("PROTOTYPE_BUILD_READY " + output);
        }
        finally
        {
            // BuildPipeline can reload the scene and invalidate the original component reference.
            File.WriteAllText(ScenePath, authoringScene);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
