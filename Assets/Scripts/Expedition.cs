using ExplorersByNature.Shared;
using UnityEngine;

namespace ExplorersByNature
{
    // Shared progress belongs to the ranch; everyone can help finish the same outing.
    public sealed class Expedition : MonoBehaviour
    {
        public static string Journal(int stage) => stage==0 ? "Pack a picnic at the basket beside the arrival trail. Follow the marked trail north to Pinewatch Overlook." : stage==1 ? "Picnic packed! Follow the trail north to the overlook. Take your time: food never spoils and the animals are happy at home." : stage==2 ? "A picnic above the valley. You found alpine flower seeds beside the overlook. Return to the arrival basket to add them to your ranch garden." : "Pinewatch picnic complete. Alpine flowers are unlocked in the building palette for everyone. The view is always worth another visit.";
        public string Prompt(Vector3 position,int stage)
        {
            Vector3 goal=stage==1?ValleyShape.Overlook:ValleyShape.Ground(Ranch.HomeX,Ranch.HomeZ);
            if(Vector3.Distance(position,goal)>7)return "";
            return stage==0?"E · Pack a picnic":stage==1?"E · Enjoy the picnic and discover alpine seeds":stage==2?"E · Bring alpine flowers home":"";
        }
        public static string Action(int stage)=>stage==0?"pack":stage==1?"picnic":"claim";
        Material wicker,cloth;
        void Start()
        {
            wicker=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.57f,.34f,.14f)};
            cloth=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.67f,.24f,.16f)};
            Basket(ValleyShape.Ground(Ranch.HomeX,Ranch.HomeZ),"Arrival picnic basket",false);
            Basket(ValleyShape.Overlook,"Pinewatch picnic",true);
        }
        void OnDestroy(){if(wicker!=null)Destroy(wicker);if(cloth!=null)Destroy(cloth);}
        void Basket(Vector3 position,string name,bool blanket)
        {
            var root=new GameObject(name);root.transform.SetParent(transform,false);root.transform.position=position;
            if(blanket)for(int x=0;x<6;x++)for(int z=0;z<5;z++)RanchVisuals.Part(root.transform,"Picnic quilt",PrimitiveType.Cube,new Vector3((x-2.5f)*.3f,.025f,(z-2)*.3f),new Vector3(.3f,.025f,.3f),(x+z)%2==0?wicker:cloth,false);
            RanchVisuals.Part(root.transform,"Woven basket",PrimitiveType.Cube,new Vector3(.55f,.24f,0),new Vector3(.5f,.4f,.38f),wicker,false);
            for(int i=0;i<5;i++)RanchVisuals.Part(root.transform,"Basket weave",PrimitiveType.Cube,new Vector3(.55f,.1f+i*.075f,0),new Vector3(.52f,.02f,.4f),cloth,false);
            foreach(int sign in new[]{-1,1})RanchVisuals.Part(root.transform,"Handle side",PrimitiveType.Cube,new Vector3(.55f+sign*.2f,.53f,0),new Vector3(.035f,.22f,.04f),wicker,false);
            RanchVisuals.Part(root.transform,"Handle",PrimitiveType.Cube,new Vector3(.55f,.64f,0),new Vector3(.44f,.035f,.04f),wicker,false);
        }
    }
}
