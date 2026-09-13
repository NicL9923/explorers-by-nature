using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class PlayerModelCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--player-model-capture")<0)return;
            var ranch=FindFirstObjectByType<RanchSession>();ranch.DataDirectory=Path.Combine(Path.GetTempPath(),"player-model-"+Guid.NewGuid().ToString("N"));
            FindFirstObjectByType<FirstPersonWalker>().Automated=true;
            new GameObject("Player model capture").AddComponent<PlayerModelCapture>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--capture-output");
            string output=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.persistentDataPath,"PlayerModels");Directory.CreateDirectory(output);
            float timeout=Time.realtimeSinceStartup+45;
            while(PlayerWardrobe.Current==null || ReferenceGrove.Current==null || !ReferenceGrove.Current.Ready)
            {if(Time.realtimeSinceStartup>timeout){Debug.LogError("PLAYER_CAPTURE_FAILED initialization");Application.Quit(2);yield break;}yield return null;}
            var session=FindFirstObjectByType<WalkSession>();session.ApplyQuality(Array.IndexOf(args,"--quality-low")<0);
            SkyWeather.Current.SetPreview(.42f,0);AudioListener.volume=0;
            var wardrobe=PlayerWardrobe.Current;
            for(int i=0;i<4;i++)
            {
                wardrobe.Open();wardrobe.Choose(i);
                for(int f=0;f<12;f++)yield return null;
                if(wardrobe.PreviewAvatar.Model==null){Debug.LogError("PLAYER_CAPTURE_FAILED missing model");Application.Quit(2);yield break;}
                yield return new WaitForEndOfFrame();
                var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,PlayerAvatar.Ids[i]+".png"),texture.EncodeToPNG());Destroy(texture);
                wardrobe.Close();yield return null;
            }
            Debug.Log("PLAYER_CAPTURE_COMPLETE "+session.buildRevision);Application.Quit(0);
        }
    }
}
