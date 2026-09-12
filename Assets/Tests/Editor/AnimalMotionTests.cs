using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;

public sealed class AnimalMotionTests
{
    [Test]
    public void StanceCancelsForwardTravelAndSwingClearsGround()
    {
        const float stride = .48f, speed = .65f, dt = .05f;
        float phaseDelta = speed * dt * .64f / stride;
        Vector2 before = AnimalMotion.Step(.2f, stride, .1f);
        Vector2 after = AnimalMotion.Step(.2f + phaseDelta, stride, .1f);
        Assert.That(after.x + speed * dt, Is.EqualTo(before.x).Within(.00001f), "A stance foot must stay at its world position as the body advances.");
        Assert.That(after.y, Is.Zero);
        Assert.That(AnimalMotion.Step(.82f, stride, .1f).y, Is.EqualTo(.1f).Within(.00001f));
        Assert.That(Vector2.Distance(AnimalMotion.Step(.99999f, stride, .1f), AnimalMotion.Step(0, stride, .1f)), Is.LessThan(.0001f));
    }
    [Test]
    public void GrazingEntersAndLeavesWithoutPoseJumps()
    {
        Assert.That(AnimalMotion.IdleEnvelope(2, 19, 3, 9), Is.Zero);
        Assert.That(AnimalMotion.IdleEnvelope(7, 19, 3, 9), Is.EqualTo(1));
        Assert.That(AnimalMotion.IdleEnvelope(12, 19, 3, 9), Is.Zero);
        Assert.That(AnimalMotion.IdleEnvelope(3.001f, 19, 3, 9), Is.LessThan(.00001f));
        Assert.That(AnimalMotion.IdleEnvelope(11.999f, 19, 3, 9), Is.LessThan(.00001f));
    }
    [TestCase("Wildlife/Deer")]
    [TestCase("Wildlife/Rabbit")]
    [TestCase("Fox/Fox")]
    [TestCase("RiverWildlife/Beaver")]
    [TestCase("RiverWildlife/Duck")]
    [TestCase("Clover")]
    [TestCase("Hen")]
    public void BothImportedLodsHaveHeadAndLegSkinning(string resource)
    {
        var model = Resources.Load<GameObject>(resource);
        Assert.That(model, Is.Not.Null);
        var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Assert.That(skins.Length, Is.EqualTo(2));
        foreach (var skin in skins)
        {
            Assert.That(skin.sharedMesh.bindposes.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(System.Array.Exists(skin.bones, bone => bone != null && bone.name == "Head"), Is.True);
            Assert.That(System.Array.Exists(skin.bones, bone => bone != null && bone.name == "FrontLLower"), Is.True);
        }
    }
}
