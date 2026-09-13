using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ExplorersByNature
{
    /// <summary>Opt-in close views of the actual western grooms in the live valley.</summary>
    public sealed class WesternHairCapture : MonoBehaviour
    {
        [Serializable] sealed class Proof
        {
            public string revision,quality="High";
            public int frames,fps=24,width,height,horseGuides,playerGuides;
            public float horseMaximumBend,playerMaximumBend;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--western-hair-capture")<0)return;
            var ranch=FindFirstObjectByType<RanchSession>();
            if(ranch!=null)ranch.DataDirectory=Path.Combine(Path.GetTempPath(),"western-hair-"+Guid.NewGuid().ToString("N"));
            var walker=FindFirstObjectByType<FirstPersonWalker>();if(walker!=null)walker.Automated=true;
            new GameObject("Western hair capture").AddComponent<WesternHairCapture>();
        }
        IEnumerator Start()
        {
            float deadline=Time.realtimeSinceStartup+60;
            while(ReferenceGrove.Current==null || !ReferenceGrove.Current.Ready || HorseRiding.Current==null || HorseRiding.Current.Horse==null || WindWeather.Current==null)
            {if(Time.realtimeSinceStartup>deadline){Fail("scene initialization");yield break;}yield return null;}
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--hair-output");
            string output=Path.GetFullPath(at>=0 && at+1<args.Length?args[at+1]:Path.Combine(Application.persistentDataPath,"WesternHairCapture"));
            Directory.CreateDirectory(output);
            var session=FindFirstObjectByType<WalkSession>();session.ApplyQuality(true);
            var walker=FindFirstObjectByType<FirstPersonWalker>();walker.Automated=true;walker.SetMenu(false);
            SkyWeather.Current.SetPreview(.42f,0);WindWeather.Current.SetMode(2);ReferenceGrove.PhotoMode=true;AudioListener.volume=0;
            var horse=HorseRiding.Current.Horse;var horseHair=horse.GetComponentInChildren<WesternHair>();
            var actor=new GameObject("Hair inspection explorer");actor.transform.position=ValleyShape.Ground(-86,-221,.1f);
            var avatar=actor.AddComponent<PlayerAvatar>();avatar.SetModel("homesteader");
            var playerHair=avatar.Model==null?null:avatar.Model.GetComponent<WesternHair>();
            if(horseHair==null || playerHair==null){Fail("missing western groom");yield break;}
            var proof=new Proof {revision=session.buildRevision,width=Screen.width,height=Screen.height,horseGuides=horseHair.GuideCount,playerGuides=playerHair.GuideCount};
            if(QualitySettings.GetQualityLevel()!=1 || proof.width!=1280 || proof.height!=720){Fail("expected High at 1280x720");yield break;}
            walker.view.fieldOfView=45;
            Time.captureFramerate=proof.fps;
            string[] shots={"horse-mane","horse-tail","explorer-temple","explorer-back"};
            for(int shot=0;shot<4;shot++)
            {
                Transform subject=shot<2?horse.transform:actor.transform;
                Vector3 target=shot==0?new Vector3(-.05f,1.80f,.64f):shot==1?new Vector3(0,.87f,-1.08f):new Vector3(0,1.64f,-.035f);
                Vector3 offset=shot==0?new Vector3(-1.2f,.12f,-.2f):shot==1?new Vector3(-.95f,.18f,-.85f):shot==2?new Vector3(.48f,.03f,.60f):new Vector3(-.48f,.06f,-.58f);
                Vector3 focus=subject.TransformPoint(target);
                walker.Teleport(ValleyShape.Ground(focus.x,focus.z,.15f));
                walker.view.transform.position=focus+subject.TransformDirection(offset);walker.view.transform.LookAt(focus);
                for(int settle=0;settle<24;settle++)yield return null;
                for(int frame=0;frame<60;frame++)
                {
                    yield return new WaitForEndOfFrame();
                    var texture=ScreenCapture.CaptureScreenshotAsTexture();byte[] png=texture.EncodeToPNG();Destroy(texture);
                    File.WriteAllBytes(Path.Combine(output,$"frame-{proof.frames:D5}.png"),png);
                    if(frame==30)File.WriteAllBytes(Path.Combine(output,shots[shot]+".png"),png);
                    proof.frames++;
                    if(shot<2)proof.horseMaximumBend=Mathf.Max(proof.horseMaximumBend,horseHair.MaximumBend);
                    else proof.playerMaximumBend=Mathf.Max(proof.playerMaximumBend,playerHair.MaximumBend);
                }
                Debug.Log("WESTERN_HAIR_SHOT "+shots[shot]);
            }
            Time.captureFramerate=0;
            File.WriteAllText(Path.Combine(output,"proof.json"),JsonUtility.ToJson(proof,true));
            if(proof.frames!=240 || proof.horseGuides!=370 || proof.playerGuides!=110 || proof.horseMaximumBend<.001f || proof.playerMaximumBend<.001f)
            {Fail("missing groom motion evidence");yield break;}
            Debug.Log("WESTERN_HAIR_COMPLETE "+proof.revision);Application.Quit(0);
        }
        static void Fail(string reason){Time.captureFramerate=0;Debug.LogError("WESTERN_HAIR_FAILED "+reason);Application.Quit(2);}
    }
}
