using System.Collections.Generic;
using ExplorersByNature.Shared;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class FrontierTarget : MonoBehaviour {public string kind;public int id;}
    public sealed class FrontierWorld : MonoBehaviour
    {
        RanchSession ranch;readonly List<FrontierTarget> sites=new List<FrontierTarget>();int revision=-1;long lastSecond=-1;
        void Start()
        {
            ranch=GetComponent<RanchSession>();
            Spawn(FrontierSites.Wood,"wood");Spawn(FrontierSites.Stone,"stone");Spawn(FrontierSites.Deer,"hunt");
        }
        void Spawn(FrontierSite[] positions,string kind)
        {
            foreach(var site in positions)
            {
                var root=new GameObject(kind=="hunt"?"Meadow hunting deer":kind=="wood"?"Gathering timber":"Gathering stone");root.transform.SetParent(transform,false);root.transform.position=ValleyShape.Ground(site.x,site.z);
                var target=root.AddComponent<FrontierTarget>();target.kind=kind;target.id=site.id;sites.Add(target);
                GameObject model=kind=="hunt"?ModelArt.Instantiate("Wildlife/Deer",root.transform,false):ReferenceGroundArt.Instantiate(kind=="wood"?"Stump":"RockA",root.transform);
                if(model==null && kind=="wood")model=ReferenceGroundArt.Instantiate("StumpA",root.transform);
                if(kind=="hunt")
                {
                    root.transform.rotation=Quaternion.Euler(0,site.id*83,0);
                    var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.9f,0);box.size=new Vector3(.65f,1.45f,1.5f);
                }
                else
                {
                    if(model!=null)model.transform.localScale=Vector3.one*.7f;
                    var box=root.AddComponent<BoxCollider>();box.center=Vector3.up*.45f;box.size=new Vector3(1.1f,.9f,1.1f);
                }
            }
        }
        void Update()
        {
            var state=ranch.Connection?.State;if(state==null)return;
            long now=ranch.Connection.Latest?.utc??0;
            if(revision==state.revision && now==lastSecond)return;revision=state.revision;lastSecond=now;
            foreach(var target in sites)
            {
                bool active=FrontierSites.Ready(state,target.kind,target.id,now);
                target.gameObject.SetActive(active);
            }
        }
    }
}
