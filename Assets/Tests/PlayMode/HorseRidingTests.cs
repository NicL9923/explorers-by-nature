using System.Collections;
using System.Linq;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class HorseRidingTests
{
    FirstPersonWalker walker;
    HorseRiding riding;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("Pinewatch");
        yield return null;
        walker=Object.FindFirstObjectByType<FirstPersonWalker>();walker.Automated=true;
        riding=Object.FindFirstObjectByType<HorseRiding>();
        Assert.That(riding,Is.Not.Null);Assert.That(riding.Horse,Is.Not.Null);
    }

    [TearDown]
    public void TearDown()
    {
        if(riding!=null)riding.Release();
        if(walker!=null){walker.Automated=false;walker.SetMenu(true);}
    }

    [UnityTest]
    public IEnumerator SeatedPlayerMustStandBeforeMountingOrUsingTools()
    {
        var comfort=Object.FindFirstObjectByType<RanchComfort>();
        var bench=new GameObject("Nearby test bench");bench.transform.position=riding.Horse.transform.position+Vector3.right*2;
        var prop=bench.AddComponent<PropComfort>();prop.Configure("bench");
        walker.Teleport(bench.transform.position);comfort.Sit(prop);
        Assert.That(comfort.Seated,Is.True);
        Assert.That(riding.Mount(),Is.False);Assert.That(riding.Mounted,Is.False);
        var equipment=Object.FindFirstObjectByType<FrontierEquipment>();equipment.Equip(3);
        Assert.That(equipment.Use(),Is.False);Assert.That(equipment.Loaded,Is.True);
        comfort.Stand();Assert.That(walker.enabled,Is.True);Assert.That(riding.Mount(),Is.True);
        Object.Destroy(bench);yield return null;
    }

    [UnityTest]
    public IEnumerator MountRequiresProximityAndPlacesCameraAboveTheSeatedRider()
    {
        var horseRenderers=riding.Horse.GetComponentsInChildren<Renderer>();
        Assert.That(horseRenderers,Is.Not.Empty);
        Bounds horseBounds=horseRenderers[0].bounds;
        foreach(var renderer in horseRenderers)horseBounds.Encapsulate(renderer.bounds);
        Assert.That(horseBounds.size.y,Is.InRange(1.8f,2.6f),"Imported horse body must remain at meter scale.");
        Assert.That(horseBounds.size.x,Is.InRange(.5f,2f));
        var body=horseRenderers.Single(renderer=>renderer.name=="BodyMesh");
        Assert.That(body.bounds.size.y,Is.InRange(.8f,1.5f),"Tack/reins alone must not mask a missing barrel mesh.");
        var seat=riding.Horse.GetComponentsInChildren<Transform>().Single(t=>t.name=="SaddleSeat");
        Assert.That(riding.Horse.transform.InverseTransformPoint(seat.position).y,Is.EqualTo(1.65f).Within(.01f));
        walker.Teleport(riding.Horse.transform.position+Vector3.right*5);
        Assert.That(riding.Mount(),Is.False);
        Assert.That(walker.GetComponent<CharacterController>().enabled,Is.True);
        walker.Teleport(riding.Horse.transform.position+Vector3.right*2);
        Assert.That(riding.Mount(),Is.True);
        Assert.That(riding.Mounted,Is.True);
        Assert.That(walker.GetComponent<CharacterController>().enabled,Is.False);
        var avatar=riding.Horse.GetComponentInChildren<PlayerAvatar>();
        Assert.That(avatar,Is.Not.Null);Assert.That(avatar.Model,Is.Not.Null);
        Assert.That(avatar.transform.position.y-riding.Horse.transform.position.y,Is.EqualTo(HorseRiding.RiderHeight).Within(.001f));
        Assert.That(Vector3.Distance(walker.transform.position,avatar.transform.position),Is.LessThan(.001f));
        Assert.That(walker.view.transform.position.y-riding.Horse.transform.position.y,Is.InRange(2.2f,2.5f));
        Assert.That(avatar.Model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Head").gameObject.activeSelf,Is.False,"Local rider head must hide immediately, including automated movement.");
        yield return null;
        var pivots=riding.Horse.GetComponentsInChildren<Transform>(true);
        foreach(string name in new[]{"FrontLegL","FrontLegR","HindLegL","HindLegR","FrontKneeL","FrontKneeR","HindKneeL","HindKneeR","SaddleSeat"})
            Assert.That(pivots.Count(t=>t.name==name),Is.EqualTo(1),name);
    }

    [UnityTest]
    public IEnumerator MountedMovementSettlesOnTerrainAndHomeTeleportRestoresWalking()
    {
        walker.Teleport(riding.Horse.transform.position+Vector3.right*2);Assert.That(riding.Mount(),Is.True);
        for(int step=0;step<120;step++)walker.Move(Vector2.zero,false,1f/60);
        Vector3 settled=riding.Horse.transform.position;
        for(int step=0;step<120;step++)walker.Move(Vector2.zero,false,1f/60);
        Assert.That(Mathf.Abs(riding.Horse.transform.position.y-settled.y),Is.LessThan(.08f),"Idle mount must remain on ground.");
        Assert.That(Mathf.Abs(settled.y-ValleyShape.Height(settled.x,settled.z)),Is.LessThan(.4f));
        walker.transform.rotation=riding.Horse.transform.rotation;
        for(int step=0;step<60;step++)walker.Move(Vector2.up,false,1f/60);
        Vector3 traveled=riding.Horse.transform.position-settled;traveled.y=0;
        Assert.That(traveled.magnitude,Is.GreaterThan(2),"Walker movement must drive the horse motor.");
        Assert.That(Vector3.Distance(walker.transform.position,riding.Horse.transform.position+Vector3.up*HorseRiding.RiderHeight),Is.LessThan(.001f));
        walker.Teleport(ValleyShape.Spawn);
        Assert.That(riding.Mounted,Is.False);Assert.That(walker.GetComponent<CharacterController>().enabled,Is.True);
        Assert.That(Vector3.Distance(walker.transform.position,ValleyShape.Spawn),Is.LessThan(.001f));
        Assert.That(riding.Horse.GetComponentInChildren<PlayerAvatar>(),Is.Null,"Released rider must hide.");
        Vector3 horsePosition=riding.Horse.transform.position;
        walker.Move(Vector2.up,false,.05f);
        Assert.That(riding.Horse.transform.position,Is.EqualTo(horsePosition));
        yield return null;
    }

    [UnityTest]
    public IEnumerator BlockedDismountKeepsTheRiderMountedAndOpenGroundReleasesThem()
    {
        walker.Teleport(riding.Horse.transform.position+Vector3.right*2);Assert.That(riding.Mount(),Is.True);
        var blockers=new GameObject("Dismount clearance blockers");
        try
        {
            for(int i=0;i<8;i++)
            {
                Vector3 offset=Quaternion.Euler(0,i*45,0)*riding.Horse.transform.right*1.7f;
                Vector3 p=ValleyShape.Ground(riding.Horse.transform.position.x+offset.x,riding.Horse.transform.position.z+offset.z,.15f);
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(blockers.transform);cube.transform.position=p+Vector3.up*.9f;cube.transform.localScale=new Vector3(.8f,1.8f,.8f);
            }
            Physics.SyncTransforms();
            Assert.That(riding.Dismount(),Is.False);Assert.That(riding.Mounted,Is.True);
            Assert.That(walker.GetComponent<CharacterController>().enabled,Is.False);
            blockers.SetActive(false);Physics.SyncTransforms();
            Assert.That(riding.Dismount(),Is.True);Assert.That(riding.Mounted,Is.False);
            Assert.That(walker.GetComponent<CharacterController>().enabled,Is.True);
            Assert.That(Vector3.Distance(walker.transform.position,riding.Horse.transform.position),Is.InRange(1.5f,2.2f));
        }
        finally{Object.Destroy(blockers);}
        yield return null;
    }
}
