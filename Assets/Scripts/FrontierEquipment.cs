using System;
using System.Collections;
using System.Linq;
using ExplorersByNature.Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    public sealed class FrontierEquipment : MonoBehaviour
    {
        public int Tool {get;private set;}
        public GameObject HeldModel=>held;
        public bool Loaded {get;private set;}=true;
        public bool Reloading {get;private set;}
        public float ReloadProgress {get;private set;}
        public string Status {get;private set;}="";
        public const float ReloadSeconds=8;
        readonly string[] names={"Empty hands","Axe","Pickaxe","Muzzleloader"};
        RanchComfort comfort;RanchSession ranch;FirstPersonWalker walker;GameObject rig,held,leftHand,rightHand;Transform rod,hammer;
        Vector3 rodRest,rodRestInTool;Quaternion hammerRest;ParticleSystem smoke;Light flash;AudioSource audio;AudioClip shot,wood,stone,click;
        Material handSkin;string skinId;
        static readonly Color[] SkinColors={new Color(.61f,.38f,.25f),new Color(.33f,.16f,.09f),new Color(.78f,.52f,.36f),new Color(.48f,.28f,.17f)};
        static readonly Vector3 GunRightGrip=new Vector3(.046f,0,-.10f),GunLeftGrip=new Vector3(-.046f,-.007f,.22f);
        static readonly Quaternion RightGripRotation=Quaternion.Euler(0,0,-90),LeftGripRotation=Quaternion.Euler(0,0,90);
        float swing=-1,recoil=20,nextUse;FrontierTarget target;bool swingDelivered;
        void Start()
        {
            comfort=FindFirstObjectByType<RanchComfort>();ranch=GetComponent<RanchSession>();walker=FindFirstObjectByType<FirstPersonWalker>();
            rig=new GameObject("First-person equipment");rig.transform.SetParent(walker.view.transform,false);
            audio=rig.AddComponent<AudioSource>();audio.spatialBlend=0;audio.volume=.45f;
            shot=Sound("Muzzleloader report",.55f,61,1);wood=Sound("Wood strike",.14f,155,.35f);stone=Sound("Stone strike",.18f,630,.25f);click=Sound("Lock and ramrod",.06f,1100,.08f);
            Equip(0);
        }
        public void Equip(int tool)
        {
            if(Reloading)return;Tool=Mathf.Clamp(tool,0,3);swing=-1;
            if(held!=null){held.SetActive(false);Destroy(held);}ReleaseHands();
            if(Tool==0)return;
            held=Presentation("Tools/"+names[Tool]);
            if(held==null)return;
            leftHand=Presentation("Tools/HandL");rightHand=Presentation("Tools/HandR");CreateHandSkin();
            foreach(var r in rig.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.gameObject.layer=30;}
            rod=held.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Ramrod");if(rod!=null){rodRest=rod.localPosition;rodRestInTool=held.transform.InverseTransformPoint(rod.position);}
            hammer=held.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Hammer");if(hammer!=null)hammerRest=hammer.localRotation;
            if(Tool==3 && smoke==null)CreateSmoke();Pose();
        }
        GameObject Presentation(string path)
        {
            // Animate a clean parent; imported FBX roots retain their axis conversion.
            var root=new GameObject(path.Substring(path.LastIndexOf('/')+1)+" presentation");root.transform.SetParent(rig.transform,false);
            if(ModelArt.Instantiate(path,root.transform,false)!=null)return root;
            Destroy(root);return null;
        }
        bool CanAct()=>walker!=null && !walker.MenuOpen && !(comfort?.Seated??false) && !walker.Automated && !ranch.Building && !PlayerWardrobe.IsOpen && !(HorseRiding.Current?.Mounted??false);
        void Update()
        {
            if(rig==null)return;
            RefreshHandSkin();
            rig.SetActive(!walker.MenuOpen && !ranch.Building && !(HorseRiding.Current?.Mounted??false) && !PlayerWardrobe.IsOpen);
            if(CanAct())
            {
                if(Input.GetKeyDown(KeyCode.Q))Equip((Tool+1)%4);
                if(Input.GetKeyDown(KeyCode.R) && Tool==3)BeginReload();
                if(Input.GetMouseButtonDown(0))Use();
            }
            recoil+=Time.deltaTime;if(flash!=null)flash.enabled=recoil<.06f;
            if(swing>=0)
            {
                swing+=Time.deltaTime;
                if(!swingDelivered && swing>=.38f)
                {
                    swingDelivered=true;
                    if(target!=null && target.gameObject.activeInHierarchy && Vector3.Distance(walker.transform.position,target.transform.position)<4)
                    {ranch.Connection?.Send(new Request{action="gather",kind=target.kind,id=target.id});audio.PlayOneShot(Tool==1?wood:stone);}
                }
                if(swing>.85f)swing=-1;
            }
            if(held!=null && !Reloading)Pose();
        }
        public bool Use()
        {
            if(comfort!=null && comfort.Seated){Status="Stand up to use tools.";return false;}
            if(Tool==0 || held==null || Reloading || Time.time<nextUse || ranch.Connection?.Connected!=true)return false;
            if(HorseRiding.Current?.Mounted??false){Status="Dismount to use tools.";return false;}
            if(Tool==3)
            {
                if(!Loaded){Status="R to reload";return false;}
                int id=-1;Vector3 aim=walker.view.transform.forward;
                if(Physics.Raycast(walker.view.transform.position,aim,out var hit,60))
                {var animal=hit.collider.GetComponentInParent<FrontierTarget>();if(animal!=null && animal.kind=="hunt" && ranch.Connection.State.huntingEnabled)id=animal.id;}
                ranch.Connection.SendShot(id,aim,walker.transform.position,walker.transform.eulerAngles.y,PlayerWardrobe.SelectedId,HorseRiding.Current!=null && HorseRiding.Current.Mounted);
                Loaded=false;recoil=0;nextUse=Time.time+.5f;Status="R to reload";audio.PlayOneShot(shot);
                if(smoke!=null){smoke.transform.position=held.transform.TransformPoint(new Vector3(0,.087f,.878f));smoke.transform.rotation=held.transform.rotation;smoke.Emit(18);}
                if(flash!=null)flash.transform.position=held.transform.TransformPoint(new Vector3(0,.087f,.878f));
                return true;
            }
            if(!Physics.Raycast(walker.view.transform.position,walker.view.transform.forward,out var resourceHit,4)){Status="Look at gathering timber or stone.";return false;}
            target=resourceHit.collider.GetComponentInParent<FrontierTarget>();
            if(target==null || target.kind!=(Tool==1?"wood":"stone")){Status=Tool==1?"Use the axe on gathering timber.":"Use the pickaxe on gathering stone.";return false;}
            swing=0;swingDelivered=false;nextUse=Time.time+2.1f;Status=Tool==1?"Splitting timber":"Breaking stone";return true;
        }
        public bool BeginReload()
        {
            if(Tool!=3 || Loaded || Reloading || held==null)return false;StartCoroutine(Reload());return true;
        }
        IEnumerator Reload()
        {
            Reloading=true;ReloadProgress=0;int lastCue=-1;
            while(ReloadProgress<1)
            {
                ReloadProgress=Mathf.Min(1,ReloadProgress+Time.deltaTime/ReloadSeconds);
                float t=ReloadProgress*ReloadSeconds;Status="Reloading";
                float tilt=Smooth(0,.7f,t)*(1-Smooth(7.25f,8,t));
                held.transform.localPosition=Vector3.Lerp(new Vector3(.21f,-.27f,.32f),new Vector3(.20f,-.46f,.36f),tilt);
                held.transform.localRotation=Quaternion.Euler(-35*tilt,-8,-12*tilt);
                HoldHands();
                // The rod must clear the muzzle before moving from its storage channel
                // to the bore, 9.4 cm higher. Coordinates are metres in the tool frame.
                float pull=0,alignment=0;
                if(t<3.35f)pull=.80f*Smooth(2.7f,3.35f,t);
                else if(t<3.65f){pull=.80f;alignment=Smooth(3.35f,3.65f,t);}
                else if(t<4.2f){pull=Mathf.Lerp(.80f,.18f,Smooth(3.65f,4.2f,t));alignment=1;}
                else if(t<5.6f){float stroke=Mathf.Sin((t-4.2f)/1.4f*Mathf.PI*3);pull=.18f+.16f*stroke*stroke;alignment=1;}
                else if(t<6.2f){pull=Mathf.Lerp(.18f,.80f,Smooth(5.6f,6.2f,t));alignment=1;}
                else if(t<6.55f){pull=.80f;alignment=1-Smooth(6.2f,6.55f,t);}
                else pull=.80f*(1-Smooth(6.55f,7.25f,t));
                Vector3 rodOffset=new Vector3(0,.094f*alignment,pull);
                if(rod!=null)rod.position=held.transform.TransformPoint(rodRestInTool+rodOffset);
                if(rightHand!=null)
                {
                    // Grip the rod near its exposed brass end and follow that same
                    // authored anchor through withdrawal, tamping and reinsertion.
                    Vector3 rodGrip=(rod!=null?held.transform.InverseTransformPoint(rod.position):new Vector3(0,-.007f,.49f))+new Vector3(.028f,0,.22f);
                    Vector3 pouch=new Vector3(.15f,-.24f,-.06f),muzzle=new Vector3(.03f,.087f,.77f);
                    Vector3 wrist=t<.7f?Vector3.Lerp(GunRightGrip,pouch,Smooth(0,.7f,t)):
                        t<1.35f?pouch:t<2f?Vector3.Lerp(pouch,muzzle,Smooth(1.35f,2,t)):
                        t<2.7f?Vector3.Lerp(muzzle,rodGrip,Smooth(2,2.7f,t)):
                        t<7.25f?rodGrip:Vector3.Lerp(rodGrip,GunRightGrip,Smooth(7.25f,8,t));
                    PlaceHand(rightHand,wrist,RightGripRotation);
                }
                if(hammer!=null)
                {
                    Vector3 axis=hammer.parent.InverseTransformDirection(held.transform.right);
                    hammer.localRotation=Quaternion.AngleAxis(t>6.7f?-35:0,axis)*hammerRest;
                }
                int cue=(int)(t*2);if(cue!=lastCue && t>2.8f && t<7.2f){audio.PlayOneShot(click);lastCue=cue;}
                yield return null;
            }
            if(rod!=null)rod.localPosition=rodRest;if(hammer!=null)hammer.localRotation=hammerRest;
            Loaded=true;Reloading=false;Status="Ready";Pose();
        }
        void Pose()
        {
            bool aim=Tool==3 && Input.GetMouseButton(1) && CanAct();
            Vector3 p=Tool==3?(aim?new Vector3(0,-.10f,.3f):new Vector3(.21f,-.27f,.32f)):new Vector3(.3f,-.34f,.48f);
            float kick=Mathf.Exp(-recoil*16);p.z-=kick*.1f;
            held.transform.localPosition=p;
            float arc=swing<0?0:Mathf.Sin(Mathf.Clamp01(swing/.7f)*Mathf.PI)*-85;
            held.transform.localRotation=Tool==3?Quaternion.Euler(-kick*8,aim?0:-8,0):Quaternion.Euler(12+arc,0,-18);
            HoldHands();
        }
        static float Smooth(float from,float to,float time)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,time));
        void HoldHands()
        {
            if(leftHand!=null)leftHand.SetActive(Tool==3);
            if(Tool==3){PlaceHand(leftHand,GunLeftGrip,LeftGripRotation);PlaceHand(rightHand,GunRightGrip,RightGripRotation);}
            else PlaceHand(rightHand,new Vector3(.052f,-.07f,.02f),Quaternion.LookRotation(Vector3.up,Vector3.right));
        }
        void PlaceHand(GameObject hand,Vector3 position,Quaternion rotation)
        {
            if(hand==null || held==null)return;
            hand.transform.SetPositionAndRotation(held.transform.TransformPoint(position),held.transform.rotation*rotation);
        }
        void CreateHandSkin()
        {
            foreach(var hand in new[]{leftHand,rightHand})
            {
                if(hand==null)continue;
                foreach(var renderer in hand.GetComponentsInChildren<Renderer>(true))
                {
                    var slots=renderer.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)if(slots[i]!=null && slots[i].name.StartsWith("FrontierSkin",StringComparison.Ordinal))
                    {
                        if(handSkin==null)handSkin=new Material(slots[i]){name="Equipped explorer skin"};
                        slots[i]=handSkin;
                    }
                    renderer.sharedMaterials=slots;
                }
            }
            RefreshHandSkin();
        }
        void RefreshHandSkin()
        {
            if(handSkin==null)return;
            string selected=PlayerWardrobe.SelectedId;if(selected==skinId)return;
            skinId=selected;handSkin.SetColor("_BaseColor",SkinColors[PlayerAvatar.Index(selected)]);
        }
        void ReleaseHands()
        {
            foreach(var hand in new[]{leftHand,rightHand})if(hand!=null){hand.SetActive(false);Destroy(hand);}
            leftHand=null;rightHand=null;
            if(handSkin!=null)Destroy(handSkin);handSkin=null;skinId=null;
        }
        void CreateSmoke()
        {
            var obj=new GameObject("Muzzle smoke");obj.transform.SetParent(transform,false);smoke=obj.AddComponent<ParticleSystem>();smoke.Stop();
            var main=smoke.main;main.loop=false;main.playOnAwake=false;main.startLifetime=1.8f;main.startSpeed=1.8f;main.startSize=.15f;main.maxParticles=80;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.7f,.68f,.6f,.5f);
            var emission=smoke.emission;emission.enabled=false;var shape=smoke.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.035f;
            var size=smoke.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(1,3)));
            var color=smoke.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.65f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial=new Material(Resources.Load<Shader>("MuzzleSmoke"));
            var lamp=new GameObject("Muzzle flash");lamp.transform.SetParent(transform,false);flash=lamp.AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(1,.6f,.2f);flash.intensity=5;flash.range=4;flash.enabled=false;
        }
        static AudioClip Sound(string name,float duration,float frequency,float noise)
        {
            const int rate=22050;float[] samples=new float[(int)(rate*duration)];var random=new System.Random(91);
            for(int i=0;i<samples.Length;i++){float t=i/(float)rate;float envelope=Mathf.Exp(-t/duration*8);samples[i]=Mathf.Clamp(((float)random.NextDouble()*2-1)*noise+Mathf.Sin(t*frequency*Mathf.PI*2)*.25f,-1,1)*envelope;}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void OnGUI()
        {
            if(walker==null || walker.MenuOpen || PlayerWardrobe.IsOpen || ReferenceGrove.PhotoMode)return;
            GUI.matrix=Matrix4x4.identity;var state=ranch.Connection?.State;
            GUI.Box(new Rect(22,Screen.height-112,355,72),names[Tool]+" · Q change tool");
            GUI.Label(new Rect(35,Screen.height-87,330,25),ranch.Building?"Building · tools put away":Status);
            GUI.Label(new Rect(35,Screen.height-64,330,25),(state?.wood??0)+" wood · "+(state?.stone??0)+" stone · "+(state?.meat??0)+" venison");
            if(Reloading){GUI.Box(new Rect(Screen.width/2-120,Screen.height-165,240,25),"Reloading "+Mathf.CeilToInt((1-ReloadProgress)*ReloadSeconds)+"s");}
            else if(Tool>0 && !ranch.Building)GUI.Label(new Rect(Screen.width/2-3,Screen.height/2-12,30,24),"+");
        }
        void OnDestroy()
        {
            ReleaseHands();if(rig!=null)Destroy(rig);foreach(var clip in new[]{shot,wood,stone,click})if(clip!=null)Destroy(clip);
            if(smoke!=null){Destroy(smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial);Destroy(smoke.gameObject);}if(flash!=null)Destroy(flash.gameObject);
        }
    }
}
