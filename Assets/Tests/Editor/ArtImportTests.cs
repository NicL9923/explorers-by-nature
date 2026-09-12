using System.Linq;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;

public sealed class ArtImportTests
{
    [Test]
    public void OriginalModelsImportWithMaterialsAndCheaperDistanceMeshes()
    {
        foreach (string name in new[] { "Clover", "Hen", "Homestead/Foundation", "Homestead/Wall", "Homestead/Door", "Homestead/Roof", "Homestead/Fence", "Homestead/Coop", "Woodland/Pine", "Woodland/Aspen", "Woodland/Wildflower", "Wildlife/Deer", "Wildlife/Rabbit", "RiverWildlife/Duck", "RiverWildlife/Beaver", "Fox/Fox" })
        {
            GameObject model = name.StartsWith("Woodland/") ? ModelArt.Tree(name, null) : ModelArt.Instantiate(name, null);
            Assert.That(model, Is.Not.Null, name);
            try
            {
                var group = model.GetComponent<LODGroup>();
                Assert.That(group, Is.Not.Null, name + " has distance meshes");
                LOD[] levels = group.GetLODs();
                Assert.That(levels.Length, Is.GreaterThanOrEqualTo(2), name);
                int high = levels[0].renderers.Sum(Triangles), low = levels[levels.Length - 1].renderers.Sum(Triangles);
                Assert.That(high, Is.LessThan(30000), name + " near triangle budget");
                Assert.That(low, Is.LessThan(high), name + " cheaper distant geometry");
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        Assert.That(material, Is.Not.Null, name);
                        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                        if (material.name == "EyeBlack") Assert.That(material.GetColor("_BaseColor").maxColorComponent, Is.LessThan(.2f), "Imported eyes retain their dark pigment");
                        if (material.name.Contains("Needles") || material.name.Contains("Leaves"))
                        {
                            Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null, material.name);
                            Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.True, material.name);
                            Assert.That(material.GetFloat("_Cull"), Is.Zero, material.name);
                        }
                    }
                if (name == "Clover") Assert.That(ValleyWorld.ModelHeight(model), Is.InRange(1.5f, 2.5f));
                if (name == "Woodland/Wildflower")
                {
                    Assert.That(ValleyWorld.ModelHeight(model), Is.InRange(.3f, 1f));
                    foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                    {
                        Assert.That(Mathf.Abs(renderer.bounds.center.x), Is.LessThan(1f), "Flowers grow at their placement origin");
                        Assert.That(Mathf.Abs(renderer.bounds.center.z), Is.LessThan(1f));
                    }
                }
                if (name == "Woodland/Pine") Assert.That(ValleyWorld.ModelHeight(model), Is.InRange(8, 13));
            }
            finally { Object.DestroyImmediate(model); }
        }
    }

    static int Triangles(Renderer renderer)
    {
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        Assert.That(filter, Is.Not.Null, renderer.name);
        return filter.sharedMesh.triangles.Length / 3;
    }
}
