using ExplorersByNature;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class VegetationWindTests
{
    [Test]
    public void MovingVegetationRetainsPbrMapsAndMatchingGeometryPasses()
    {
        Material fern = ReferenceGroundArt.Surface("Fern");
        Assert.That(fern.shader.name, Is.EqualTo("Explorers/VegetationLit"));
        Assert.That(ShaderUtil.ShaderHasError(fern.shader), Is.False);
        Assert.That(fern.IsKeywordEnabled("_NATURE_FERN"), Is.True);
        Assert.That(fern.IsKeywordEnabled("_NORMALMAP"), Is.True);
        Assert.That(fern.IsKeywordEnabled("_METALLICSPECGLOSSMAP"), Is.True);
        Assert.That(fern.GetFloat("_AlphaToMask"), Is.EqualTo(1));
        foreach (string pass in new[] { "ForwardLit", "ShadowCaster", "DepthOnly", "DepthNormals" })
            Assert.That(fern.FindPass(pass), Is.GreaterThanOrEqualTo(0), pass + " must deform the same leaf silhouette");
        foreach (string solid in new[] { "Stump", "Rock" })
            Assert.That(ReferenceGroundArt.Surface(solid).shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), "Solid ground props must stay still");
    }
}
