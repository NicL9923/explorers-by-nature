using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ExplorersByNature
{
    // Local, cosmetic water interaction. No terrain, saved state or server authority changes.
    public sealed class RiverDynamics : MonoBehaviour
    {
        public static RiverDynamics Current { get; private set; }
        public RiverWaveField ActiveField { get; private set; }
        public int FloatingObjectCount => wood.Length;
        public int ImpactCount { get; private set; }
        public Vector3 LastImpact { get; private set; }
        FirstPersonWalker walker;
        Texture2D waves;
        Color[] pixels;
        Material water, woodMaterial, stoneMaterial;
        Mesh woodMesh;
        readonly Floater[] wood=new Floater[8];
        readonly Pebble[] pebbles=new Pebble[6];
        int pebbleIndex;
        float accumulator, stepDistance, throwCooldown;
        Vector3 previousPosition;
        float centerZ;
        bool fieldInitialized;
        sealed class Floater { public Transform transform;public Vector3 velocity;public float radius; }
        sealed class Pebble { public Transform transform;public Vector3 velocity;public float life; }

        public void Initialize(FirstPersonWalker player,Material material)
        {
            Current=this; walker=player;water=material;
            ActiveField=new RiverWaveField(65,97,.75f);
            waves=new Texture2D(65,97,TextureFormat.RGBAHalf,false,true){name="Local river displacement and slopes",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            pixels=new Color[65*97];
            water.SetTexture("_RiverWaves",waves);
            woodMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Waterlogged driftwood"};
            woodMaterial.SetColor("_BaseColor",new Color(.38f,.32f,.24f));
            woodMaterial.SetTexture("_BaseMap",Resources.Load<Texture2D>("ReferenceTrees/Fir_bark_BaseColor"));
            woodMaterial.SetFloat("_Smoothness",.3f);
            stoneMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Skipping stone",color=new Color(.22f,.25f,.23f)};
            woodMesh=MakeBranch();
            for(int i=0;i<wood.Length;i++)
            {
                var obj=new GameObject("Floating weathered branch");obj.transform.SetParent(transform,false);
                obj.AddComponent<MeshFilter>().sharedMesh=woodMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=woodMaterial;
                float scale=.65f+i*.075f;obj.transform.localScale=Vector3.one*scale;
                wood[i]=new Floater{transform=obj.transform,radius=.07f*scale};
            }
            for(int i=0;i<pebbles.Length;i++)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name="Tossed river pebble";obj.transform.SetParent(transform,false);
                Destroy(obj.GetComponent<Collider>());obj.GetComponent<MeshRenderer>().sharedMaterial=stoneMaterial;
                obj.transform.localScale=new Vector3(.11f,.055f,.085f);obj.SetActive(false);
                pebbles[i]=new Pebble{transform=obj.transform};
            }
            Recenter(player.transform.position.z);
            previousPosition=walker.transform.position;
        }
        public static float DepthAt(float x,float z)=>Mathf.Max(0,ValleyShape.WaterHeight-ValleyShape.Height(x,z));
        public static Vector3 CurrentAt(Vector3 p)
        {
            float depth=DepthAt(p.x,p.z);
            return new Vector3(.264f*Mathf.Cos(p.z*.012f),0,1).normalized*(.25f+.24f*Mathf.Min(depth,3))*Mathf.SmoothStep(0,1,depth/.45f);
        }
        public float SurfaceHeight(Vector3 p)=>ValleyShape.WaterHeight+(ActiveField==null?0:ActiveField.Sample(p.x,p.z));
        public void AddImpulse(Vector3 p,float strength,float radius)
        {
            if(DepthAt(p.x,p.z)<.025f)return;
            ActiveField?.Impulse(p.x,p.z,strength,radius); ImpactCount++;LastImpact=p;
        }
        public void Visit()
        {
            FindFirstObjectByType<RanchComfort>()?.Stand();
            const float z=-165; float x=ValleyWorld.ShoreX(z,-1)-1.4f;
            walker.Teleport(new Vector3(x,ValleyShape.Height(x,z)+.15f,z));
            walker.transform.rotation=Quaternion.LookRotation(new Vector3(ValleyShape.RiverX(z+20),ValleyShape.WaterHeight,z+20)-walker.transform.position);
            Vector3 euler=walker.transform.eulerAngles;walker.transform.rotation=Quaternion.Euler(0,euler.y,0);
            walker.view.transform.localRotation=Quaternion.Euler(12,0,0);walker.SyncLookPitch();walker.SetMenu(false);
            Recenter(z);previousPosition=walker.transform.position;
        }
        public static float DesiredCenter(float z)=>Mathf.Clamp(Mathf.Round(z/12)*12,-414,414);
        public static bool NeedsRecenter(float z,float currentCenter)=>Mathf.Abs(z-currentCenter)>24 && DesiredCenter(z)!=currentCenter;
        void Recenter(float z)
        {
            float desired=DesiredCenter(z);
            if(fieldInitialized && desired==centerZ)return;
            centerZ=desired;
            // Align both axes to cells so ordinary grid shifts preserve waves exactly.
            float x=Mathf.Round(ValleyShape.RiverX(centerZ)/ActiveField.Spacing)*ActiveField.Spacing;
            Vector2 origin=new Vector2(x-24,centerZ-36);
            Vector2 Flow(float wx,float wz){Vector3 flow=CurrentAt(new Vector3(wx,0,wz));return new Vector2(flow.x,flow.z);}
            if(fieldInitialized)ActiveField.Shift(origin,DepthAt,Flow);
            else ActiveField.Reset(origin,DepthAt,Flow);
            water.SetVector("_RiverGrid",new Vector4(ActiveField.Origin.x,ActiveField.Origin.y,48,72));
            for(int i=0;i<wood.Length;i++)
            {
                Vector3 p=wood[i].transform.position;
                if(!fieldInitialized || p.z<centerZ-32 || p.z>centerZ+32 || p.x<origin.x+1 || p.x>origin.x+47)
                    ResetWood(i,centerZ-28+i*7);
            }
            fieldInitialized=true;
            UploadWaves();
        }
        void ResetWood(int i,float z)
        {
            float x=ValleyShape.RiverX(z)+Mathf.Sin(i*2.7f)*5;
            wood[i].transform.position=new Vector3(x,ValleyShape.WaterHeight,z);
            wood[i].transform.rotation=Quaternion.Euler(0,i*57,0);wood[i].velocity=CurrentAt(wood[i].transform.position);
        }
        void Update()
        {
            if(ActiveField==null || walker==null)return;
            Vector3 p=walker.transform.position;
            if(NeedsRecenter(p.z,centerZ))Recenter(p.z);
            if(!walker.Automated && !walker.MenuOpen && Input.GetKeyDown(KeyCode.G))ThrowPebble();
            throwCooldown=Mathf.Max(0,throwCooldown-Time.deltaTime);
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            water.SetFloat("_RiverSceneColor",pipeline!=null && pipeline.supportsCameraOpaqueTexture && pipeline.supportsCameraDepthTexture?1:0);
            // Stop CPU waves while far from the river. The full-length shader keeps flowing.
            bool nearby=Mathf.Abs(p.x-ValleyShape.RiverX(p.z))<65;
            float distance=Vector3.Distance(p,previousPosition);previousPosition=p;
            if(nearby && DepthAt(p.x,p.z)>.08f && p.y<ValleyShape.WaterHeight+.3f && distance<2)
            {
                stepDistance+=distance;
                if(stepDistance>.75f){AddImpulse(p,-.7f,1.4f);stepDistance=0;}
            }
            if(!nearby)return;
            accumulator=Mathf.Min(accumulator+Time.deltaTime,.1f);
            while(accumulator>=1f/30){ActiveField.Step(1f/30);SimulateObjects(1f/30);accumulator-=1f/30;}
            UploadWaves();
        }
        public bool ThrowPebble()
        {
            if(throwCooldown>0 || walker==null || Mathf.Abs(walker.transform.position.x-ValleyShape.RiverX(walker.transform.position.z))>45)return false;
            var pebble=pebbles[pebbleIndex++%pebbles.Length];
            pebble.transform.position=walker.view.transform.position+walker.view.transform.forward*.7f-walker.view.transform.right*.15f;
            pebble.velocity=walker.view.transform.forward*10+Vector3.up*3;
            pebble.life=5;pebble.transform.gameObject.SetActive(true);throwCooldown=.35f;return true;
        }
        void SimulateObjects(float dt)
        {
            for(int i=0;i<wood.Length;i++)
            {
                Floater f=wood[i];Vector3 p=f.transform.position;
                Vector3 current=CurrentAt(p);
                f.velocity.x=Mathf.Lerp(f.velocity.x,current.x,1-Mathf.Exp(-dt*1.8f));
                f.velocity.z=Mathf.Lerp(f.velocity.z,current.z,1-Mathf.Exp(-dt*1.8f));
                RiverWaveField.FloatStep(ref p.y,ref f.velocity.y,SurfaceHeight(p),f.radius,dt);
                p.x+=f.velocity.x*dt;p.z+=f.velocity.z*dt;
                if(p.z>centerZ+32 || DepthAt(p.x,p.z)<.12f){ResetWood(i,centerZ-30+i*.25f);continue;}
                f.transform.position=p;
                float hx=ActiveField.Sample(p.x+.4f,p.z)-ActiveField.Sample(p.x-.4f,p.z);
                float hz=ActiveField.Sample(p.x,p.z+.4f)-ActiveField.Sample(p.x,p.z-.4f);
                float yaw=f.transform.eulerAngles.y+dt*(Mathf.Sin(p.z*.17f+i)*5);
                f.transform.rotation=Quaternion.Slerp(f.transform.rotation,Quaternion.FromToRotation(Vector3.up,new Vector3(-hx/.8f,1,-hz/.8f))*Quaternion.Euler(0,yaw,0),dt*4);
                ActiveField.Impulse(p.x,p.z,-.022f,.95f);
            }
            foreach(Pebble pebble in pebbles)
            {
                if(pebble.life<=0)continue;
                Vector3 old=pebble.transform.position;pebble.velocity+=Vector3.down*(9.81f*dt);
                Vector3 p=old+pebble.velocity*dt;pebble.transform.position=p;pebble.life-=dt;
                float surface=SurfaceHeight(p);
                if(old.y>=surface && p.y<=surface && DepthAt(p.x,p.z)>.03f)
                {AddImpulse(p,-1.8f,1.5f);pebble.life=0;}
                if(p.y<ValleyShape.Height(p.x,p.z) || pebble.life<=0){pebble.life=0;pebble.transform.gameObject.SetActive(false);}
            }
        }
        void UploadWaves()
        {
            int w=ActiveField.Width,h=ActiveField.Height;float[] elevation=ActiveField.Elevation;
            for(int z=0;z<h;z++)for(int x=0;x<w;x++)
            {
                int i=z*w+x;
                float dx=(elevation[z*w+Mathf.Min(x+1,w-1)]-elevation[z*w+Mathf.Max(x-1,0)])/(2*ActiveField.Spacing);
                float dz=(elevation[Mathf.Min(z+1,h-1)*w+x]-elevation[Mathf.Max(z-1,0)*w+x])/(2*ActiveField.Spacing);
                pixels[i]=new Color(dx,dz,elevation[i],Mathf.Clamp01((Mathf.Abs(dx)+Mathf.Abs(dz))*.8f));
            }
            waves.SetPixels(pixels);waves.Apply(false,false);
        }
        static Mesh MakeBranch()
        {
            // Broken, tapered wood, with uneven rings and real bark UVs. Shared by all floaters.
            const int rings=9,sides=9;var v=new Vector3[rings*sides];var uv=new Vector2[v.Length];var tri=new int[(rings-1)*sides*6];
            for(int r=0;r<rings;r++)for(int s=0;s<sides;s++)
            {
                float t=r/(float)(rings-1),a=s/(float)sides*Mathf.PI*2;
                float radius=Mathf.Lerp(.09f,.027f,t)*(1+Mathf.Sin(s*7+r*2.3f)*.14f);
                v[r*sides+s]=new Vector3((t-.5f)*1.8f,Mathf.Sin(a)*radius+Mathf.Sin(t*5)*.025f,Mathf.Cos(a)*radius+Mathf.Sin(t*3)*.08f);
                uv[r*sides+s]=new Vector2(t*2,s/(float)sides);
                if(r==rings-1)continue;int n=(r*sides+s)*6,k=r*sides+s,next=r*sides+(s+1)%sides;
                tri[n]=k;tri[n+1]=k+sides;tri[n+2]=next;tri[n+3]=next;tri[n+4]=k+sides;tri[n+5]=next+sides;
            }
            var mesh=new Mesh{name="Tapered driftwood branch"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        void OnDestroy()
        {
            if(Current==this)Current=null;
            if(water!=null){water.SetVector("_RiverGrid",Vector4.zero);water.SetTexture("_RiverWaves",null);}
            if(waves!=null)Destroy(waves);if(woodMesh!=null)Destroy(woodMesh);
            if(woodMaterial!=null)Destroy(woodMaterial);if(stoneMaterial!=null)Destroy(stoneMaterial);
        }
    }
}
