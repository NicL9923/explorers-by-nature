using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;

public sealed class HabitatTests
{
    [Test]
    public void RiverMeshEdgesMeetGroundAtWaterLevel()
    {
        for(int z=-450;z<=450;z+=5)foreach(int side in new[]{-1,1})
        {
            float x=ValleyWorld.ShoreX(z,side);
            Assert.That(ValleyShape.Height(x,z),Is.EqualTo(ValleyShape.WaterHeight).Within(.002f));
            Assert.That(ValleyShape.Height(x+side*.5f,z),Is.GreaterThan(ValleyShape.WaterHeight));
        }
    }
    [Test]
    public void HabitatRoutesKeepDucksAfloatAndBeaversDry()
    {
        for(int i=0;i<5;i++)for(int time=0;time<120;time+=3)
        {
            Vector3 p=HabitatAnimal.Position(new Vector2(i*1.4f-3,-155+i*3),true,2,i,time);
            Assert.That(ValleyShape.Height(p.x,p.z),Is.LessThan(ValleyShape.WaterHeight));
            Assert.That(p.y,Is.InRange(ValleyShape.WaterHeight-.09f,ValleyShape.WaterHeight-.06f));
        }
        for(int i=0;i<2;i++)for(int time=0;time<120;time+=3)
        {
            float z=-115+i*13;
            Vector3 p=HabitatAnimal.Position(new Vector2(ValleyWorld.ShoreX(z,-1)-1.5f,z),false,.5f,i+8,time);
            Assert.That(p.y,Is.GreaterThan(ValleyShape.WaterHeight));
            Assert.That(p.y,Is.EqualTo(ValleyShape.Height(p.x,p.z)).Within(.001f));
        }
    }
}
