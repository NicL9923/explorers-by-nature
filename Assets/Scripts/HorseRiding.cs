using System.Linq;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class HorseRiding : MonoBehaviour
    {
        public static HorseRiding Current {get;private set;}
        public bool Mounted {get;private set;}
        public const float RiderHeight=.71f;
        public GameObject Horse {get;private set;}
        RanchComfort comfort;FirstPersonWalker walker;CharacterController motor;PlayerAvatar rider;float gravity;string notice="";
        void Start()
        {
            Current=this;walker=FindFirstObjectByType<FirstPersonWalker>();comfort=FindFirstObjectByType<RanchComfort>();
            Horse=new GameObject("Your ranch horse");Horse.transform.position=ValleyShape.Ground(-82,-218,.1f);
            var model=ModelArt.Instantiate("Horse/Horse",Horse.transform,false);
            if(model!=null)model.AddComponent<HorseMotion>();
            motor=Horse.AddComponent<CharacterController>();motor.center=new Vector3(0,.85f,0);motor.height=1.7f;motor.radius=.4f;motor.stepOffset=.4f;motor.slopeLimit=42;
            var seat=new GameObject("Rider");seat.transform.SetParent(Horse.transform,false);seat.transform.localPosition=Vector3.up*RiderHeight;
            rider=seat.AddComponent<PlayerAvatar>();seat.SetActive(false);
        }
        void Update()
        {
            if(walker==null || walker.Automated || walker.MenuOpen || PlayerWardrobe.IsOpen)return;
            if(Input.GetKeyDown(KeyCode.H)) {if(Mounted)Dismount();else Mount();}
            if(Mounted)RefreshRider();
        }
        public bool Mount()
        {
            if(Mounted)return true;
            if(comfort!=null && comfort.Seated){notice="Stand up before mounting.";return false;}
            if(Horse==null || walker==null || Vector3.Distance(walker.transform.position,Horse.transform.position)>4)return false;
            var session=FindFirstObjectByType<RanchSession>();if(session!=null && session.Building)return false;
            Mounted=true;gravity=0;walker.GetComponent<CharacterController>().enabled=false;
            rider.gameObject.SetActive(true);RefreshRider();
            walker.transform.position=Horse.transform.position+Vector3.up*RiderHeight;notice="";return true;
        }
        void RefreshRider()
        {
            rider.SetModel(PlayerWardrobe.SelectedId);rider.SetMounted(true,false);
            if(rider.Model!=null)foreach(var part in rider.Model.GetComponentsInChildren<Transform>(true))
                if(part.name=="Head")part.gameObject.SetActive(false);
        }
        public bool Dismount()
        {
            if(!Mounted)return true;
            var controller=walker.GetComponent<CharacterController>();
            float radius=controller.radius;float halfSegment=Mathf.Max(0,controller.height*.5f-radius);
            for(int i=0;i<8;i++)
            {
                Vector3 offset=Quaternion.Euler(0,i*45,0)*Horse.transform.right*1.7f;
                Vector3 p=ValleyShape.Ground(Horse.transform.position.x+offset.x,Horse.transform.position.z+offset.z,.15f);
                Vector3 center=p+walker.transform.rotation*controller.center;
                if(RiverDynamics.DepthAt(p.x,p.z)>.3f || Physics.CheckCapsule(center-Vector3.up*halfSegment,center+Vector3.up*halfSegment,radius,~0,QueryTriggerInteraction.Ignore))continue;
                Release();walker.Teleport(p);return true;
            }
            notice="Move to open ground to dismount.";return false;
        }
        public void Release()
        {
            Mounted=false;if(rider!=null)rider.gameObject.SetActive(false);
            if(walker!=null)walker.GetComponent<CharacterController>().enabled=true;
        }
        public void Move(Vector2 input,bool sprint,float dt)
        {
            if(!Mounted)return;
            dt=Mathf.Clamp(dt,0,.05f);input=Vector2.ClampMagnitude(input,1);
            Horse.transform.rotation=Quaternion.Slerp(Horse.transform.rotation,Quaternion.Euler(0,walker.transform.eulerAngles.y,0),1-Mathf.Exp(-dt*4));
            Vector3 motion=(Horse.transform.forward*input.y+Horse.transform.right*input.x*.45f)*(sprint?10:5.5f);
            Vector3 next=Horse.transform.position+motion*dt;
            if(RiverDynamics.DepthAt(next.x,next.z)>.35f || Mathf.Abs(next.x)>439 || Mathf.Abs(next.z)>439)motion=Vector3.zero;
            gravity=motor.isGrounded?-2:Mathf.Max(-30,gravity-22*dt);motion.y=gravity;motor.Move(motion*dt);
            walker.transform.position=Horse.transform.position+Vector3.up*RiderHeight;
        }
        void OnGUI()
        {
            if(walker==null || walker.Automated || walker.MenuOpen || PlayerWardrobe.IsOpen || ReferenceGrove.PhotoMode)return;
            if(Mounted || Vector3.Distance(walker.transform.position,Horse.transform.position)<4)
            {
                GUI.matrix=Matrix4x4.identity;
                GUI.Box(new Rect(Screen.width/2-230,Screen.height-100,460,35),notice!=""?notice:Mounted?"H dismount · WASD ride · Shift canter":"H saddle up · Your ranch horse");
            }
        }
        void OnDestroy(){Release();if(Horse!=null)Destroy(Horse);if(Current==this)Current=null;}
    }

    public sealed class HorseMotion : MonoBehaviour
    {
        Transform[] joints;Quaternion[] rest;Vector3 previous;float phase,blend;
        void Start()
        {
            string[] names={"FrontLegL","FrontLegR","HindLegL","HindLegR","FrontKneeL","FrontKneeR","HindKneeL","HindKneeR","Neck","Tail"};
            var all=GetComponentsInChildren<Transform>();joints=names.Select(n=>all.FirstOrDefault(t=>t.name==n)).ToArray();rest=joints.Select(t=>t==null?Quaternion.identity:t.localRotation).ToArray();previous=transform.position;
        }
        void LateUpdate()
        {
            float speed=Vector3.Distance(transform.position,previous)/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;
            blend=Mathf.Lerp(blend,Mathf.Clamp01(speed/3),1-Mathf.Exp(-Time.deltaTime*8));phase+=Time.deltaTime*Mathf.Lerp(2,10,Mathf.Clamp01(speed/9));
            for(int i=0;i<joints.Length;i++)if(joints[i]!=null)
            {
                float wave=Mathf.Sin(phase+(i%4==0||i%4==3?0:Mathf.PI));
                float angle=i<4?wave*25*blend:i<8?Mathf.Max(0,-wave)*(i<6?40:-40)*blend:Mathf.Sin(Time.time*1.7f)*(i==8?2:8);
                Vector3 axis=joints[i].parent.InverseTransformDirection(i==9?transform.parent.forward:transform.parent.right);
                joints[i].localRotation=Quaternion.AngleAxis(angle,axis)*rest[i];
            }
        }
    }
}
