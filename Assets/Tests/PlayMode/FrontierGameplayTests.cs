using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using ExplorersByNature;
using ExplorersByNature.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FrontierGameplayTests
{
    [UnityTest]
    public IEnumerator QueuedShotKeepsItsAimPoseWhileLaterPollsTrackMovement()
    {
        string directory=Path.Combine(Path.GetTempPath(),"unity-shot-pose-"+Guid.NewGuid());Directory.CreateDirectory(directory);
        string path=Path.Combine(directory,"ranch.json");File.WriteAllText(path,JsonUtility.ToJson(new RanchState{huntingEnabled=true}));
        using var pollEntered=new ManualResetEventSlim();using var releasePoll=new ManualResetEventSlim();
        var shots=new ConcurrentQueue<Request>();int holdPoll=0;
        Request Decode(string json)
        {
            var request=JsonUtility.FromJson<Request>(json);
            if(request.action=="poll" && Interlocked.Exchange(ref holdPoll,0)==1)
            {pollEntered.Set();if(!releasePoll.Wait(4000))throw new TimeoutException("Shot test did not release the held poll.");}
            if(request.action=="fire")shots.Enqueue(request);
            return request;
        }
        RanchServer server=null;RanchConnection connection=null;
        try
        {
            server=new RanchServer(IPAddress.Loopback,0,"shot-test",new Ranch(path,JsonUtility.ToJson,s=>JsonUtility.FromJson<RanchState>(s),(x,z)=>20),JsonUtility.ToJson,Decode);
            connection=new RanchConnection("127.0.0.1",server.Port,"shot-test","Moving explorer");
            var deer=FrontierSites.Deer[0];Vector3 original=new Vector3(deer.x,20,deer.z-10),later=original+Vector3.right*2,stale=original+Vector3.left;
            yield return Wait(()=>{connection.Tick(stale,0,PlayerModels.TrailScout);return connection.Connected && connection.State!=null;},"initial shot-test snapshot");
            Interlocked.Exchange(ref holdPoll,1);
            yield return Wait(()=>{connection.Tick(stale,0,PlayerModels.TrailScout);return pollEntered.IsSet;},"worker blocked on an in-flight poll");
            connection.SendShot(deer.id,new Vector3(0,-.75f,10),original,0,PlayerModels.TrailScout);
            connection.Tick(later,90,PlayerModels.Frontiersman);
            releasePoll.Set();
            yield return Wait(()=>{connection.Tick(later,90,PlayerModels.Frontiersman);return connection.State.meat==2;},"queued shot hits from its original aim pose");
            Assert.That(shots.TryDequeue(out var shot),Is.True);
            Assert.That(new Vector3(shot.px,shot.py,shot.pz),Is.EqualTo(original));
            Assert.That(shot.yaw,Is.EqualTo(0));Assert.That(shot.model,Is.EqualTo(PlayerModels.TrailScout));
            Assert.That(shot.ax,Is.EqualTo(0));Assert.That(shot.ay,Is.EqualTo(-.75f));Assert.That(shot.az,Is.EqualTo(10));
            yield return Wait(()=>
            {
                connection.Tick(later,90,PlayerModels.Frontiersman);
                var reply=connection.Latest;return reply.players.Any(p=>p.id==reply.playerId && p.x==later.x && p.model==PlayerModels.Frontiersman);
            },"subsequent polls publish the current pose and appearance");
        }
        finally
        {
            releasePoll.Set();connection?.Dispose();server?.Dispose();
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
    }

    [UnityTest]
    public IEnumerator ToolsGatherReloadAndHuntThroughTheSharedAuthority()
    {
        yield return SceneManager.LoadSceneAsync("Pinewatch");yield return null;
        var session=UnityEngine.Object.FindFirstObjectByType<RanchSession>();
        var walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonWalker>();walker.Automated=true;walker.SetMenu(false);
        var equipment=session.GetComponent<FrontierEquipment>();
        string directory=Path.Combine(Path.GetTempPath(),"unity-frontier-"+Guid.NewGuid());session.DataDirectory=directory;session.StartSolo();
        try
        {
            yield return Wait(()=>session.Connection?.State!=null && equipment!=null,"frontier connection and tools ready");
            yield return null;
            var targets=UnityEngine.Object.FindObjectsByType<FrontierTarget>(FindObjectsSortMode.None);
            foreach(string kind in new[]{"wood","stone"})
            {
                var target=targets.First(t=>t.kind==kind && t.id==0);
                int before=kind=="wood"?session.Connection.State.wood:session.Connection.State.stone;
                equipment.Equip(kind=="wood"?1:2);AimAt(walker,target);
                yield return null;
                Assert.That(equipment.Use(),Is.True,kind+" tool begins a real aimed swing: "+equipment.Status);
                yield return Wait(()=>(kind=="wood"?session.Connection.State.wood:session.Connection.State.stone)==before+(kind=="wood"?8:6),kind+" gathering commits the expected resources");
                yield return Wait(()=>!target.gameObject.activeSelf,kind+" depleted node disappears");
                Assert.That(session.Connection.State.cooldowns.Any(c=>c.kind==kind && c.id==target.id && c.ready>session.Connection.Latest.utc),Is.True,kind+" regrowth is authoritative");
                yield return new WaitForSeconds(2.2f);
            }
            Assert.That(session.Connection.State.huntingEnabled,Is.False,"hunting starts off");
            equipment.Equip(3);yield return null;
            var weaponRenderers=equipment.HeldModel.GetComponentsInChildren<Renderer>();
            Bounds weaponBounds=weaponRenderers[0].bounds;foreach(var renderer in weaponRenderers)weaponBounds.Encapsulate(renderer.bounds);
            Vector3 visible=walker.view.WorldToViewportPoint(weaponBounds.center);
            Assert.That(visible.z,Is.GreaterThan(.1f),"Muzzleloader geometry must remain in front of the camera after import-axis conversion.");
            Assert.That(visible.x,Is.InRange(0f,1f));Assert.That(visible.y,Is.InRange(0f,1f));
            var ramrod=walker.view.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Ramrod");
            Vector3 storedRod=equipment.HeldModel.transform.InverseTransformPoint(ramrod.position);
            walker.view.transform.rotation=Quaternion.LookRotation(Vector3.up);
            int beforeShot=session.Connection.State.revision;
            Assert.That(equipment.Use(),Is.True,"untargeted shot fires while hunting is off");
            Assert.That(equipment.Loaded,Is.False,"firing leaves the muzzleloader unloaded");
            Assert.That(equipment.Use(),Is.False,"an unloaded muzzleloader cannot fire again");
            float began=Time.time;
            Assert.That(equipment.BeginReload(),Is.True);
            Assert.That(equipment.Reloading,Is.True);
            equipment.Equip(1);Assert.That(equipment.Tool,Is.EqualTo(3),"tool switching cannot skip a reload");
            float movement=0,deadline=Time.realtimeSinceStartup+12;
            while(equipment.Reloading && Time.realtimeSinceStartup<deadline)
            {
                movement=Mathf.Max(movement,Vector3.Distance(storedRod,equipment.HeldModel.transform.InverseTransformPoint(ramrod.position)));
                Assert.That(equipment.Loaded,Is.False,"weapon remains unloaded throughout the animation");
                yield return null;
            }
            Assert.That(equipment.Reloading,Is.False,"reload completes");
            Assert.That(equipment.Loaded,Is.True);
            Assert.That(Time.time-began,Is.GreaterThanOrEqualTo(7.8f),"reload lasts the eight-second gameplay interval");
            Assert.That(movement,Is.GreaterThan(.1f),"the imported ramrod visibly moves during reloading");
            Assert.That(Vector3.Distance(storedRod,equipment.HeldModel.transform.InverseTransformPoint(ramrod.position)),Is.LessThan(.001f),"ramrod returns to its stored position");
            Assert.That(session.Connection.State.revision,Is.EqualTo(beforeShot),"untargeted fire and reload do not change the ranch");
            session.Connection.Send(new Request{action="hunting-mode",id=1});
            yield return Wait(()=>session.Connection.State.huntingEnabled,"shared hunting opt-in accepted");
            var deer=targets.First(t=>t.kind=="hunt" && t.id==0);
            AimAt(walker,deer);yield return null;
            Assert.That(equipment.Use(),Is.True,"loaded muzzleloader fires at the designated deer");
            yield return Wait(()=>session.Connection.State.meat==2,"aimed shot awards two venison through authority");
            yield return Wait(()=>!deer.gameObject.activeSelf,"harvested deer disappears without graphic effects");
            Assert.That(session.Connection.State.cooldowns.Any(c=>c.kind=="hunt" && c.id==deer.id && c.ready>session.Connection.Latest.utc),Is.True,"deer recovery is saved");
        }
        finally
        {
            // Close the server lease before removing its isolated save directory.
            if(session!=null)
            {
                var owner=session.gameObject;
                UnityEngine.Object.DestroyImmediate(session);
                foreach(var component in owner.GetComponents<MonoBehaviour>())
                    if(component is FrontierEquipment || component is FrontierWorld)UnityEngine.Object.DestroyImmediate(component);
            }
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
    }

    static void AimAt(FirstPersonWalker walker,FrontierTarget target)
    {
        var collider=target.GetComponent<BoxCollider>();Vector3 center=collider.transform.TransformPoint(collider.center);
        var diagnostics=new StringBuilder();
        diagnostics.AppendLine("Target position="+target.transform.position.ToString("F3")+" center="+center.ToString("F3")+
            " ground="+ValleyShape.Height(target.transform.position.x,target.transform.position.z).ToString("F3")+
            " scale="+target.transform.lossyScale+" active="+target.gameObject.activeInHierarchy+" colliderEnabled="+collider.enabled+" layer="+target.gameObject.layer);
        for(int approach=0;approach<8;approach++)
        {
            Vector3 offset=Quaternion.Euler(0,approach*45,0)*Vector3.back*2;
            walker.Teleport(ValleyShape.Ground(target.transform.position.x+offset.x,target.transform.position.z+offset.z,.15f));
            walker.view.transform.LookAt(center);Physics.SyncTransforms();
            var ray=new Ray(walker.view.transform.position,walker.view.transform.forward);
            bool hitScene=Physics.Raycast(ray,out var hit,4);
            if(hitScene && hit.collider.GetComponentInParent<FrontierTarget>()==target)return;
            bool hitTarget=collider.Raycast(ray,out var targetHit,4);
            diagnostics.AppendLine("Approach "+approach+" feet="+walker.transform.position.ToString("F3")+" eye="+ray.origin.ToString("F3")+
                " direction="+ray.direction.ToString("F3")+" centerDistance="+Vector3.Distance(ray.origin,center).ToString("F3")+
                " directTargetHit="+hitTarget+(hitTarget?" at "+targetHit.distance.ToString("F3"):"")+
                " sceneHit="+(hitScene?Hierarchy(hit.collider.transform)+" at "+hit.distance.ToString("F3")+" position="+hit.point.ToString("F3"):"none"));
            foreach(var other in Physics.RaycastAll(ray,10).OrderBy(h=>h.distance))
                diagnostics.AppendLine("  Along ray: "+Hierarchy(other.collider.transform)+" distance="+other.distance.ToString("F3")+" position="+other.point.ToString("F3")+" layer="+other.collider.gameObject.layer);
        }
        Assert.Fail("No unobstructed tool approach to "+target.kind+" site "+target.id+"\n"+diagnostics);
    }

    static string Hierarchy(Transform item)
    {
        string path=item.name;while(item.parent!=null){item=item.parent;path=item.name+"/"+path;}return path;
    }

    static IEnumerator Wait(Func<bool> condition,string step)
    {
        float deadline=Time.realtimeSinceStartup+10;
        while(!condition() && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(condition(),Is.True,step);
    }

    [TestCase(0,1,0)]
    [TestCase(1,1,45)]
    [TestCase(1,0,90)]
    [TestCase(1,-1,135)]
    [TestCase(0,-1,180)]
    [TestCase(-1,-1,225)]
    [TestCase(-1,0,270)]
    [TestCase(-1,1,315)]
    public void CompassBearingMatchesWorldCardinals(float x,float z,float degrees)
    {
        Vector3 origin=new Vector3(-90,23,-235);
        Assert.That(TrailCompass.Bearing(origin,origin+new Vector3(x,0,z)),Is.EqualTo(degrees).Within(.001f));
    }
}
