using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    public sealed class SkyWeather : MonoBehaviour
    {
        public Material rainMaterial;
        public static SkyWeather Current { get; private set; }
        public static float RainAmount { get; private set; }
        public static float Daylight { get; private set; } = 1;
        public int WeatherMode { get; private set; }
        public int TimeMode { get; private set; }
        public string WeatherLabel => new[]{"Changing weather","Clear skies","Gentle drizzle"}[WeatherMode];
        public string TimeLabel => new[]{"Gentle day/night cycle","Stay in daylight","Stay in evening"}[TimeMode];
        Material sky;
        Material previousSky;
        ParticleSystem rain;
        FirstPersonWalker walker;
        bool repeatable;
        float roofCheck;
        bool sheltered;
        bool preview;
        float previewPhase,previewRain;
        public void SetPreview(float phase,float amount){preview=true;previewPhase=Mathf.Repeat(phase,1);previewRain=Mathf.Clamp01(amount);RainAmount=previewRain;}

        void Start()
        {
            Current=this;walker=FindFirstObjectByType<FirstPersonWalker>();
            WeatherMode=Mathf.Clamp(PlayerPrefs.GetInt("WeatherMode",0),0,2);
            TimeMode=Mathf.Clamp(PlayerPrefs.GetInt("TimeMode",0),0,2);
            var args=Environment.GetCommandLineArgs();repeatable=Array.IndexOf(args,"--benchmark")>=0||Array.IndexOf(args,"--ranch-smoke")>=0;
            if(Array.IndexOf(args,"--benchmark-rain")>=0)SetPreview(.77f,1);
            previousSky=RenderSettings.skybox;sky=new Material(previousSky);RenderSettings.skybox=sky;
            var go=new GameObject("Gentle rain");go.transform.SetParent(transform);rain=go.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main;main.loop=true;main.duration=2;main.startLifetime=.85f;main.startSpeed=16;main.startSize=.018f;main.startColor=new Color(.68f,.79f,.86f,.27f);main.maxParticles=350;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=rain.emission;emission.rateOverTime=0;
            var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(20,20,.1f);
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=3;renderer.velocityScale=.04f;renderer.sharedMaterial=rainMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
            go.transform.rotation=Quaternion.Euler(90,0,0);rain.Play();
        }
        public void NextWeather(){preview=false;WeatherMode=(WeatherMode+1)%3;PlayerPrefs.SetInt("WeatherMode",WeatherMode);PlayerPrefs.Save();}
        public void NextTime(){preview=false;TimeMode=(TimeMode+1)%3;PlayerPrefs.SetInt("TimeMode",TimeMode);PlayerPrefs.Save();}
        public static float CycleDaylight(float phase) => Mathf.Clamp01((Mathf.Sin((phase-.25f)*Mathf.PI*2)+.18f)/.95f);
        void Update()
        {
            if(sky==null)return;
            double seconds=(DateTime.UtcNow-new DateTime(2026,1,1)).TotalSeconds;
            float phase=preview?previewPhase:repeatable?.42f:TimeMode==1?.42f:TimeMode==2?.73f:(float)(seconds%1920)/1920;
            Daylight=CycleDaylight(phase);
            float weather=preview?previewRain:repeatable||WeatherMode==1?0:WeatherMode==2?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.8f,Mathf.Sin((float)(seconds%960)/960*Mathf.PI*2)));
            RainAmount=Mathf.MoveTowards(RainAmount,weather,Time.deltaTime*.08f);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=Color.Lerp(new Color(.38f,.45f,.58f),new Color(.82f,.87f,.94f),Daylight);
            RenderSettings.ambientEquatorColor=Color.Lerp(new Color(.28f,.34f,.40f),new Color(.65f,.69f,.57f),Daylight);
            RenderSettings.ambientGroundColor=Color.Lerp(new Color(.20f,.24f,.30f),new Color(.42f,.46f,.32f),Daylight);
            // Runtime sky changes need an explicit diffuse probe; the generated scene has no baked GI.
            var probe=new SphericalHarmonicsL2();
            probe.AddAmbientLight(Color.Lerp(new Color(.15f,.19f,.28f),new Color(.43f,.47f,.51f),Daylight));
            probe.AddDirectionalLight(Vector3.up,new Color(.22f,.30f,.42f),.35f);
            RenderSettings.ambientProbe=probe;
            RenderSettings.fogColor=Color.Lerp(new Color(.24f,.33f,.45f),new Color(.65f,.76f,.8f),Daylight);
            RenderSettings.fogDensity=Mathf.Lerp(.0011f,.0018f,RainAmount);
            var sun=RenderSettings.sun;
            if(sun!=null)
            {
                float elevation=12+Daylight*24;
                sun.transform.rotation=Quaternion.Euler(elevation,-32,0);
                sun.intensity=Mathf.Lerp(.28f,1.9f,Daylight)*(1-RainAmount*.3f);
                sun.shadowStrength=.82f;
                sun.color=Color.Lerp(new Color(.72f,.80f,1),Color.Lerp(new Color(1,.75f,.48f),new Color(1,.89f,.70f),Daylight),Mathf.Clamp01(Daylight*3));
                sky.SetVector("_SunDirection",-sun.transform.forward);
            }
            sky.SetFloat("_Daylight",Daylight);sky.SetFloat("_CloudCover",RainAmount);
            Shader.SetGlobalFloat("_WeatherWetness",RainAmount);
            if(walker!=null)
            {
                rain.transform.position=walker.view.transform.position+Vector3.up*9;
                roofCheck-=Time.deltaTime;if(roofCheck<=0){roofCheck=.25f;sheltered=Physics.Raycast(walker.view.transform.position,Vector3.up,15,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);}
                var emission=rain.emission;emission.rateOverTime=sheltered?0:RainAmount*240;
            }
        }
        void OnDestroy()
        {
            if(Current==this){Current=null;RainAmount=0;Daylight=1;Shader.SetGlobalFloat("_WeatherWetness",0);}
            if(sky!=null){if(RenderSettings.skybox==sky)RenderSettings.skybox=previousSky;Destroy(sky);}
        }
    }
}
