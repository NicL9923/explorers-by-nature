using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MountainSurfaceTests
{
    [Test]
    public void MountainMaterialKeepsItsDedicatedShaderInPlayerBuilds()
    {
        var material=Resources.Load<Material>("AlpineRange/Mountain");
        Assert.That(material,Is.Not.Null,"The runtime-created range needs a serialized Resources material to retain its shader");
        Assert.That(material.shader.name,Is.EqualTo("Explorers/AlpineRock"));
        Assert.That(ShaderUtil.ShaderHasError(material.shader),Is.False,"A failed shader renders the entire northern skyline pink");
        Assert.That(material.GetTexture("_RockMap"),Is.Not.Null,"Triplanar granite needs its serialized scan reference");
        Assert.That(material.HasProperty("_SnowLine"),Is.True);
    }
}
