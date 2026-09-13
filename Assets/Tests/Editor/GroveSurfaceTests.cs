using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GroveSurfaceTests
{
    [TestCase("ForestFloor")]
    [TestCase("TrailGround")]
    public void TerrainDiffuseCarriesDrySmoothnessWithoutTransparencyProcessing(string name)
    {
        string path="Assets/Resources/ReferenceGround/"+name+"_BaseColor.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        Assert.That(importer.textureShape,Is.EqualTo(TextureImporterShape.Texture2D));
        Assert.That(importer.alphaSource,Is.EqualTo(TextureImporterAlphaSource.FromInput));
        Assert.That(importer.alphaIsTransparency,Is.False,"Terrain consumes alpha as smoothness, not leaf coverage");
        var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            Assert.That(source.LoadImage(File.ReadAllBytes(path)),Is.True);
            int maximum=0;
            foreach(var pixel in source.GetPixels32())maximum=Mathf.Max(maximum,pixel.a);
            Assert.That(maximum,Is.LessThan(128),"Opaque alpha makes Terrain Lit render mirror-like forest soil");
        }
        finally {Object.DestroyImmediate(source);}
    }
}
