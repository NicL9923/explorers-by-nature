using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ExplorersByNature
{
    // Opt-in standalone visual inspection. This never visits or changes a player's ranch save.
    public sealed class ReferenceGroveCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--grove-capture") < 0) return;
            if (Array.IndexOf(args, "--benchmark") >= 0 || Array.IndexOf(args, "--ranch-smoke") >= 0 || Array.IndexOf(args, "--ranch-host") >= 0)
            {
                Debug.LogError("GROVE_CAPTURE_FAILED incompatible automation or remote ranch argument");
                Application.Quit(2);
                return;
            }
            // AfterSceneLoad runs before Start, so RanchSession opens only this isolated save.
            var ranch = FindFirstObjectByType<RanchSession>();
            if (ranch != null) ranch.DataDirectory = Path.Combine(Path.GetTempPath(), "grove-capture-" + Guid.NewGuid().ToString("N"));
            var walker = FindFirstObjectByType<FirstPersonWalker>();
            if (walker != null) walker.Automated = true;
            new GameObject("Reference grove capture").AddComponent<ReferenceGroveCapture>();
        }

        static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        IEnumerator Start()
        {
            float deadline = Time.realtimeSinceStartup + 90;
            while (ReferenceGrove.Current == null || !ReferenceGrove.Current.Ready || SkyWeather.Current == null)
            {
                if (Time.realtimeSinceStartup > deadline) { Fail("scene did not become ready"); yield break; }
                yield return null;
            }
            var walker = FindFirstObjectByType<FirstPersonWalker>();
            var session = FindFirstObjectByType<WalkSession>();
            if (walker == null || walker.view == null || session == null) { Fail("missing first-person camera/session"); yield break; }
            Debug.Log("GROVE_CAPTURE_REVISION "+session.buildRevision);
            string quality = Argument("--grove-quality", "high");
            if (quality != "high" && quality != "low") { Fail("quality must be high or low"); yield break; }
            string output = Path.GetFullPath(Argument("--grove-capture-dir", Path.Combine(Application.persistentDataPath, "GroveCapture")));
            session.ApplyQuality(quality == "high");
            walker.Automated = true;
            walker.view.fieldOfView = 75;
            walker.SetMenu(false);
            ReferenceGrove.PhotoMode = true;
            SkyWeather.Current.SetPreview(.42f, 0);
            AudioListener.volume = 0;
            foreach(var layer in session.world.Ground.terrainData.terrainLayers)
                Debug.Log("GROVE_TERRAIN_SURFACE "+layer.diffuseTexture.name+" format="+layer.diffuseTexture.graphicsFormat+" smoothness="+layer.smoothness);
            var names = new[] { "01-entrance", "02-trail-and-doe", "03-fern-trail", "04-stump-and-floor" };
            float[] positions = { -178, -158, -132, -124 };
            float[] targetZ = { -149, -143, -108, -120 };
            float[] targetOffset = { 0, -4.5f, 0, 5.2f };
            float[] targetHeight = { 1.8f, 1.5f, 1.8f, .4f };
            for (int i = 0; i < names.Length; i++)
            {
                walker.Teleport(ValleyShape.Ground(ValleyShape.TrailX(positions[i])+(i==3?2:0),positions[i],.15f));
                Vector3 target = ValleyShape.Ground(ValleyShape.TrailX(targetZ[i]) + targetOffset[i], targetZ[i], targetHeight[i]);
                walker.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(target - walker.transform.position, Vector3.up));
                walker.view.transform.LookAt(target);
                walker.SyncLookPitch();
                // Let terrain LOD, shadows, and texture streaming settle at each fixed view.
                yield return new WaitForSecondsRealtime(3);
                yield return new WaitForEndOfFrame();
                if (!SaveFrame(Path.Combine(output, names[i] + ".png"))) yield break;
                Debug.Log("GROVE_CAPTURE_FRAME " + names[i] + " position=" + walker.view.transform.position + " quality=" + quality);
            }
            Debug.Log("GROVE_CAPTURE_COMPLETE " + output + " quality=" + quality + " resolution=" + Screen.width + "x" + Screen.height);
            Application.Quit(0);
        }

        static bool SaveFrame(string path)
        {
            Texture2D frame = null;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                frame = ScreenCapture.CaptureScreenshotAsTexture();
                if (frame == null) throw new IOException("rendered screenshot was unavailable");
                File.WriteAllBytes(path, frame.EncodeToPNG());
                return true;
            }
            catch (Exception error) { Fail(error.ToString()); return false; }
            finally { if (frame != null) Destroy(frame); }
        }

        static void Fail(string reason)
        {
            Debug.LogError("GROVE_CAPTURE_FAILED " + reason);
            Application.Quit(2);
        }
    }
}
