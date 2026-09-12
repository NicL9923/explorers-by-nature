using System;
using System.Collections;
using System.IO;
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
        IEnumerator Start()
        {
            yield return null;
            var session=FindFirstObjectByType<RanchSession>();var walker=FindFirstObjectByType<FirstPersonWalker>();
            walker.Automated=true;walker.SetMenu(false);
            string[] args=Environment.GetCommandLineArgs();bool observer=Array.IndexOf(args,"--smoke-observer")>=0;
            if(Array.IndexOf(args,"--ranch-host")<0){session.DataDirectory=Path.Combine(Path.GetTempPath(),"ranch-smoke-"+Guid.NewGuid());session.StartSolo();}
            float deadline=Time.realtimeSinceStartup+20;
            while(session.Connection?.State==null&&Time.realtimeSinceStartup<deadline)yield return null;
            if(session.Connection?.State==null){Debug.LogError("RANCH_SMOKE_FAILED no connection");Application.Quit(2);yield break;}
            walker.Teleport(ValleyShape.Ground(-93,-239,.3f));
            if(!observer)
            {
                for(int x=-31;x<=-30;x++)for(int z=-79;z<=-78;z++)
                {
                    session.Connection.Send(new Request{action="place",kind="foundation",x=x,z=z});yield return new WaitForSeconds(.2f);
                    session.Connection.Send(new Request{action="place",kind="roof",x=x,z=z});yield return new WaitForSeconds(.2f);
                    session.Connection.Send(new Request{action="place",kind=z==-79?"door":"wall",x=x,z=z,turn=z==-79?2:0});yield return new WaitForSeconds(.2f);
                }
                session.Connection.Send(new Request{action="place",kind="flower",x=-29,z=-78});yield return new WaitForSeconds(.3f);
                session.Connection.Send(new Request{action="place",kind="fence",x=-32,z=-79,turn=1});yield return new WaitForSeconds(.3f);
                walker.Teleport(ValleyShape.Ground(-99,-230,.3f));yield return new WaitForSeconds(.3f);session.Connection.Send(new Request{action="milk"});yield return new WaitForSeconds(.3f);
                walker.Teleport(ValleyShape.Ground(-108,-230,.3f));yield return new WaitForSeconds(.3f);session.Connection.Send(new Request{action="eggs"});
            }
            deadline=Time.realtimeSinceStartup+25;
            while((session.Connection.State.pieces.Count<14||session.Connection.State.eggs<3||session.Connection.State.milk<1)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(session.Connection.State.pieces.Count<14||session.Connection.State.eggs<3||session.Connection.State.milk<1){Debug.LogError("RANCH_SMOKE_FAILED "+session.Connection.Message);Application.Quit(3);yield break;}
            walker.Teleport(ValleyShape.Ground(observer?-81:-83,-247,.3f));walker.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(ValleyShape.Ground(-99,-230,1)-walker.transform.position,Vector3.up));walker.view.transform.LookAt(ValleyShape.Ground(-99,-230,1));
            yield return new WaitForSeconds(4);
            int at=Array.IndexOf(args,"--smoke-output");string output=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.persistentDataPath,"Smoke");Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,observer?"observer.json":"builder.json"),JsonUtility.ToJson(session.Connection.State,true));
            ScreenCapture.CaptureScreenshot(Path.Combine(output,observer?"observer.png":"builder.png"));Debug.Log("RANCH_SMOKE_PASSED "+session.Connection.State.revision);
            yield return new WaitForSeconds(2);Application.Quit();
        }
    }
}
