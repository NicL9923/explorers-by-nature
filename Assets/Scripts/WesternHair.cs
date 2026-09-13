using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    /// <summary>Authored meter-space mane, switch and loose explorer hairs attached to imported rigid pivots.</summary>
    [DefaultExecutionOrder(190)]
    public sealed class WesternHair : MonoBehaviour
    {
        sealed class Lock
        {
            public Transform bone;
            public Vector3 root, growth, normal;
            public float width;
            public bool mane;
            public Matrix4x4 metersToBone;
            public Vector3[] combed;
            public readonly HairGuide guide = new HairGuide();
        }
        readonly List<Lock> locks = new List<Lock>();
        Mesh mesh; Material material; MeshRenderer hairRenderer;
        Vector3[] vertices, normals; Vector4[] tangents;
        Camera view;
        bool reset=true;
        public int GuideCount => locks.Count;
        public float MaximumBend {get;private set;}

        public static void Attach(GameObject model,string path)
        {
            bool horse=path=="Horse/Horse",player=path.StartsWith("Players/");
            if((!horse && !player) || model.GetComponent<WesternHair>()!=null)return;
            var hair=model.AddComponent<WesternHair>();
            hair.Build(horse,path);
        }
        void Build(bool horse,string path)
        {
            Transform logical=transform.parent??transform;
            Transform Bone(string name)=>GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
            void Add(Transform bone,Vector3 position,Vector3 growth,Vector3 normal,float width,bool mane=false,Vector3[] combed=null)
            {
                if(bone==null)return;
                var strand=new Lock {bone=bone,root=bone.InverseTransformPoint(transform.position+logical.TransformDirection(position)),
                    growth=bone.InverseTransformVector(logical.TransformDirection(growth)),normal=bone.InverseTransformDirection(logical.TransformDirection(normal)),width=width,mane=mane,metersToBone=bone.worldToLocalMatrix*Matrix4x4.TRS(transform.position,logical.rotation,Vector3.one)};
                if(combed!=null)strand.combed=combed.Select(point=>strand.metersToBone.MultiplyPoint3x4(point)).ToArray();
                locks.Add(strand);
            }
            var random=new System.Random(9817);float Next()=>(float)random.NextDouble();
            if(horse)
            {
                for(int i=0;i<190;i++)
                {
                    // Stratify roots so random clustering cannot leave bald strips along the crest.
                    float t=(i+Next())/190f;
                    // Sample the actual arched-neck rings used by make-horse-art.py.
                    // Roots lie on the rear crest. A smooth authored arc wraps around
                    // the neck; simulated tip motion bends it without breaking its flow.
                    float along=.18f+t*.82f;
                    var combed=new Vector3[HairGuide.Segments+1];
                    float lengthVariation=Next();
                    for(int row=0;row<=HairGuide.Segments;row++)
                    {
                        float u=row/(float)HairGuide.Segments;
                        NeckRing(along-.075f*u*u,out Vector3 center,out Vector3 back,out float radius,out float depth);
                        float angle=.46f+u*(1.02f+lengthVariation*.12f);
                        combed[row]=center+Vector3.left*((radius+.018f)*Mathf.Sin(angle))+back*((depth+.018f)*Mathf.Cos(angle));
                        combed[row]+=Vector3.down*(.065f*u*u)+Vector3.left*(.015f*u*u);
                    }
                    NeckRing(along,out _,out Vector3 rootBack,out _,out _);
                    Add(Bone("Neck"),combed[0],combed[HairGuide.Segments]-combed[0],
                        (Vector3.left*.7f+rootBack*.3f).normalized,.0055f+Next()*.002f,true,combed);
                }
                for(int i=0;i<180;i++)
                {
                    float angle=Next()*Mathf.PI*2;
                    Vector3 radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                    // Follow the outside of the solid switch, rather than emerging through its tip.
                    Add(Bone("Tail"),new Vector3(radial.x*.073f,1.29f,-1.005f+radial.z*.078f),
                        new Vector3(radial.x*.025f,-.98f-Next()*.065f,-.145f+radial.z*.008f),new Vector3(radial.x,-radial.z*.15f,radial.z).normalized,.0018f);
                }
            }
            else
            {
                var head=Bone("Head");
                for(int i=0;i<110;i++)
                {
                    float side=i%2==0?-1:1;
                    bool longHair=path.EndsWith("TrailScout") || path.EndsWith("Homesteader");
                    // The temple ellipsoid ends at x=+/-.089. Keep wisps on its outer
                    // rear surface and comb them behind the ear, not across exposed skin.
                    Add(head,new Vector3(side*(.089f+Next()*.003f),1.685f+Next()*.014f,-.026f-Next()*.017f),
                        new Vector3(side*.009f,-(longHair?.085f:.043f)*(1+Next()*.2f),-.042f),new Vector3(side,0,-.2f).normalized,.0008f);
                }
            }
            var child=new GameObject("Wind-combed hair");child.transform.SetParent(transform,false);
            mesh=new Mesh {name="Six-segment western hair"};mesh.MarkDynamic();
            child.AddComponent<MeshFilter>().sharedMesh=mesh;hairRenderer=child.AddComponent<MeshRenderer>();
            material=new Material(Resources.Load<Shader>("AnimalFur"));
            material.SetColor("_BaseColor",horse?new Color(.032f,.024f,.018f):path.EndsWith("Homesteader")?new Color(.20f,.063f,.018f):new Color(.045f,.028f,.015f));
            material.SetFloat("_FiberCount",1);
            material.SetFloat("_SpecularStrength",horse?.025f:.07f);
            material.SetFloat("_Roughness",horse?.65f:.35f);material.SetFloat("_Transmission",.42f);
            hairRenderer.sharedMaterial=material;hairRenderer.shadowCastingMode=ShadowCastingMode.On;
            const int stride=(HairGuide.Segments+1)*2;
            vertices=new Vector3[locks.Count*stride];normals=new Vector3[vertices.Length];tangents=new Vector4[vertices.Length];
            var uv=new Vector2[vertices.Length];var cards=new Vector2[vertices.Length];var triangles=new List<int>();
            for(int i=0;i<locks.Count;i++)for(int row=0;row<=HairGuide.Segments;row++)
            {
                int b=i*stride+row*2;cards[b]=new Vector2(0,row/(float)HairGuide.Segments);cards[b+1]=new Vector2(1,row/(float)HairGuide.Segments);
                if(row<HairGuide.Segments)triangles.AddRange(new[]{b,b+2,b+1,b+1,b+2,b+3});
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.uv2=cards;mesh.SetTriangles(triangles,0);
            view=Camera.main;
        }
        void LateUpdate()
        {
            if(mesh==null)return;
            if(view==null)view=Camera.main;
            bool visible=view==null || Vector3.Distance(view.transform.position,transform.position)<40;
            // Wardrobe studio has a dedicated camera far beneath the world.
            var avatar=GetComponentInParent<PlayerAvatar>();if(avatar!=null && avatar.Preview)visible=true;
            if(locks.Count>0 && !locks[0].bone.gameObject.activeInHierarchy)visible=false;
            hairRenderer.enabled=visible;if(!visible){reset=true;return;}
            MaximumBend=0;var local=transform.worldToLocalMatrix;
            Vector3 wind=WindWeather.VelocityAt(transform.position,WindWeather.SimulationTime);
            for(int i=0;i<locks.Count;i++)
            {
                var strand=locks[i];Vector3 root=strand.bone.TransformPoint(strand.root),growth=strand.bone.TransformVector(strand.growth),normal=strand.bone.TransformDirection(strand.normal).normalized;
                if(reset)strand.guide.Reset(root,growth);else strand.guide.Step(root,growth,normal,wind,Time.deltaTime);
                // Keep collision/shape corrections out of Verlet history. Writing projected
                // vertices back into the solver injected false velocity and kinked every row.
                Vector3 displacement=Vector3.ClampMagnitude(strand.guide.points[6]-(root+growth),.035f);
                Vector3 Point(int row)
                {
                    if(!strand.mane)return strand.guide.points[row];
                    float u=row/(float)HairGuide.Segments;
                    return strand.bone.TransformPoint(strand.combed[row])+displacement*(u*u);
                }
                Vector3 ribbonSide=Vector3.Cross(growth,normal).normalized;
                MaximumBend=Mathf.Max(MaximumBend,strand.mane?displacement.magnitude:Vector3.Distance(strand.guide.points[6],root+growth));
                for(int row=0;row<=HairGuide.Segments;row++)
                {
                    Vector3 direction=(Point(Mathf.Min(6,row+1))-Point(Mathf.Max(0,row-1))).normalized;
                    // Transport one frame down the lock; a fresh cross product at each row
                    // can reverse the ribbon when a strand bends through its normal.
                    Vector3 side=Vector3.ProjectOnPlane(ribbonSide,direction).normalized;
                    if(side.sqrMagnitude<.1f)side=Vector3.Cross(direction,normal).normalized;
                    if(Vector3.Dot(side,ribbonSide)<0)side=-side;
                    ribbonSide=side;
                    float width=strand.width*Mathf.Pow(1-row/6f*.97f,.6f);
                    int b=(i*7+row)*2;vertices[b]=local.MultiplyPoint3x4(Point(row)-side*width);vertices[b+1]=local.MultiplyPoint3x4(Point(row)+side*width);
                    normals[b]=normals[b+1]=local.MultiplyVector(normal).normalized;
                    Vector3 tangent=local.MultiplyVector(direction).normalized;tangents[b]=tangents[b+1]=new Vector4(tangent.x,tangent.y,tangent.z,1);
                }
            }
            mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.RecalculateBounds();reset=false;
        }
        // Meter-space centerline and ring radii from the authored horse neck.
        static readonly Vector3[] NeckCenters={new Vector3(0,1.32f,.57f),new Vector3(0,1.50f,.67f),new Vector3(0,1.71f,.81f),new Vector3(0,1.89f,.91f),new Vector3(0,2,1)};
        static readonly float[] NeckRadii={.20f,.195f,.15f,.115f,.095f};
        static readonly float[] NeckDepths={.25f,.27f,.24f,.19f,.13f};
        static void NeckRing(float t,out Vector3 center,out Vector3 back,out float radius,out float depth)
        {
            float at=Mathf.Clamp01(t)*4;int i=Mathf.Min(3,(int)at);float f=at-i;
            // Match tangents on both sides of every authored ring. A segment-constant
            // frame displaced adjacent root rows, leaving four diagonal bald bands.
            Vector3 Tangent(int k)=>(NeckCenters[Mathf.Min(4,k+1)]-NeckCenters[Mathf.Max(0,k-1)])/(k==0 || k==4?1:2);
            float f2=f*f,f3=f2*f;
            Vector3 m0=Tangent(i),m1=Tangent(i+1);
            center=(2*f3-3*f2+1)*NeckCenters[i]+(f3-2*f2+f)*m0+(-2*f3+3*f2)*NeckCenters[i+1]+(f3-f2)*m1;
            Vector3 direction=((6*f2-6*f)*NeckCenters[i]+(3*f2-4*f+1)*m0+(-6*f2+6*f)*NeckCenters[i+1]+(3*f2-2*f)*m1).normalized;
            back=new Vector3(0,direction.z,-direction.y);
            float Radius(float[] values)
            {
                float a=(values[Mathf.Min(4,i+1)]-values[Mathf.Max(0,i-1)])/(i==0?1:2);
                int j=i+1;float b=(values[Mathf.Min(4,j+1)]-values[Mathf.Max(0,j-1)])/(j==4?1:2);
                return (2*f3-3*f2+1)*values[i]+(f3-2*f2+f)*a+(-2*f3+3*f2)*values[j]+(f3-f2)*b;
            }
            radius=Radius(NeckRadii);depth=Radius(NeckDepths);
        }
        void OnDisable(){reset=true;if(hairRenderer!=null)hairRenderer.enabled=false;}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
