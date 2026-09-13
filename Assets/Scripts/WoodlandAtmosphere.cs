using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace ExplorersByNature
{
    // Local, depth-clipped scattering volumes. High quality only; collision is untouched.
    public sealed class WoodlandAtmosphere : MonoBehaviour
    {
        public Material material;
        readonly List<Material> owned=new List<Material>();
        readonly List<Renderer> volumes=new List<Renderer>();
        void Start()
        {
            foreach(float z in new[]{-165f,-65f,40f})
            {
                float x=ValleyShape.TrailX(z)-18;float ground=ValleyShape.Height(x,z)-3;
                Vector3 center=new Vector3(x,ground+16,z),size=new Vector3(100,32,90);
                var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name="Sunlight through woodland air";box.layer=29;box.transform.SetParent(transform,false);box.transform.position=center;box.transform.localScale=size;
                var collider=box.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                var copy=new Material(material);copy.SetVector("_BoxMin",center-size*.5f);copy.SetVector("_BoxMax",center+size*.5f);owned.Add(copy);
                var renderer=box.GetComponent<Renderer>();renderer.sharedMaterial=copy;renderer.shadowCastingMode=ShadowCastingMode.Off;volumes.Add(renderer);
            }
            SetQuality(QualitySettings.GetQualityLevel()==1);
        }
        public void SetQuality(bool high){foreach(var volume in volumes)volume.enabled=high;}
        void OnDestroy(){foreach(var m in owned)Destroy(m);}
    }
}
