using System;
using System.Collections;
using System.IO;
using System.Linq;
using ExplorersByNature;
using ExplorersByNature.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RanchPlayTests
{
    [UnityTest]
    public IEnumerator RanchBuildsMovesCollectsAndReloads()
    {
        yield return SceneManager.LoadSceneAsync("Pinewatch");yield return null;
        var session=UnityEngine.Object.FindFirstObjectByType<RanchSession>();
        var walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonWalker>();walker.SetMenu(true);
        string dir=Path.Combine(Path.GetTempPath(),"unity-ranch-"+Guid.NewGuid());session.DataDirectory=dir;session.StartSolo();
        try
        {
            yield return Wait(()=>session.Connection?.State!=null,"initial snapshot");
            walker.Teleport(ValleyShape.Ground(-90,-234,.2f));yield return null;
            session.Connection.Send(new Request{action="place",kind="foundation",x=-30,z=-78});
            yield return Wait(()=>session.Connection.State.pieces.Count==1,"foundation committed");
            int id=session.Connection.State.pieces[0].id;
            yield return new WaitForSeconds(1);
            Assert.That(session.Connection.State.pieces.Count,Is.EqualTo(1),"unchanged polls retain the committed world");
            yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<RanchTarget>(FindObjectsSortMode.None).Any(p=>p.pieceId==id),Is.True,"committed piece rendered");
            Physics.SyncTransforms();
            var floor=UnityEngine.Object.FindObjectsByType<RanchTarget>(FindObjectsSortMode.None).First(p=>p.pieceId==id).GetComponent<BoxCollider>();
            Assert.That(floor.Raycast(new Ray(ValleyShape.Ground(-90,-234,5),Vector3.down),out RaycastHit floorHit,10),Is.True,"Floor collider is queryable");
            Assert.That(floorHit.point.y,Is.EqualTo(ValleyShape.Height(-90,-234)+.63f).Within(.01f));
            walker.Teleport(ValleyShape.Ground(-90,-237.5f,.1f));
            var controller=walker.GetComponent<CharacterController>();
            for(int step=0;step<120;step++){controller.Move(new Vector3(0,-4,2)/60);yield return null;}
            Assert.That(walker.transform.position.z,Is.GreaterThan(-234.5f),"Explorer can step onto the foundation without jumping");
            for(int step=0;step<20;step++){controller.Move(Vector3.down*.1f);yield return null;}
            Assert.That(controller.isGrounded,Is.True,"Grounding at "+walker.transform.position+"; foundation top="+(ValleyShape.Height(-90,-234)+.63f));
            Assert.That(walker.transform.position.y,Is.GreaterThan(ValleyShape.Height(-90,-234)+.4f),"Explorer stands on the floor, not beneath it");
            session.Connection.Send(new Request{action="place",kind="wall",x=-30,z=-78,turn=1});
            yield return Wait(()=>session.Connection.State.pieces.Count==2,"wall supported");
            session.Connection.Send(new Request{action="place",kind="flower",x=-29,z=-78});
            yield return Wait(()=>session.Connection.State.pieces.Count==3,"flowers planted");
            int flower=session.Connection.State.pieces.Find(p=>p.kind=="flower").id;
            session.Connection.Send(new Request{action="move",id=flower,kind="flower",x=-29,z=-79});
            yield return Wait(()=>session.Connection.State.pieces.Find(p=>p.id==flower).z==-79,"flower moved");
            walker.Teleport(ValleyShape.Ground(-99,-230,.2f));yield return null;
            session.Connection.Send(new Request{action="milk"});
            yield return Wait(()=>session.Connection.State.milk==1,"milk collected");
            walker.Teleport(ValleyShape.Ground(-108,-230,.2f));yield return null;
            session.Connection.Send(new Request{action="eggs"});
            yield return Wait(()=>session.Connection.State.eggs==3,"eggs collected");
            session.StartSolo();
            yield return Wait(()=>session.Connection?.State?.eggs==3,"saved world reloaded");
            FindFirstSession().ApplyQuality(false);yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<RanchTarget>(FindObjectsSortMode.None).Any(p=>p.pieceId==flower),Is.True,"planted flowers remain on low graphics");
            Assert.That(session.Connection.State.pieces.Count,Is.EqualTo(3));Assert.That(session.Connection.State.milk,Is.EqualTo(1));
            var cow=UnityEngine.Object.FindObjectsByType<RanchTarget>(FindObjectsSortMode.None).First(p=>p.animal=="milk");
            var renderers=cow.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            Assert.That(bounds.size.y,Is.InRange(1.5f,2.5f),"Blender cow imports at life size");
            Assert.That(renderers.All(r=>r.sharedMaterial.shader.name=="Universal Render Pipeline/Lit"),Is.True,"animal materials use URP");
        }
        finally {UnityEngine.Object.DestroyImmediate(session);if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    static WalkSession FindFirstSession()=>UnityEngine.Object.FindFirstObjectByType<WalkSession>();
    static IEnumerator Wait(Func<bool> condition,string step)
    {float deadline=Time.realtimeSinceStartup+8;while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(condition(),Is.True,step);}
}
