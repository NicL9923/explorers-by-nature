using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class ArtImportSettings : AssetPostprocessor
{
    [MenuItem("Explorers/Prepare art shader variants")]
    public static void PrepareMaterials()
    {
        foreach(string referencePath in AssetDatabase.GetAllAssetPaths())
            if(referencePath.StartsWith("Assets/Resources/ReferenceGround/") || referencePath.StartsWith("Assets/Resources/ReferenceDeer/"))
                if(referencePath.EndsWith(".png") || referencePath.EndsWith(".fbx"))AssetImporter.GetAtPath(referencePath)?.SaveAndReimport();
        foreach(string animalAsset in new[]{"Wildlife/Deer","Wildlife/Rabbit","Fox/Fox","Clover"})
            AssetImporter.GetAtPath("Assets/Resources/"+animalAsset+".fbx")?.SaveAndReimport();
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
        material.shader=Shader.Find("Explorers/Foliage");
        material.enableInstancing = true;
        material.SetTexture("_BaseMap", Resources.Load<Texture2D>("Woodland/PineNeedles"));
        material.SetFloat("_Cull", 0);
        material.SetFloat("_AlphaClip", 1);
        material.SetFloat("_Cutoff", .35f);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.renderQueue = 2450;
        EditorUtility.SetDirty(material);
        const string animalPath="Assets/Resources/ArtSupport/AnimalSurface.mat";
        var animal=AssetDatabase.LoadAssetAtPath<Material>(animalPath);
        if(animal==null){animal=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(animal,animalPath);}
        animal.SetTexture("_BumpMap",Resources.Load<Texture2D>("CloverDetail_Normal"));animal.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(animal);
        foreach(string surface in new[]{"Fern","Rock","Stump"})
            Preserve(ExplorersByNature.ReferenceGroundArt.Surface(surface),"Ground-"+surface);
        foreach(bool fir in new[]{false,true})
        {
            var tree=ExplorersByNature.ReferenceTreeArt.Tree(fir,null);
            if(tree==null)continue;
            foreach(var renderer in tree.GetComponentsInChildren<Renderer>())
                foreach(var treeMaterial in renderer.sharedMaterials)Preserve(treeMaterial,"Tree-"+treeMaterial.name);
            Object.DestroyImmediate(tree);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("ART_MATERIAL_VARIANTS_READY");
    }

    static void Preserve(Material source,string name)
    {
        string path="Assets/Resources/ArtSupport/"+name+".mat";
        var target=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(target==null)AssetDatabase.CreateAsset(new Material(source),path);
        else {target.shader=source.shader;target.CopyPropertiesFromMaterial(source);EditorUtility.SetDirty(target);}
    }

    void OnPreprocessModel()
    {
        if(!assetPath.StartsWith("Assets/Resources/ReferenceGround/") && !assetPath.StartsWith("Assets/Resources/ReferenceDeer/"))return;
        var importer=(ModelImporter)assetImporter;
        importer.globalScale=1;importer.useFileScale=true;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation=ModelImporterMaterialLocation.InPrefab;
        importer.isReadable=false;importer.importAnimation=false;
        importer.animationType=assetPath.Contains("ReferenceDeer/")?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
        importer.importTangents=ModelImporterTangents.CalculateMikk;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/")) return;
        var importer = (TextureImporter)assetImporter;
        bool normal=assetPath.EndsWith("_Normal.png");
        importer.textureShape=TextureImporterShape.Texture2D;
        bool terrainDiffuse=assetPath.EndsWith("ReferenceGround/ForestFloor_BaseColor.png") || assetPath.EndsWith("ReferenceGround/TrailGround_BaseColor.png");
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        bool data=assetPath.EndsWith("_MetallicSmoothness.png") || assetPath.EndsWith("_Roughness.png");
        importer.sRGBTexture = !(normal || data);
        importer.alphaIsTransparency = !normal && !data && !terrainDiffuse;
        importer.mipmapEnabled = true;
        importer.mipMapsPreserveCoverage = !normal && !data && !terrainDiffuse;
        importer.alphaTestReferenceValue = .35f;
        importer.anisoLevel = 4;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
