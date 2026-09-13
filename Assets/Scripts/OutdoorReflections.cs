using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ExplorersByNature
{
    // Live specular environment for runtime-built scenery and changing skies.
    // This is reflection capture, not diffuse global illumination.
    public sealed class OutdoorReflections : MonoBehaviour
    {
        ReflectionProbe probe;
        FirstPersonWalker walker;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;EnsurePresent();
        }
        static void OnSceneLoaded(Scene scene,LoadSceneMode mode){EnsurePresent();}
        static void EnsurePresent(){if(FindFirstObjectByType<OutdoorReflections>()==null)new GameObject("Live outdoor reflections").AddComponent<OutdoorReflections>();}
        void Start()
        {
            walker=FindFirstObjectByType<FirstPersonWalker>();
            probe=gameObject.AddComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Realtime;
            probe.refreshMode=ReflectionProbeRefreshMode.EveryFrame;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution=256;probe.hdr=true;probe.size=Vector3.one*1200;
            probe.nearClipPlane=.3f;probe.farClipPlane=1600;
            probe.clearFlags=ReflectionProbeClearFlags.Skybox;
            probe.cullingMask=~((1<<4)|(1<<29)|(1<<30)|(1<<31));probe.boxProjection=false;probe.intensity=1;
        }
        void LateUpdate()
        {
            if(probe==null || walker==null)return;
            probe.enabled=QualitySettings.GetQualityLevel()==1;
            if(Vector3.Distance(transform.position,walker.transform.position)>20)
                transform.position=walker.transform.position+Vector3.up*3;
        }
    }
}
