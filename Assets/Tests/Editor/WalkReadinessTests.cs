using ExplorersByNature;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class WalkReadinessTests
{
    [Test]
    public void TrailIsDryAndTraversableWithoutJumping()
    {
        Vector3 previous = ValleyShape.Trail(-235);
        for (float z = -234; z <= 135; z++)
        {
            Vector3 point = ValleyShape.Trail(z);
            Assert.That(point.y, Is.GreaterThan(ValleyShape.WaterHeight + 1), "Trail under water at " + z);
            float rise = Mathf.Abs(point.y - previous.y);
            float run = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(previous.x, previous.z));
            Assert.That(Mathf.Atan2(rise, run) * Mathf.Rad2Deg, Is.LessThan(45), "Unwalkable trail slope at " + z);
            previous = point;
        }
    }

    [Test]
    public void TerrainStaysWithinItsHeightmapAndContainsTheRiver()
    {
        for (float z = -450; z <= 450; z += 5)
        {
            Assert.That(ValleyShape.Height(ValleyShape.RiverX(z), z), Is.LessThan(ValleyShape.WaterHeight));
            for (float x = -450; x <= 450; x += 5)
                Assert.That(ValleyShape.Height(x, z), Is.InRange(0, 360));
        }
    }

    [Test]
    public void PlayableSceneHasConnectedCameraMaterialsAndQualityPresets()
    {
        EditorSceneManager.OpenScene(PrototypeProject.ScenePath);
        var world = Object.FindFirstObjectByType<ValleyWorld>();
        var session = Object.FindFirstObjectByType<WalkSession>();
        Assert.That(world, Is.Not.Null);
        Assert.That(session.world, Is.SameAs(world));
        Assert.That(session.walker, Is.SameAs(world.walker));
        Assert.That(world.walker.view, Is.Not.Null);
        Assert.That(world.walker.GetComponent<CharacterController>(), Is.Not.Null);
        Assert.That(session.lowPipeline, Is.Not.Null);
        Assert.That(session.highPipeline, Is.Not.Null);
        Assert.That(session.lowPipeline, Is.Not.SameAs(session.highPipeline));
        foreach (Material material in new[] { world.terrainMaterial, world.waterMaterial, world.grassMaterial, world.barkMaterial, world.leavesMaterial, world.rockMaterial, world.deerMaterial })
        {
            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False, material.name);
        }
        Assert.That(world.grassTexture, Is.Not.Null);
        Assert.That(world.earthTexture, Is.Not.Null);
        Assert.That(world.rockTexture, Is.Not.Null);
        Assert.That(world.stoneMeshes, Has.Length.EqualTo(3));
        foreach (Mesh stone in world.stoneMeshes)
        {
            Assert.That(stone, Is.Not.Null);
            Assert.That(stone.vertexCount, Is.GreaterThan(20));
            Assert.That(stone.bounds.size.magnitude, Is.InRange(1, 5), "FBX scale must stay in world meters");
        }
        Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(PrototypeProject.ScenePath));
    }
}
