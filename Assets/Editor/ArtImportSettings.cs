using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class ArtImportSettings : AssetPostprocessor
{
    [MenuItem("Explorers/Prepare art shader variants")]
    public static void PrepareMaterials()
    {
        for(int i=1;i<=3;i++)
        {
            var importer=AssetImporter.GetAtPath("Assets/Models/RiverStone"+i+".fbx") as ModelImporter;
            if(importer!=null && !importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
        }
        Directory.CreateDirectory("Assets/Resources/ArtSupport");
        const string firePath="Assets/Resources/ArtSupport/Fire.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(firePath)==null)AssetDatabase.CreateAsset(new Material(Shader.Find("Explorers/CampfireFlame")),firePath);
        // Serialized Resources material keeps the runtime-created foliage variant in players.
        Directory.CreateDirectory("Assets/Resources/ArtSupport");
        const string path = "Assets/Resources/ArtSupport/Foliage.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.enableInstancing = true;
        material.SetTexture("_BaseMap", Resources.Load<Texture2D>("Woodland/PineNeedles"));
        material.SetFloat("_Cull", 0);
        material.SetFloat("_AlphaClip", 1);
        material.SetFloat("_Cutoff", .35f);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.renderQueue = 2450;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        Debug.Log("ART_MATERIAL_VARIANTS_READY");
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.mipMapsPreserveCoverage = true;
        importer.alphaTestReferenceValue = .35f;
        importer.anisoLevel = 4;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
