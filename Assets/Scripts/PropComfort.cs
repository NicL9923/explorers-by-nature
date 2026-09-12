using System.Collections.Generic;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class PropComfort : MonoBehaviour
    {
        public string Kind;
        internal static readonly List<PropComfort> Lamps=new List<PropComfort>();
        void OnEnable(){if(Kind=="lantern" || Kind=="campfire")Lamps.Add(this);}
        public void Configure(string kind){Kind=kind;if((kind=="lantern" || kind=="campfire")&&!Lamps.Contains(this))Lamps.Add(this);}
        void OnDisable(){Lamps.Remove(this);}
    }

    // Four pooled lights regardless of the number of built lanterns and hearths.
    public sealed class RanchComfort : MonoBehaviour
    {
        readonly Light[] lights=new Light[4];
        readonly PropComfort[] closest=new PropComfort[4];
        readonly float[] distances=new float[4];
        FirstPersonWalker walker;
        PropComfort seat;
        Vector3 returnPosition,eyePosition;
        float pitch,nextLights;
        int sitFrame,standFrame=-1;
        public bool JustStood=>standFrame==Time.frameCount;
        public bool Seated {get;private set;}
        void Start()
        {
            walker=FindFirstObjectByType<FirstPersonWalker>();
            for(int i=0;i<lights.Length;i++)
            {
                var go=new GameObject("Nearby warm light "+i);go.transform.SetParent(transform);
                lights[i]=go.AddComponent<Light>();lights[i].type=LightType.Point;lights[i].shadows=LightShadows.None;lights[i].color=new Color(1,.61f,.27f);lights[i].range=7;lights[i].enabled=false;
            }
        }
        public void Sit(PropComfort bench)
        {
            if(walker==null || bench==null || bench.Kind!="bench" || Seated)return;
            returnPosition=walker.transform.position;eyePosition=walker.view.transform.localPosition;
            seat=bench;Seated=true;sitFrame=Time.frameCount;walker.enabled=false;
            walker.Teleport(bench.transform.TransformPoint(new Vector3(0,.6f,-.05f)));
            walker.view.transform.localPosition=new Vector3(0,.8f,0);
            pitch=walker.view.transform.localEulerAngles.x;if(pitch>180)pitch-=360;
        }
        public void Stand()
        {
            if(!Seated)return;
            Seated=false;seat=null;standFrame=Time.frameCount;
            if(walker!=null){walker.view.transform.localPosition=eyePosition;walker.Teleport(returnPosition);walker.SyncLookPitch();walker.enabled=true;}
        }
        void Update()
        {
            if(walker==null)return;
            if(Seated)
            {
                if(seat==null || (Time.frameCount>sitFrame && (Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.Tab)||Input.GetKeyDown(KeyCode.Home)||Mathf.Abs(Input.GetAxisRaw("Horizontal"))+Mathf.Abs(Input.GetAxisRaw("Vertical"))>.1f)))
                {Stand();}
                else if(!walker.MenuOpen)
                {
                    walker.transform.Rotate(0,Input.GetAxisRaw("Mouse X")*walker.sensitivity,0);
                    pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*walker.sensitivity,-80,80);
                    walker.view.transform.localRotation=Quaternion.Euler(pitch,0,0);
                }
            }
            if(Time.unscaledTime<nextLights)return;nextLights=Time.unscaledTime+.5f;
            for(int i=0;i<4;i++){closest[i]=null;distances[i]=18*18;}
            foreach(var prop in PropComfort.Lamps)
            {
                if(prop==null)continue;float d=(prop.transform.position-walker.transform.position).sqrMagnitude;
                for(int i=0;i<4;i++)if(d<distances[i]){for(int j=3;j>i;j--){distances[j]=distances[j-1];closest[j]=closest[j-1];}distances[i]=d;closest[i]=prop;break;}
            }
            float evening=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,.85f,SkyWeather.Daylight));
            for(int i=0;i<4;i++)
            {
                lights[i].enabled=closest[i]!=null && evening>.01f;
                if(closest[i]==null)continue;
                lights[i].transform.position=closest[i].transform.TransformPoint(closest[i].Kind=="lantern"?new Vector3(.34f,1.35f,0):new Vector3(0,.7f,0));
                lights[i].intensity=evening*(closest[i].Kind=="campfire"?2.5f:2f);
            }
        }
        void OnDisable(){Stand();foreach(var light in lights)if(light!=null)light.enabled=false;}
    }
}
