using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;

public sealed class RiverWaveTests
{
    [Test]
    public void PebbleWavePropagatesDecaysAndSurvivesAFrameStall()
    {
        var field=new RiverWaveField(33,33,.5f);
        field.Reset(Vector2.zero,(x,z)=>2,(x,z)=>Vector2.zero);
        field.Impulse(8,8,1.5f,1);
        for(int i=0;i<30;i++)field.Step(1f/60);
        Assert.That(Mathf.Abs(field.Sample(9,8)),Is.GreaterThan(.0001f),"Disturbance must propagate beyond the impact.");
        float initial=0;foreach(float h in field.Elevation)initial+=h*h;
        field.Step(3); // Catch-up is bounded rather than taking an unstable multi-second step.
        for(int i=0;i<1200;i++)field.Step(1f/60);
        float final=0;foreach(float h in field.Elevation){Assert.That(float.IsNaN(h)||float.IsInfinity(h),Is.False);final+=h*h;}
        Assert.That(final,Is.LessThan(initial*.15f));
    }
    [Test]
    public void DryBankCannotReceiveOrTransportWaves()
    {
        var field=new RiverWaveField(25,25,.5f);
        field.Reset(Vector2.zero,(x,z)=>x<6?1:0,(x,z)=>new Vector2(.2f,.7f));
        field.Impulse(5.5f,6,2,2);
        for(int i=0;i<180;i++)field.Step(1f/30);
        for(int z=0;z<25;z++)for(int x=12;x<25;x++)Assert.That(field.Elevation[z*25+x],Is.Zero);
    }
    [Test]
    public void CurrentCarriesWaveEnergyDownstream()
    {
        var calm=new RiverWaveField(33,49,.5f);
        var moving=new RiverWaveField(33,49,.5f);
        calm.Reset(Vector2.zero,(x,z)=>1,(x,z)=>Vector2.zero);
        moving.Reset(Vector2.zero,(x,z)=>1,(x,z)=>new Vector2(0,1));
        calm.Impulse(8,8,1.5f,1);moving.Impulse(8,8,1.5f,1);
        for(int i=0;i<60;i++){calm.Step(1f/60);moving.Step(1f/60);}
        float Centroid(RiverWaveField field)
        {
            float moment=0,total=0;
            for(int z=0;z<field.Height;z++)for(int x=0;x<field.Width;x++)
            {float weight=Mathf.Abs(field.Elevation[z*field.Width+x]);moment+=z*field.Spacing*weight;total+=weight;}
            return moment/total;
        }
        Assert.That(Centroid(moving)-Centroid(calm),Is.GreaterThan(.5f));
    }
    [Test]
    public void MovingGridPreservesOverlappingWaveAndItsMomentum()
    {
        var original=new RiverWaveField(41,41,.5f);
        var shifted=new RiverWaveField(41,41,.5f);
        foreach(var field in new[]{original,shifted})
        {
            field.Reset(Vector2.zero,(x,z)=>1,(x,z)=>Vector2.zero);
            field.Impulse(10,10,1.5f,1);
            for(int step=0;step<12;step++)field.Step(1f/60);
        }
        shifted.Shift(new Vector2(2,3),(x,z)=>1,(x,z)=>Vector2.zero);
        Assert.That(shifted.Sample(10,10),Is.EqualTo(original.Sample(10,10)).Within(.00001f));
        // Matching the next frame requires retaining velocity as well as visible height.
        for(int step=0;step<12;step++){original.Step(1f/60);shifted.Step(1f/60);}
        Assert.That(shifted.Sample(10,10),Is.EqualTo(original.Sample(10,10)).Within(.00001f));
        Assert.That(shifted.Sample(21,22),Is.Zero,"Newly exposed cells start at rest.");
    }
    [TestCase(443f)]
    [TestCase(439f)]
    [TestCase(-443f)]
    [TestCase(-439f)]
    public void ClampedValleyLimitDoesNotRecenterEveryFrame(float playerZ)
    {
        float center=0;int moves=0;
        for(int frame=0;frame<120;frame++)
            if(RiverDynamics.NeedsRecenter(playerZ,center)){center=RiverDynamics.DesiredCenter(playerZ);moves++;}
        Assert.That(moves,Is.EqualTo(1));
        Assert.That(Mathf.Abs(center),Is.EqualTo(414));
    }
    [Test]
    public void WoodSettlesAtSurfaceAndRespondsToARisingWave()
    {
        float y=2,velocity=0;
        for(int i=0;i<600;i++)RiverWaveField.FloatStep(ref y,ref velocity,1,.1f,1f/60);
        Assert.That(y,Is.EqualTo(1).Within(.01f));
        RiverWaveField.FloatStep(ref y,ref velocity,1.1f,.1f,1f/30);
        Assert.That(velocity,Is.GreaterThan(0));
        for(int i=0;i<600;i++)RiverWaveField.FloatStep(ref y,ref velocity,1.1f,.1f,1f/60);
        Assert.That(y,Is.EqualTo(1.1f).Within(.01f));
    }
}
