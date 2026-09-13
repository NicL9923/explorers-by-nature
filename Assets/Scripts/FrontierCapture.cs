using System;
using System.Collections;
using System.IO;
using System.Linq;
using ExplorersByNature.Shared;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class FrontierCapture : MonoBehaviour
    {
        [Serializable] sealed class Proof {public string revision;public int woodGained,stoneGained,meatGained,frames,fps=24;public float riddenMetres;public bool reloaded;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--frontier-capture")<0)return;
            FindFirstObjectByType<RanchSession>().DataDirectory=Path.Combine(Path.GetTempPath(),"frontier-capture-"+Guid.NewGuid().ToString("N"));
            FindFirstObjectByType<FirstPersonWalker>().Automated=true;new GameObject("Frontier capture").AddComponent<FrontierCapture>();
        }
        string output;FirstPersonWalker walker;Proof proof;RanchSession ranch;
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--capture-output");output=args[at+1];Directory.CreateDirectory(output);
            walker=FindFirstObjectByType<FirstPersonWalker>();ranch=FindFirstObjectByType<RanchSession>();
            float deadline=Time.realtimeSinceStartup+45;
            while(ranch.Connection?.State==null || HorseRiding.Current?.Horse==null || !ReferenceGrove.Current.Ready)
            {if(Time.realtimeSinceStartup>deadline){Fail("initialization");yield break;}yield return null;}
            var session=FindFirstObjectByType<WalkSession>();session.ApplyQuality(Array.IndexOf(args,"--quality-low")<0);SkyWeather.Current.SetPreview(.42f,0);AudioListener.volume=0;
            proof=new Proof{revision=session.buildRevision};var tool=ranch.GetComponent<FrontierEquipment>();
            for(int kind=0;kind<2;kind++)
            {
                var site=(kind==0?FrontierSites.Wood:FrontierSites.Stone)[0];int before=kind==0?ranch.Connection.State.wood:ranch.Connection.State.stone;
                Position(site.x,site.z-2.5f,new Vector3(site.x,ValleyShape.Height(site.x,site.z)+.5f,site.z));tool.Equip(kind+1);
                yield return new WaitForSeconds(.6f);if(!tool.Use()){Fail("gather refused");yield break;}
                yield return new WaitForSeconds(.42f);yield return new WaitForEndOfFrame();Save(kind==0?"axe":"pickaxe");
                yield return new WaitForSeconds(1.1f);
                int gain=(kind==0?ranch.Connection.State.wood:ranch.Connection.State.stone)-before;
                if(kind==0)proof.woodGained=gain;else proof.stoneGained=gain;
            }
            tool.Equip(0);var riding=HorseRiding.Current;Vector3 hp=riding.Horse.transform.position;
            walker.Teleport(hp+riding.Horse.transform.right*2);walker.transform.rotation=Quaternion.identity;walker.view.transform.localRotation=Quaternion.Euler(12,0,0);walker.SyncLookPitch();
            if(!riding.Mount()){Fail("mount refused");yield break;}
            for(int i=0;i<90;i++){walker.Move(Vector2.up,false,1f/60);yield return null;}
            proof.riddenMetres=Vector3.Distance(hp,riding.Horse.transform.position);
            yield return new WaitForEndOfFrame();Save("riding");riding.Release();
            var deer=FrontierSites.Deer[0];Position(deer.x,deer.z-7,new Vector3(deer.x,ValleyShape.Height(deer.x,deer.z)+.9f,deer.z));
            ranch.Connection.Send(new Request{action="hunting-mode",id=1});deadline=Time.realtimeSinceStartup+5;
            while(!ranch.Connection.State.huntingEnabled && Time.realtimeSinceStartup<deadline)yield return null;
            int meat=ranch.Connection.State.meat;tool.Equip(3);yield return new WaitForSeconds(.5f);
            if(!tool.Use()){Fail("fire refused");yield break;}
            yield return new WaitForEndOfFrame();Save("muzzle-flash");
            yield return new WaitForSeconds(.5f);proof.meatGained=ranch.Connection.State.meat-meat;
            if(!tool.BeginReload()){Fail("reload refused");yield break;}
            Time.captureFramerate=24;
            for(int frame=0;frame<240;frame++)
            {
                yield return new WaitForEndOfFrame();Save($"frame-{frame:D5}");proof.frames++;
                if(frame==110)Save("reload");
            }
            Time.captureFramerate=0;proof.reloaded=tool.Loaded;
            File.WriteAllText(Path.Combine(output,"proof.json"),JsonUtility.ToJson(proof,true));
            if(proof.woodGained!=8 || proof.stoneGained!=6 || proof.meatGained!=2 || proof.riddenMetres<3 || !proof.reloaded){Fail("gameplay proof failed");yield break;}
            Debug.Log("FRONTIER_CAPTURE_COMPLETE "+proof.revision);Application.Quit(0);
        }
        void Position(float x,float z,Vector3 target)
        {walker.Teleport(ValleyShape.Ground(x,z,.15f));walker.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(target-walker.transform.position,Vector3.up));walker.view.transform.LookAt(target);walker.SyncLookPitch();}
        void Save(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);}
        static void Fail(string why){Time.captureFramerate=0;Debug.LogError("FRONTIER_CAPTURE_FAILED "+why);Application.Quit(2);}
    }
}
