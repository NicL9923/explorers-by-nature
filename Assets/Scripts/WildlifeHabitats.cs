using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    // Ambient animals are local scenery, like the existing deer. Ranch products remain server-owned.
    public sealed class WildlifeHabitats : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        void Start()
        {
            for (int i=0;i<5;i++) Spawn("RiverWildlife/Duck", "Mallard", new Vector2(i*1.4f-3,-155+i*3), true, i);
            for (int i=0;i<2;i++) Spawn("RiverWildlife/Beaver", "Beaver", new Vector2(ValleyWorld.ShoreX(-115+i*13,-1)-1.5f,-115+i*13), false, i+8);
            for (int i=0;i<2;i++) Spawn("Fox/Fox", "Red fox", new Vector2(-118+i*35,-178+i*74), false, i+12);
            BuildReeds();
        }

        void Spawn(string path,string name,Vector2 home,bool swims,int phase)
        {
            var root=new GameObject(name);root.transform.SetParent(transform);
            var model=ModelArt.Instantiate(path,root.transform,false);
            if(model==null){Destroy(root);return;}
            var actor=root.AddComponent<HabitatAnimal>();actor.home=home;actor.swims=swims;actor.phase=phase;
            actor.radius=name=="Beaver"?.5f:2;
            root.transform.position=HabitatAnimal.Position(home,swims,actor.radius,phase,0);
        }

        void BuildReeds()
        {
            var random=new System.Random(412);
            var material=new Material(Shader.Find("Explorers/MeadowGrass")){name="River reeds",enableInstancing=true};material.SetFloat("_WindStrength",.045f);owned.Add(material);
            for(int patch=0;patch<65;patch++)
            {
                float z=-260+patch*7.3f;int side=patch%2==0?-1:1;
                float x=ValleyWorld.ShoreX(z,side)+side*.6f;
                var vertices=new List<Vector3>();var colors=new List<Color>();var uv=new List<Vector2>();var triangles=new List<int>();
                for(int stem=0;stem<13;stem++)
                {
                    float wx=x+(float)random.NextDouble()*side*1.3f,wz=z+(float)random.NextDouble()*2;
                    bool restingSpot=false;
                    for(int b=0;b<2;b++) {float bz=-115+b*13; if(Vector2.Distance(new Vector2(wx,wz),new Vector2(ValleyWorld.ShoreX(bz,-1)-1.5f,bz))<2) restingSpot=true;}
                    if(restingSpot)continue;
                    Vector3 basePoint=ValleyShape.Ground(wx,wz);
                    float height=.6f+(float)random.NextDouble()*.7f;
                    for(int leaf=0;leaf<3;leaf++)
                    {
                        float angle=leaf*2.4f+stem;Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        Vector3 width=Vector3.Cross(direction,Vector3.up)*.025f;
                        Vector3 middle=basePoint+Vector3.up*height*.55f+direction*.09f;
                        int n=vertices.Count;
                        vertices.AddRange(new[]{basePoint-width,basePoint+width,middle-width,middle+width,basePoint+Vector3.up*height+direction*.3f});
                        Color green=Color.Lerp(new Color(.18f,.28f,.10f),new Color(.42f,.46f,.19f),(float)random.NextDouble()).linear;
                        colors.AddRange(new[]{green*.65f,green*.65f,green,green,green});
                        uv.AddRange(new[]{Vector2.zero,Vector2.zero,new Vector2(0,.5f),new Vector2(0,.5f),Vector2.up});
                        triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3,n+2,n+4,n+3});
                    }
                }
                var mesh=new Mesh{name="Bank reed clump"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
                var reeds=new GameObject("Riverbank reeds",typeof(MeshFilter),typeof(MeshRenderer),typeof(LODGroup));reeds.transform.SetParent(transform,false);reeds.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=reeds.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                var lod=reeds.GetComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.015f,new Renderer[]{renderer})});lod.RecalculateBounds();
            }
        }
        void OnDestroy(){foreach(Object asset in owned)if(asset!=null)Destroy(asset);}
    }

    public sealed class HabitatAnimal : MonoBehaviour
    {
        public Vector2 home;
        public bool swims;
        public int phase;
        public float radius=2;
        float clock;
        FirstPersonWalker observer;
        void Start()
        {
            observer=FindFirstObjectByType<FirstPersonWalker>();
            if(!swims)transform.rotation=Quaternion.FromToRotation(Vector3.up,SurfaceUp(transform.position));
        }
        static Vector3 SurfaceUp(Vector3 p)
        {
            return new Vector3(ValleyShape.Height(p.x-.3f,p.z)-ValleyShape.Height(p.x+.3f,p.z),.6f,ValleyShape.Height(p.x,p.z-.3f)-ValleyShape.Height(p.x,p.z+.3f)).normalized;
        }
        public static Vector3 Position(Vector2 home,bool swims,float radius,int phase,float time)
        {
            float angle=time*.12f+phase;
            float z=home.y+Mathf.Sin(angle*.7f)*radius;
            float x=swims?ValleyShape.RiverX(z)+home.x+Mathf.Cos(angle)*radius:home.x+Mathf.Cos(angle)*radius;
            if(swims)return new Vector3(x,ValleyShape.WaterHeight-.075f+Mathf.Sin(time*1.8f+phase)*.012f,z);
            // Keep bank animals on dry land even at the end of their small home range.
            x=Mathf.Min(x,ValleyWorld.ShoreX(z,-1)-.3f);
            return ValleyShape.Ground(x,z);
        }
        void Update()
        {
            bool watched=observer!=null&&Vector3.SqrMagnitude(observer.transform.position-transform.position)<16;
            if(!swims&&(watched||Mathf.Repeat(Time.time+phase,22)>9))return;
            clock+=Time.deltaTime;
            Vector3 next=Position(home,swims,radius,phase,clock);
            Vector3 direction=Position(home,swims,radius,phase,clock+.2f)-next;direction.y=0;
            Vector3 up=swims?Vector3.up:SurfaceUp(next);
            direction=Vector3.ProjectOnPlane(direction,up);
            if(direction.sqrMagnitude>.000001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction,up),Time.deltaTime*2);
            transform.position=next;
        }
    }
}
