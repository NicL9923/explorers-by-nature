using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;

namespace ExplorersByNature
{
    public sealed class WalkSession : MonoBehaviour
    {
        public ValleyWorld world;
        public FirstPersonWalker walker;
        public UniversalRenderPipelineAsset lowPipeline;
        public UniversalRenderPipelineAsset highPipeline;
        public string buildRevision = "development";
        bool high;
        bool benchmark;
        bool groveBenchmark;
        bool quitAfterBenchmark;
        float elapsed;
        float frameTime;
        bool foundOverlook;
        string outputDirectory;
        string status = "Follow the trail to the ridge. There is no hurry.";
        readonly List<float> samples = new List<float>();
        GUIStyle titleStyle;
        GUIStyle textStyle;

        void Start()
        {
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", .8f);
            string[] args = Environment.GetCommandLineArgs();
            groveBenchmark=Array.IndexOf(args,"--grove-benchmark")>=0;
            bool lowArg = Array.IndexOf(args, "--quality-low") >= 0;
            bool highArg = Array.IndexOf(args, "--quality-high") >= 0;
            ApplyQuality(!lowArg && (highArg || PlayerPrefs.GetInt("HighQuality", 1) == 1));
            int output = Array.IndexOf(args, "--benchmark-output");
            outputDirectory = output >= 0 && output + 1 < args.Length ? args[output + 1] : Path.Combine(Application.persistentDataPath, "Benchmarks");
            if (Array.IndexOf(args, "--benchmark") >= 0)
            {
                quitAfterBenchmark = true;
                BeginBenchmark();
            }
        }

        void Update()
        {
            frameTime = Mathf.Lerp(frameTime, Time.unscaledDeltaTime, .03f);
            if (benchmark)
            {
                elapsed += Time.unscaledDeltaTime;
                // Five seconds warm-up, then a deterministic 60-second camera pass.
                float t = Mathf.Clamp01((elapsed - 5) / 60);
                float routeZ=groveBenchmark?Mathf.Lerp(-178,-106,t):Mathf.Lerp(-235,135,t);
                Vector3 position = ValleyShape.Trail(routeZ, .3f);
                walker.Teleport(position);
                Vector3 look = ValleyShape.Trail(routeZ + (groveBenchmark?12:30), groveBenchmark?2:4);
                if (!groveBenchmark && t > .8f) look = Vector3.Lerp(look, new Vector3(130, 180, 365), (t - .8f) * 5);
                walker.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(look - position, Vector3.up));
                walker.view.transform.rotation = Quaternion.LookRotation(look - walker.view.transform.position);
                if (elapsed > 5) samples.Add(Time.unscaledDeltaTime * 1000);
                if (elapsed >= 65) EndBenchmark();
                return;
            }
            if (!walker.MenuOpen && Input.GetKeyDown(KeyCode.F6)) BeginBenchmark();
            if (!walker.MenuOpen && Input.GetKeyDown(KeyCode.F2)) ApplyQuality(!high);
            if (!foundOverlook && Vector3.Distance(walker.transform.position, ValleyShape.Overlook) < 15)
            {
                foundOverlook = true;
                status = "You found Pinewatch Overlook. Stay a while.";
            }
        }

        public void ApplyQuality(bool useHigh)
        {
            high = useHigh;
            walker.view.GetUniversalAdditionalCameraData().renderPostProcessing=high;
            walker.view.GetUniversalAdditionalCameraData().requiresDepthTexture=high;
            QualitySettings.SetQualityLevel(high ? 1 : 0, true);
            QualitySettings.renderPipeline = high ? highPipeline : lowPipeline;
            QualitySettings.lodBias = high ? 1.5f : .75f;
            QualitySettings.globalTextureMipmapLimit = high ? 0 : 1;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            if (world.Ground != null) world.Ground.heightmapPixelError = high ? 5 : 12;
            if (world.DenseGrass != null) world.DenseGrass.SetActive(high);
            FindFirstObjectByType<WoodlandAtmosphere>()?.SetQuality(high);
            PlayerPrefs.SetInt("HighQuality", high ? 1 : 0);
        }

        void BeginBenchmark()
        {
            samples.Clear(); elapsed = 0; benchmark = true; walker.Automated = true;
            walker.SetMenu(false);
            Application.targetFrameRate = -1;
            status = "Walking the benchmark route...";
        }

        void EndBenchmark()
        {
            benchmark = false; walker.Automated = false;
            samples.Sort();
            float sum = 0; int hitches = 0;
            foreach (float sample in samples) { sum += sample; if (sample > 50) hitches++; }
            var result = new BenchmarkResult
            {
                route=groveBenchmark?"Fern Hollow":"Valley", revision = buildRevision, unity = Application.unityVersion, utc = DateTime.UtcNow.ToString("O"),
                weather=SkyWeather.RainAmount>.5f?"Drizzle":"Clear", daylight=SkyWeather.Daylight,
                operatingSystem = SystemInfo.operatingSystem, processor = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount, memoryMB = SystemInfo.systemMemorySize,
                graphics = SystemInfo.graphicsDeviceName, graphicsAPI = SystemInfo.graphicsDeviceType.ToString(),
                graphicsDriver = SystemInfo.graphicsDeviceVersion, graphicsMemoryMB = SystemInfo.graphicsMemorySize,
                quality = high ? "High" : "Low", width = Screen.width, height = Screen.height,
                frames = samples.Count, meanMs = sum / Mathf.Max(1, samples.Count),
                p95Ms = Percentile(.95f), p99Ms = Percentile(.99f), framesOver50Ms = hitches,
                unityAllocatedMB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024),
                processWorkingSetMB = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)
            };
            try
            {
                Directory.CreateDirectory(outputDirectory);
                string stem = Path.Combine(outputDirectory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + result.quality.ToLowerInvariant() + (result.weather=="Drizzle"?"-rain":""));
                File.WriteAllText(stem + ".json", JsonUtility.ToJson(result, true));
                ScreenCapture.CaptureScreenshot(stem + ".png");
                Debug.Log("BENCHMARK_COMPLETE " + stem + ".json");
                status = "Benchmark saved. Escape opens settings.";
                if (quitAfterBenchmark) Invoke(nameof(Quit), 2);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                status = "Could not save benchmark. Check the player log.";
                if (quitAfterBenchmark) Application.Quit(1);
            }
            Application.targetFrameRate = 60;
            if (!quitAfterBenchmark)
            {
                walker.view.transform.localRotation = Quaternion.identity;
                walker.SetMenu(true);
            }
        }

        float Percentile(float fraction) => samples.Count == 0 ? 0 : samples[Mathf.Clamp(Mathf.CeilToInt(samples.Count * fraction) - 1, 0, samples.Count - 1)];
        void Quit() => Application.Quit();

        void OnGUI()
        {
            if(ReferenceGrove.PhotoMode && !walker.MenuOpen)return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            }
            float scale = Mathf.Clamp(Screen.height / 900f, 1f, 1.6f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.Label(new Rect(24,height-35,720,28),benchmark?"Pinewatch benchmark · "+Mathf.CeilToInt(Mathf.Max(0,65-elapsed))+"s":"WASD walk   E interact   B build   F8 Fern Hollow   F9 photo mode   Esc settings",textStyle);
            if (!walker.MenuOpen || benchmark || RanchSession.PanelOpen) return;
            GUILayout.BeginArea(new Rect(width / 2 - 190, height / 2 - 280, 380, 560), GUI.skin.box);
            GUILayout.Space(14); GUILayout.Label("Take your time", titleStyle); GUILayout.Space(10);
            GUILayout.Label("Mouse sensitivity", textStyle);
            walker.sensitivity = GUILayout.HorizontalSlider(walker.sensitivity, .3f, 4);
            GUILayout.Label("Field of view: " + walker.view.fieldOfView.ToString("F0"), textStyle);
            walker.view.fieldOfView = GUILayout.HorizontalSlider(walker.view.fieldOfView, 60, 100);
            GUILayout.Label("Sound volume", textStyle);
            AudioListener.volume = GUILayout.HorizontalSlider(AudioListener.volume, 0, 1);
            PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
            GUILayout.Space(12);
            if (SkyWeather.Current != null)
            {
                if (GUILayout.Button(SkyWeather.Current.TimeLabel, GUILayout.Height(30))) SkyWeather.Current.NextTime();
                if (GUILayout.Button(SkyWeather.Current.WeatherLabel, GUILayout.Height(30))) SkyWeather.Current.NextWeather();
            }
            if (GUILayout.Button("Graphics: " + (high ? "High" : "Low"), GUILayout.Height(32))) ApplyQuality(!high);
            if (GUILayout.Button("Walk the benchmark route", GUILayout.Height(32))) BeginBenchmark();
            if (GUILayout.Button("Return to the meadow", GUILayout.Height(32))) { walker.Teleport(ValleyShape.Spawn); walker.SetMenu(false); }
            if (GUILayout.Button("Resume", GUILayout.Height(32))) walker.SetMenu(false);
            if (GUILayout.Button("Quit", GUILayout.Height(32))) Quit();
            GUILayout.EndArea();
        }

        [Serializable]
        sealed class BenchmarkResult
        {
            public string route;
            public string revision, unity, utc, operatingSystem, processor, graphics, graphicsAPI, graphicsDriver, quality, weather;
            public float daylight;
            public int processorCount, memoryMB, graphicsMemoryMB, width, height, frames, framesOver50Ms;
            public float meanMs, p95Ms, p99Ms;
            public long unityAllocatedMB, processWorkingSetMB;
        }
    }
}
