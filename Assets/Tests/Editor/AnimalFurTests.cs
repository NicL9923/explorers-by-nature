using System.Linq;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;

public sealed class AnimalFurTests
{
    [Test]
    public void WindBendsPinnedHairAndStillAirSettlesIt()
    {
        var spring=new FurSpring();
        for(int i=0;i<120;i++)spring.Step(Vector3.zero,new Vector3(4,0,0),Vector3.up,.03f,1f/60);
        Assert.That(spring.displacement.x,Is.GreaterThan(.005f));
        Assert.That(spring.displacement.y,Is.EqualTo(0).Within(.00001f));
        Assert.That(spring.displacement.magnitude,Is.LessThanOrEqualTo(.03f*.8f));
        for(int i=0;i<240;i++)spring.Step(Vector3.zero,Vector3.zero,Vector3.up,.03f,1f/60);
        Assert.That(spring.displacement.magnitude,Is.LessThan(.00001f));
    }
    [Test]
    public void MotionHasInertiaAndTeleportsDoNotStretchCoat()
    {
        var spring=new FurSpring();
        spring.Step(new Vector3(.004f,0,0),Vector3.zero,Vector3.up,.03f,1f/60);
        Assert.That(spring.displacement.x,Is.LessThan(0));
        spring.Step(new Vector3(5,0,0),Vector3.right*40,Vector3.up,.03f,1f/60);
        Assert.That(spring.displacement,Is.EqualTo(Vector3.zero));Assert.That(spring.velocity,Is.EqualTo(Vector3.zero));
        spring.Step(Vector3.zero,Vector3.right*40,Vector3.up,.03f,.5f);
        Assert.That(spring.displacement,Is.EqualTo(Vector3.zero));
    }
    [Test]
    public void LowAndHighFrameRatesConvergeWithoutExplodingInGusts()
    {
        var slow=new FurSpring();var fast=new FurSpring();
        for(int i=0;i<90;i++)slow.Step(Vector3.zero,Vector3.right*50,Vector3.up,.1f,1f/30);
        for(int i=0;i<360;i++)fast.Step(Vector3.zero,Vector3.right*50,Vector3.up,.1f,1f/120);
        Assert.That(Vector3.Distance(slow.displacement,fast.displacement),Is.LessThan(.0001f));
        Assert.That(slow.displacement.magnitude,Is.LessThanOrEqualTo(.08001f));
        Assert.That(float.IsNaN(slow.velocity.x),Is.False);
    }
    [TestCase("ReferenceDeer/Deer")]
    [TestCase("Wildlife/Deer")]
    [TestCase("Wildlife/Rabbit")]
    [TestCase("Fox/Fox")]
    [TestCase("Clover")]
    public void ImportedCoatGroomHasValidPinnedRootsAndNoFacialHair(string path)
    {
        var asset=Resources.Load<GameObject>(path);var groom=asset.GetComponent<AnimalFurGroom>();
        Assert.That(groom,Is.Not.Null,path+" requires an FBX reimport to generate the groom.");
        Assert.That(groom.roots.Length,Is.GreaterThanOrEqualTo(1200));
        foreach(var root in groom.roots)
        {
            var w=root.weights;
            Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.00001f));
            Assert.That(root.normal.sqrMagnitude,Is.EqualTo(1).Within(.001f));
            Assert.That(root.material,Is.LessThan(groom.skin.sharedMaterials.Length));
            float excluded=0;
            void Check(int bone,float weight)
            {
                Assert.That(bone,Is.InRange(0,groom.bindposes.Length-1));
                string name=groom.skin.bones[bone].name;
                if(name=="Head" || name.StartsWith("Ear") || name.EndsWith("Foot") || name.EndsWith("Lower"))excluded+=weight;
            }
            Check(w.boneIndex0,w.weight0);Check(w.boneIndex1,w.weight1);Check(w.boneIndex2,w.weight2);Check(w.boneIndex3,w.weight3);
            Assert.That(excluded,Is.LessThan(.151f));
        }
        if(path=="Clover")Assert.That(groom.roots.Count(root=>root.length>.06f),Is.GreaterThan(30),"The cow needs visible long switch hairs, not only its short coat.");
    }
}
