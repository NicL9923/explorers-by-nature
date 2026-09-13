using System;
using System.Collections;
using System.IO;
using System.Linq;
using ExplorersByNature.Shared;
using UnityEngine;

namespace ExplorersByNature
{
    // Opt-in standalone integration check. It uses a supplied test server or isolated local save.
    public sealed class RanchSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {if(Array.IndexOf(Environment.GetCommandLineArgs(),"--ranch-smoke")>=0)new GameObject("Ranch integration check").AddComponent<RanchSmoke>();}
        bool commandFailed,hadModel,modelOverridden;
        string originalModel;
        [Serializable] sealed class AppearanceEvidence
        {
            public string initialOwn,initialPeer,finalOwn,finalPeer;
            public bool initialAvatar,finalAvatar;
            public int revisionBefore,revisionAfter;
        }
        bool PeerVisible(RanchSession session,string ownModel,string peerModel)
        {
            var reply=session.Connection.Latest;
            return reply?.players!=null && reply.players.Any(p=>p.id==reply.playerId && p.model==ownModel)
                && reply.players.Any(p=>p.id!=reply.playerId && p.model==peerModel)
                && FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None).Any(a=>!a.Preview && a.ModelId==peerModel && a.Model!=null && a.Model.activeInHierarchy);
        }
        IEnumerator WaitForAppearance(RanchSession session,string ownModel,string peerModel)
        {
            float deadline=Time.realtimeSinceStartup+20;
            while(!PeerVisible(session,ownModel,peerModel) && Time.realtimeSinceStartup<deadline)yield return null;
            if(PeerVisible(session,ownModel,peerModel))yield break;
            commandFailed=true;Debug.LogError("RANCH_SMOKE_FAILED appearance own="+ownModel+" peer="+peerModel);Application.Quit(4);
        }
        void OnDestroy()
        {
            if(!modelOverridden)return;
            if(hadModel)PlayerPrefs.SetString("ExplorerModel",originalModel);else PlayerPrefs.DeleteKey("ExplorerModel");
        }
        IEnumerator SendAndWait(RanchSession session,FirstPersonWalker walker,Request request)
        {
            var connection=session.Connection;
            float deadline=Time.realtimeSinceStartup+12;
            // Publish teleports before the worker attaches a pose to this command.
            connection.Tick(walker.transform.position,walker.transform.eulerAngles.y,PlayerWardrobe.SelectedId);
            while(!connection.Connected && Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                connection.Tick(walker.transform.position,walker.transform.eulerAngles.y,PlayerWardrobe.SelectedId);
            }
            int expectedRevision=(connection.State?.revision??0)+1;
            if(connection.Connected)connection.Send(request);
            // This isolated smoke has one writer. Each command commits exactly one revision.
            // Hold the pose and do not issue another command until that commit arrives.
            while((connection.State?.revision??0)<expectedRevision && Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                connection.Tick(walker.transform.position,walker.transform.eulerAngles.y,PlayerWardrobe.SelectedId);
            }
            if((connection.State?.revision??0)>=expectedRevision)yield break;
            commandFailed=true;
            Debug.LogError("RANCH_SMOKE_FAILED command="+JsonUtility.ToJson(request)+" expectedRevision="+expectedRevision+
                " status="+connection.Status+" message="+connection.Message+" state="+JsonUtility.ToJson(connection.State,true));
            Application.Quit(3);
        }
        IEnumerator Start()
        {
            yield return null;
            var session=FindFirstObjectByType<RanchSession>();var walker=FindFirstObjectByType<FirstPersonWalker>();
            Debug.Log("RANCH_SMOKE_REVISION "+FindFirstObjectByType<WalkSession>().buildRevision);
            walker.Automated=true;walker.SetMenu(false);
            string[] args=Environment.GetCommandLineArgs();bool observer=Array.IndexOf(args,"--smoke-observer")>=0;
            bool appearanceCheck=Array.IndexOf(args,"--smoke-appearance")>=0;
            int outputAt=Array.IndexOf(args,"--smoke-output");string output=outputAt>=0&&outputAt+1<args.Length?args[outputAt+1]:Path.Combine(Application.persistentDataPath,"Smoke");Directory.CreateDirectory(output);
            var appearance=new AppearanceEvidence {initialOwn=observer?PlayerModels.Homesteader:PlayerModels.RanchHand,initialPeer=observer?PlayerModels.RanchHand:PlayerModels.Homesteader};
            if(appearanceCheck)
            {
                hadModel=PlayerPrefs.HasKey("ExplorerModel");originalModel=PlayerPrefs.GetString("ExplorerModel");modelOverridden=true;
                PlayerPrefs.SetString("ExplorerModel",appearance.initialOwn);
            }
            if(Array.IndexOf(args,"--ranch-host")<0){session.DataDirectory=Path.Combine(Path.GetTempPath(),"ranch-smoke-"+Guid.NewGuid());session.StartSolo();}
            float deadline=Time.realtimeSinceStartup+20;
            while(session.Connection?.State==null&&Time.realtimeSinceStartup<deadline)yield return null;
            if(session.Connection?.State==null){Debug.LogError("RANCH_SMOKE_FAILED no connection");Application.Quit(2);yield break;}
            if(appearanceCheck)
            {
                // Overlapping remote avatars are intentionally hidden at the shared spawn.
                walker.Teleport(ValleyShape.Ground(observer?-81:-83,-247,.1f));
                yield return WaitForAppearance(session,appearance.initialOwn,appearance.initialPeer);if(commandFailed)yield break;
                appearance.initialAvatar=true;
                string ready=Path.Combine(output,"observer-appearance-ready");
                if(observer)File.WriteAllText(ready,"ready");
                else
                {
                    deadline=Time.realtimeSinceStartup+20;
                    while(!File.Exists(ready)&&Time.realtimeSinceStartup<deadline)yield return null;
                    if(!File.Exists(ready)){Debug.LogError("RANCH_SMOKE_FAILED observer initial appearance acknowledgement");Application.Quit(4);yield break;}
                }
            }
            walker.Teleport(ValleyShape.Ground(-93,-239,.3f));
            if(!observer)
            {
                for(int x=-31;x<=-30;x++)for(int z=-79;z<=-78;z++)
                {
                    yield return SendAndWait(session,walker,new Request{action="place",kind="foundation",x=x,z=z});if(commandFailed)yield break;
                    yield return SendAndWait(session,walker,new Request{action="place",kind="roof",x=x,z=z});if(commandFailed)yield break;
                    yield return SendAndWait(session,walker,new Request{action="place",kind=z==-79?"door":"wall",x=x,z=z,turn=z==-79?2:0});if(commandFailed)yield break;
                }
                yield return SendAndWait(session,walker,new Request{action="place",kind="flower",x=-29,z=-78});if(commandFailed)yield break;
                yield return SendAndWait(session,walker,new Request{action="place",kind="fence",x=-32,z=-79,turn=1});if(commandFailed)yield break;
                walker.Teleport(ValleyShape.Ground(-99,-230,.3f));
                yield return SendAndWait(session,walker,new Request{action="milk"});if(commandFailed)yield break;
                walker.Teleport(ValleyShape.Ground(-108,-230,.3f));
                yield return SendAndWait(session,walker,new Request{action="eggs"});if(commandFailed)yield break;
                foreach(string action in new[]{"pack","picnic","claim"})
                {
                    walker.Teleport(ValleyShape.Ground(action=="picnic"?Ranch.ExpeditionX:Ranch.HomeX,action=="picnic"?Ranch.ExpeditionZ:Ranch.HomeZ,.3f));
                    yield return SendAndWait(session,walker,new Request{action=action});if(commandFailed)yield break;
                }
                walker.Teleport(ValleyShape.Ground(-90,-237,.3f));
                string[] furnishings={"bench","lantern","trough","flowerbox","campfire","alpineflower"};
                int[] xs={-31,-30,-32,-29,-28,-28},zs={-79,-79,-78,-79,-80,-79};
                for(int i=0;i<furnishings.Length;i++)
                {
                    yield return SendAndWait(session,walker,new Request{action="place",kind=furnishings[i],x=xs[i],z=zs[i]});if(commandFailed)yield break;
                }
            }
            deadline=Time.realtimeSinceStartup+(observer?60:25);
            while((session.Connection.State.pieces.Count<20||session.Connection.State.eggs<3||session.Connection.State.milk<1||session.Connection.State.expeditionStage<3)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(session.Connection.State.pieces.Count<20||session.Connection.State.eggs<3||session.Connection.State.milk<1||session.Connection.State.expeditionStage<3){Debug.LogError("RANCH_SMOKE_FAILED "+session.Connection.Message);Application.Quit(3);yield break;}
            if(appearanceCheck)
            {
                appearance.revisionBefore=session.Connection.State.revision;
                appearance.finalOwn=observer?PlayerModels.Homesteader:PlayerModels.Frontiersman;
                appearance.finalPeer=observer?PlayerModels.Frontiersman:PlayerModels.Homesteader;
                if(!observer)PlayerPrefs.SetString("ExplorerModel",appearance.finalOwn);
                yield return WaitForAppearance(session,appearance.finalOwn,appearance.finalPeer);if(commandFailed)yield break;
                appearance.finalAvatar=true;appearance.revisionAfter=session.Connection.State.revision;
                if(appearance.revisionBefore!=appearance.revisionAfter){Debug.LogError("RANCH_SMOKE_FAILED appearance changed ranch revision");Application.Quit(4);yield break;}
                File.WriteAllText(Path.Combine(output,observer?"observer-appearance.json":"builder-appearance.json"),JsonUtility.ToJson(appearance,true));
                if(!observer)
                {
                    deadline=Time.realtimeSinceStartup+20;
                    while(!File.Exists(Path.Combine(output,"observer-appearance.json"))&&Time.realtimeSinceStartup<deadline)yield return null;
                    if(!File.Exists(Path.Combine(output,"observer-appearance.json"))){Debug.LogError("RANCH_SMOKE_FAILED observer live appearance acknowledgement");Application.Quit(4);yield break;}
                }
            }
            walker.Teleport(ValleyShape.Ground(observer?-81:-83,-247,.3f));walker.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(ValleyShape.Ground(-99,-230,1)-walker.transform.position,Vector3.up));walker.view.transform.LookAt(ValleyShape.Ground(-99,-230,1));
            yield return new WaitForSeconds(4);
            File.WriteAllText(Path.Combine(output,observer?"observer.json":"builder.json"),JsonUtility.ToJson(session.Connection.State,true));
            ScreenCapture.CaptureScreenshot(Path.Combine(output,observer?"observer.png":"builder.png"));Debug.Log("RANCH_SMOKE_PASSED "+session.Connection.State.revision);
            yield return new WaitForSeconds(2);
            if (Array.IndexOf(args, "--art-tour") >= 0)
            {
                var positions = new[] { new Vector2(-95.5f, -221.5f), new Vector2(-105.2f, -222.5f), new Vector2(-89, -235), new Vector2(-77, -176), new Vector2(-20, -155), new Vector2(-47, 135) };
                var targets = new[] { new Vector2(-99, -226), new Vector2(-108, -226), new Vector2(-87, -234), new Vector2(-115, -135), new Vector2(35, -70), new Vector2(130, 365) };
                var names = new[] { "clover-close", "hens-close", "flowers-close", "woodland", "river", "overlook" };
                for (int i = 0; i < positions.Length; i++)
                {
                    walker.Teleport(ValleyShape.Ground(positions[i].x, positions[i].y, .1f));
                    walker.view.transform.LookAt(ValleyShape.Ground(targets[i].x, targets[i].y, i == 0 ? 1 : i == 1 ? .5f : i == 2 ? .3f : 5));
                    yield return new WaitForSeconds(1);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, names[i] + ".png"));
                    yield return new WaitForSeconds(.3f);
                }
            }
            if (Array.IndexOf(args, "--art-tour") >= 0)
            {
                foreach(string species in new[]{"Mallard","Beaver","Red fox"})
                {
                    HabitatAnimal chosen=null;
                    foreach(var animal in FindObjectsByType<HabitatAnimal>(FindObjectsSortMode.None))
                        if(animal.name==species){chosen=animal;break;}
                    if(chosen==null)throw new Exception("Missing wildlife for photo tour: "+species);
                    chosen.enabled=false;
                    var animalTransform=chosen.transform;
                    walker.Teleport(ValleyShape.Ground(animalTransform.position.x-3,animalTransform.position.z,.1f));
                    walker.view.fieldOfView=50;
                    walker.view.transform.position=animalTransform.position+animalTransform.forward*1.6f+animalTransform.right*.9f+Vector3.up*(species=="Beaver"?1.35f:.85f);
                    walker.view.transform.LookAt(animalTransform.position+Vector3.up*.3f);
                    yield return new WaitForSeconds(.5f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,species.Replace(" ","-").ToLowerInvariant()+".png"));
                    yield return new WaitForSeconds(.3f);
                }
            }
            if(!observer && Array.IndexOf(args,"--art-tour")>=0)
            {
                var comfort=FindFirstObjectByType<RanchComfort>();
                foreach(var prop in FindObjectsByType<PropComfort>(FindObjectsSortMode.None))if(prop.Kind=="bench")
                {
                    comfort.Sit(prop);yield return null;if(!comfort.Seated)throw new Exception("Bench did not seat explorer");comfort.Stand();break;
                }
                HabitatAnimal fox=null;
                foreach(var animal in FindObjectsByType<HabitatAnimal>(FindObjectsSortMode.None))if(animal.name=="Red fox"){fox=animal;break;}
                if(fox!=null)
                {
                    fox.enabled=true;walker.Teleport(ValleyShape.Ground(fox.transform.position.x-9,fox.transform.position.z-5,.1f));
                    // Wait for the natural walking interval before documenting the gait.
                    float waitForWalk=Time.time+15;Vector3 stillPosition=fox.transform.position;
                    while(Vector3.Distance(stillPosition,fox.transform.position)<.03f && Time.time<waitForWalk)yield return null;
                    for(int frame=0;frame<24;frame++)
                    {
                        walker.view.fieldOfView=50;
                        walker.view.transform.position=fox.transform.position+fox.transform.forward*1.7f+fox.transform.right+Vector3.up*.85f;
                        walker.view.transform.LookAt(fox.transform.position+Vector3.up*.38f);
                        yield return new WaitForSeconds(.15f);
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"fox-motion-"+frame.ToString("D2")+".png"));
                    }
                    yield return new WaitForSeconds(.3f);
                }
                SkyWeather.Current.SetPreview(.77f,.65f);
                walker.view.fieldOfView=75;
                walker.Teleport(ValleyShape.Ground(-83,-247,.3f));walker.view.transform.localPosition=Vector3.up*1.65f;
                walker.view.transform.LookAt(ValleyShape.Ground(-90,-236,1));
                yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(Path.Combine(output,"evening-ranch.png"));yield return new WaitForSeconds(.4f);
                SkyWeather.Current.SetPreview(.69f,0);
                walker.Teleport(ValleyShape.Trail(-105,.1f));walker.view.fieldOfView=65;walker.view.transform.localPosition=Vector3.up*1.65f;
                walker.view.transform.LookAt(ValleyShape.Ground(ValleyShape.TrailX(-75)-18,-75,6));
                yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(output,"golden-woodland.png"));yield return new WaitForSeconds(.4f);
            }
            Application.Quit();
        }
    }
}
