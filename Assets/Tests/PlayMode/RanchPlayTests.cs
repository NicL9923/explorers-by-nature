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
            var renderers=cow.GetComponentsInChildren<Renderer>();
            Assert.That(ValleyWorld.ModelHeight(cow.gameObject),Is.InRange(1.5f,2.5f),"Blender cow imports at life size");
            foreach(var renderer in renderers)
                Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo(renderer.name=="Simulated coat"?"Explorers/AnimalFur":"Universal Render Pipeline/Lit"),"Body and simulated coat retain their intended URP shaders");
        }
        finally {UnityEngine.Object.DestroyImmediate(session);if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    [UnityTest]
    public IEnumerator FurnishingsSitAboveDeckAndBenchReleasesPlayer()
    {
        yield return SceneManager.LoadSceneAsync("Pinewatch");yield return null;
        var walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonWalker>();walker.SetMenu(true);
        var comfort=UnityEngine.Object.FindFirstObjectByType<RanchComfort>();
        var state=new RanchState();state.pieces.Add(new Piece{kind="foundation",x=-30,z=-78,id=7001});
        var benchPiece=new Piece{kind="bench",x=-30,z=-78,id=7002};
        var bench=RanchVisuals.Piece(benchPiece,false,state);
        var ghost=RanchVisuals.Piece(benchPiece,true,state);
        var lamps=new System.Collections.Generic.List<GameObject>();
        try
        {
            Assert.That(bench.transform.position.y,Is.EqualTo(ValleyShape.Height(-90,-234)+.63f).Within(.001f));
            Assert.That(ghost.transform.position,Is.EqualTo(bench.transform.position),"preview and placed bench share deck height");
            Vector3 approach=walker.transform.position,eye=walker.view.transform.localPosition;
            comfort.Sit(bench.GetComponent<PropComfort>());
            Assert.That(comfort.Seated,Is.True);Assert.That(walker.enabled,Is.False);
            Assert.That(walker.view.transform.position.y,Is.EqualTo(bench.transform.position.y+1.4f).Within(.01f),"seated eyes stay above the bench");
            // Sitting owns the camera while the walker is disabled. Standing must
            // transfer the resulting pitch before normal mouse-look resumes.
            walker.view.transform.localRotation=Quaternion.Euler(24,0,0);
            comfort.Stand();
            Assert.That(walker.transform.position,Is.EqualTo(approach),"standing restores the approach position");
            walker.SetMenu(false);
            yield return null;
            Assert.That(Mathf.DeltaAngle(walker.view.transform.localEulerAngles.x,24),Is.EqualTo(0).Within(.1f),"standing preserves the seated look direction");
            walker.SetMenu(true);
            walker.Teleport(approach);
            Assert.That(walker.enabled,Is.True);Assert.That(walker.transform.position,Is.EqualTo(approach));Assert.That(walker.view.transform.localPosition,Is.EqualTo(eye));
            comfort.Sit(bench.GetComponent<PropComfort>());UnityEngine.Object.Destroy(bench);
            yield return null;yield return null;
            Assert.That(comfort.Seated,Is.False,"deleting an occupied bench releases the player");Assert.That(walker.enabled,Is.True);
            var session=UnityEngine.Object.FindFirstObjectByType<RanchSession>();
            session.BeginPicnic();
            Assert.That(walker.MenuOpen,Is.True,"picnic pauses walking");
            Assert.That(RanchSession.PanelOpen,Is.True,"picnic excludes the general settings overlay");
            yield return null;
            session.EndPicnic(false);
            Assert.That(walker.MenuOpen,Is.False,"cancelling picnic returns control");
            Assert.That(RanchSession.PanelOpen,Is.False,"cancelling clears picnic panel ownership");
            walker.SetMenu(true);
            for(int n=0;n<7;n++)lamps.Add(RanchVisuals.Piece(new Piece{kind=n%2==0?"lantern":"campfire",x=-30+n,z=-78,id=7010+n}));
            yield return new WaitForSeconds(.6f);
            var pooled=comfort.GetComponentsInChildren<Light>(true);
            Assert.That(pooled,Has.Length.EqualTo(4),"many furnishings share only four light components");
            Assert.That(pooled.All(light=>light.shadows==LightShadows.None),Is.True);
            Assert.That(lamps.All(lamp=>lamp.GetComponentsInChildren<Light>(true).Length==0),Is.True,"furnishings never allocate per-prop lights");
        }
        finally {comfort.Stand();if(bench!=null)UnityEngine.Object.Destroy(bench);UnityEngine.Object.Destroy(ghost);foreach(var lamp in lamps)UnityEngine.Object.Destroy(lamp);}
    }
    static WalkSession FindFirstSession()=>UnityEngine.Object.FindFirstObjectByType<WalkSession>();
    static IEnumerator Wait(Func<bool> condition,string step)
    {float deadline=Time.realtimeSinceStartup+8;while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(condition(),Is.True,step);}
}
