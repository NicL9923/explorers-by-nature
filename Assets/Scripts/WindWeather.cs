using UnityEngine;

namespace ExplorersByNature
{
    // A shared moving air field for vegetation, fur and surface water. Visual only.
    [DefaultExecutionOrder(-150)]
    public sealed class WindWeather : MonoBehaviour
    {
        public static WindWeather Current { get; private set; }
        public static Vector3 CurrentVelocity { get; private set; }
        public static float SimulationTime => Time.timeSinceLevelLoad;
        public int Mode { get; private set; } = 1;
        public string ModeLabel => new[] { "Wind: calm", "Wind: breeze", "Wind: gusty" }[Mode];
        FirstPersonWalker walker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindFirstObjectByType<WindWeather>() == null)
                new GameObject("Valley wind").AddComponent<WindWeather>();
        }

        void Awake()
        {
            Current = this;
            Mode = Mathf.Clamp(PlayerPrefs.GetInt("WindMode", 1), 0, 2);
            walker = FindFirstObjectByType<FirstPersonWalker>();
        }

        public void SetMode(int mode)
        {
            Mode = Mathf.Clamp(mode, 0, 2);
            PlayerPrefs.SetInt("WindMode", Mode);
            PlayerPrefs.Save();
        }

        public void NextMode() => SetMode((Mode + 1) % 3);

        public static Vector3 VelocityAt(Vector3 position, float time)
        {
            int mode = Current == null ? 1 : Current.Mode;
            return SampleVelocity(position, time, SkyWeather.RainAmount, mode == 0 ? 0 : mode == 2 ? 1.6f : 1);
        }

        public static Vector3 SampleVelocity(Vector3 position, float time, float rain, float strength)
        {
            // Gusts travel through the valley instead of choosing a new force every frame.
            float gust = Mathf.PerlinNoise((position.x - time * 2.4f) * .016f + 97,
                (position.z - time * 1.8f) * .016f + 43);
            gust = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .78f, gust));
            float angle = .62f + Mathf.Sin(time * .035f) * .18f;
            float speed = (1.4f + gust * 3.2f + Mathf.Clamp01(rain) * 1.8f) * Mathf.Max(0, strength);
            return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * speed;
        }

        void Update()
        {
            Vector3 position = walker == null ? ValleyShape.Spawn : walker.transform.position;
            CurrentVelocity = VelocityAt(position, SimulationTime);
            Shader.SetGlobalVector("_NatureWind", new Vector4(CurrentVelocity.x, CurrentVelocity.y,
                CurrentVelocity.z, CurrentVelocity.magnitude / 4.6f));
            Shader.SetGlobalFloat("_NatureWindTime", SimulationTime);
        }

        void OnDestroy()
        {
            if (Current != this) return;
            Current = null; CurrentVelocity = Vector3.zero;
            Shader.SetGlobalVector("_NatureWind", Vector4.zero);
            Shader.SetGlobalFloat("_NatureWindTime", 0);
        }
    }
}
