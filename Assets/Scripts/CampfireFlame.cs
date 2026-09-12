using UnityEngine;
using UnityEngine.Rendering;
namespace ExplorersByNature
{
    public static class CampfireFlame
    {
        public static void Add(Transform parent)
        {
            Material material=Resources.Load<Material>("ArtSupport/Fire");
            for(int i=0;i<2;i++)
            {
                var flame=GameObject.CreatePrimitive(PrimitiveType.Quad);flame.name="Living flame";flame.transform.SetParent(parent,false);
                flame.transform.localPosition=Vector3.up*.61f;flame.transform.localScale=new Vector3(.72f,.9f,1);flame.transform.localRotation=Quaternion.Euler(0,i*90,0);
                var collider=flame.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);
                var renderer=flame.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
    }
}
