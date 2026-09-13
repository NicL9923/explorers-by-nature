using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ExplorersByNature
{
    // Opt-in recordings of the actual player, isolated from the family's saved ranch.
    public sealed class NatureMotionCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--nature-motion-capture") < 0) return;
            var ranch=FindFirstObjectByType<RanchSession>();
            if(ranch!=null)ranch.DataDirectory=Path.Combine(Path.GetTempPath(),"nature-motion-"+Guid.NewGuid().ToString("N"));
            var walker=FindFirstObjectByType<FirstPersonWalker>();if(walker!=null)walker.Automated=true;
            new GameObject("Nature motion capture").AddComponent<NatureMotionCapture>();
        }
        [Serializable] sealed class Proof
        {
            public string revision, quality, actualQualityName;
            public int frames, fps=24, activeFurClumps, waterImpacts, actualQualityIndex;
            public float maximumFurBend;
        }
        static string Argument(string name,string fallback)
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);
            return i>=0 && i+1<args.Length?args[i+1]:fallback;
        }
        IEnumerator Start()
        {
            float deadline=Time.realtimeSinceStartup+60;
            while(ReferenceGrove.Current==null || !ReferenceGrove.Current.Ready || WindWeather.Current==null || RiverDynamics.Current==null)
            {
                if(Time.realtimeSinceStartup>deadline){Fail("scene did not initialize");yield break;}
                yield return null;
            }
            var walker=FindFirstObjectByType<FirstPersonWalker>();var session=FindFirstObjectByType<WalkSession>();
            string output=Path.GetFullPath(Argument("--motion-output",Path.Combine(Application.persistentDataPath,"MotionCapture")));
            Directory.CreateDirectory(output);
            var proof=new Proof { revision=session.buildRevision,quality=Argument("--motion-quality","high") };
            session.ApplyQuality(proof.quality=="high");walker.Automated=true;walker.SetMenu(false);
            proof.actualQualityIndex=QualitySettings.GetQualityLevel();
            proof.actualQualityName=QualitySettings.names[proof.actualQualityIndex];
            if(!string.Equals(proof.actualQualityName,proof.quality,StringComparison.OrdinalIgnoreCase)){Fail("requested quality is unavailable in player");yield break;}
            SkyWeather.Current.SetPreview(.42f,0);WindWeather.Current.SetMode(2);ReferenceGrove.PhotoMode=true;AudioListener.volume=0;
            yield return new WaitForSecondsRealtime(2);
            Time.captureFramerate=proof.fps;
            var actors=FindObjectsByType<AnimalMotion>(FindObjectsSortMode.None);
            var cow=actors.FirstOrDefault(a=>a.species=="Clover");
            if(cow==null){Fail("missing cow for hair inspection");yield break;}
            for(int shot=0;shot<4;shot++)
            {
                walker.view.fieldOfView=shot==0?75:shot==3?65:55;
                if(shot==0)Position(walker,ValleyShape.Ground(ValleyShape.TrailX(-158),-158,.15f),ValleyShape.Ground(ValleyShape.TrailX(-148)+4,-148,8));
                if(shot==1)Position(walker,ValleyShape.Ground(ValleyShape.TrailX(-146)-7.5f,-146,.15f),ValleyShape.Ground(ValleyShape.TrailX(-144)-6.2f,-144,1.1f));
                if(shot==2)
                {
                    var tail=cow.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Tail");
                    Vector3 stand=cow.transform.position+cow.transform.right*1.6f-cow.transform.forward*2.1f;
                    Position(walker,ValleyShape.Ground(stand.x,stand.z,.15f),tail==null?cow.transform.position+Vector3.up:tail.position-Vector3.up*.55f);
                }
                if(shot==3)RiverDynamics.Current.Visit();
                // Settle culling and fur initialization before recording each real camera view.
                for(int settle=0;settle<12;settle++)yield return null;
                int count=shot==3?192:96;
                int impactsBefore=RiverDynamics.Current.ImpactCount;
                var fur=FindObjectsByType<AnimalFur>(FindObjectsSortMode.None);
                for(int frame=0;frame<count;frame++)
                {
                    if(shot==3 && frame==18 && !RiverDynamics.Current.ThrowPebble()){Fail("pebble throw refused");yield break;}
                    yield return new WaitForEndOfFrame();
                    var capture=ScreenCapture.CaptureScreenshotAsTexture();
                    byte[] png=capture.EncodeToPNG();Destroy(capture);
                    File.WriteAllBytes(Path.Combine(output,$"frame-{proof.frames:D5}.png"),png);
                    if(frame==count/2)File.WriteAllBytes(Path.Combine(output,new[]{"wind.png","fur.png","tail.png","river.png"}[shot]),png);
                    proof.frames++;
                    foreach(var coat in fur)
                    {
                        proof.activeFurClumps=Mathf.Max(proof.activeFurClumps,coat.ActiveClumps);
                        proof.maximumFurBend=Mathf.Max(proof.maximumFurBend,coat.MaximumBend);
                    }
                }
                if(shot==3)proof.waterImpacts=RiverDynamics.Current.ImpactCount-impactsBefore;
                Debug.Log("NATURE_MOTION_SHOT "+shot+" frames="+proof.frames);
            }
            Time.captureFramerate=0;
            File.WriteAllText(Path.Combine(output,"proof.json"),JsonUtility.ToJson(proof,true));
            if(proof.activeFurClumps!=(proof.quality=="high"?5400:600) || proof.maximumFurBend<.001f || proof.waterImpacts<1){Fail("motion proof missing fur response or water impact");yield break;}
            Debug.Log("NATURE_MOTION_COMPLETE "+proof.revision);Application.Quit(0);
        }
        static void Position(FirstPersonWalker walker,Vector3 stand,Vector3 target)
        {
            walker.Teleport(stand);walker.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(target-stand,Vector3.up));
            walker.view.transform.LookAt(target);walker.SyncLookPitch();
        }
        static void Fail(string message){Time.captureFramerate=0;Debug.LogError("NATURE_MOTION_FAILED "+message);Application.Quit(2);}
    }
}
