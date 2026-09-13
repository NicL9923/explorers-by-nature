using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;

public sealed class HairGuideTests
{
    [Test]
    public void StrongWindKeepsRootsPinnedAndEverySegmentInextensible()
    {
        var guide=new HairGuide();Vector3 root=new Vector3(2,3,4),growth=Vector3.up*.12f;
        guide.Reset(root,growth);
        for(int frame=0;frame<240;frame++)guide.Step(root,growth,Vector3.up,new Vector3(18,-30,3),1f/60);
        Assert.That(guide.points[0],Is.EqualTo(root));
        for(int i=1;i<=HairGuide.Segments;i++)
        {
            Assert.That(Vector3.Distance(guide.points[i],guide.points[i-1]),Is.EqualTo(.02f).Within(.00001f));
            Assert.That(guide.points[i].y,Is.GreaterThanOrEqualTo(root.y-.00001f));
        }
        Assert.That(guide.points[HairGuide.Segments].x,Is.GreaterThan(root.x+.01f));
    }
    [Test]
    public void TeleportResetsHistoryAndZeroTimeDoesNotChangeHair()
    {
        var guide=new HairGuide();guide.Reset(Vector3.zero,Vector3.up*.1f);
        guide.Step(Vector3.right*20,Vector3.up*.1f,Vector3.up,Vector3.right*30,1f/60);
        Assert.That(guide.points[HairGuide.Segments],Is.EqualTo(Vector3.right*20+Vector3.up*.1f));
        Vector3 tip=guide.points[HairGuide.Segments];
        guide.Step(Vector3.right*20,Vector3.up*.1f,Vector3.up,Vector3.right*30,0);
        Assert.That(guide.points[HairGuide.Segments],Is.EqualTo(tip));
    }
    [Test]
    public void DifferentFrameRatesSettleToComparableGuideShapes()
    {
        var slow=new HairGuide();var fast=new HairGuide();
        slow.Reset(Vector3.zero,Vector3.up*.08f);fast.Reset(Vector3.zero,Vector3.up*.08f);
        for(int i=0;i<120;i++)slow.Step(Vector3.zero,Vector3.up*.08f,Vector3.up,Vector3.right*5,1f/30);
        for(int i=0;i<480;i++)fast.Step(Vector3.zero,Vector3.up*.08f,Vector3.up,Vector3.right*5,1f/120);
        Assert.That(Vector3.Distance(slow.points[6],fast.points[6]),Is.LessThan(.002f));
    }
}
