using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    [DefaultExecutionOrder(180)]
    public sealed class AnimalFur : MonoBehaviour
    {
        AnimalFurGroom groom;
        Mesh mesh;
        MeshRenderer furRenderer;
        Material[] materials;
        Vector3[] vertices,normals,previousRoots;
        Vector2[] coatUV,cardUV;
        Matrix4x4[] palette;
        Transform[] bones;
        FurSpring[] springs;
        int[] selected;
        int quality=-1;
        bool awake;
        Camera view;
        public int ConfiguredClumps => selected == null ? 0 : selected.Length;
        public int ActiveClumps => selected == null || furRenderer == null || !furRenderer.enabled ? 0 : selected.Length;
        public float MaximumBend { get; private set; }

        public static void Attach(GameObject model)
        {
            if(model.GetComponent<AnimalFurGroom>() != null && model.GetComponent<AnimalFur>() == null)model.AddComponent<AnimalFur>();
        }
        void Start()
        {
            groom=GetComponent<AnimalFurGroom>();
            if(groom==null || groom.roots==null || groom.roots.Length==0){enabled=false;return;}
            // The CPU groom uses four weights; retain matching base-surface skinning on Low.
            groom.skin.quality=SkinQuality.Bone4;
            var child=new GameObject("Simulated coat");child.transform.SetParent(transform,false);
            mesh=new Mesh { name="Root-pinned fur clumps" };mesh.MarkDynamic();
            child.AddComponent<MeshFilter>().sharedMesh=mesh;
            furRenderer=child.AddComponent<MeshRenderer>();
            // Tiny strands receive the coat's lighting; no expensive per-hair shadow maps.
            furRenderer.shadowCastingMode=ShadowCastingMode.Off;furRenderer.receiveShadows=true;
            furRenderer.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
            Material[] source=groom.skin.sharedMaterials;materials=new Material[source.Length];
            Shader shader=Resources.Load<Shader>("AnimalFur");
            for(int i=0;i<materials.Length;i++)
            {
                materials[i]=new Material(shader){name=source[i].name+" simulated fur"};
                materials[i].SetTexture("_BaseMap",source[i].GetTexture("_BaseMap"));
                materials[i].SetColor("_BaseColor",source[i].HasProperty("_BaseColor")?source[i].GetColor("_BaseColor"):Color.white);
            }
            furRenderer.sharedMaterials=materials;
            palette=new Matrix4x4[groom.bindposes.Length];bones=groom.skin.bones;
            previousRoots=new Vector3[groom.roots.Length];springs=new FurSpring[groom.roots.Length];
            view=Camera.main;
        }
        void Configure(int level)
        {
            quality=level;
            var indices=new List<int>();for(int i=0;i<groom.roots.Length;i++)if(level>0 || i%3==0)indices.Add(i);
            selected=indices.ToArray();int count=selected.Length*12;
            vertices=new Vector3[count];normals=new Vector3[count];coatUV=new Vector2[count];cardUV=new Vector2[count];
            var triangles=new List<int>[materials.Length];for(int i=0;i<triangles.Length;i++)triangles[i]=new List<int>();
            for(int i=0;i<selected.Length;i++)
            {
                var root=groom.roots[selected[i]];
                for(int plane=0;plane<2;plane++)
                {
                    int b=i*12+plane*6;
                    for(int row=0;row<3;row++)for(int side=0;side<2;side++)
                    {int index=b+row*2+side;coatUV[index]=root.uv;cardUV[index]=new Vector2(side,row*.5f);}
                    for(int row=0;row<2;row++)
                    {int a=b+row*2;triangles[root.material].AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
                }
            }
            mesh.Clear();mesh.vertices=vertices;mesh.normals=normals;mesh.uv=coatUV;mesh.uv2=cardUV;
            mesh.subMeshCount=triangles.Length;for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i,false);
            awake=false;
        }
        void LateUpdate()
        {
            if(groom==null || furRenderer==null)return;
            if(view==null)view=Camera.main;
            bool high=QualitySettings.GetQualityLevel()>0;
            if(quality!=(high?1:0))Configure(high?1:0);
            float distance=view==null?float.MaxValue:Vector3.Distance(view.transform.position,groom.skin.bounds.center);
            if(distance>(high?19:10) || !groom.skin.isVisible)
            {furRenderer.enabled=false;awake=false;return;}
            furRenderer.enabled=true;
            for(int i=0;i<palette.Length;i++)palette[i]=bones[i].localToWorldMatrix*groom.bindposes[i];
            Matrix4x4 local=transform.worldToLocalMatrix;
            float scale=Mathf.Max(.01f,Mathf.Abs(transform.lossyScale.x));
            Vector3 wind=WindWeather.VelocityAt(transform.position,WindWeather.SimulationTime);
            MaximumBend=0;
            for(int i=0;i<selected.Length;i++)
            {
                int sample=selected[i];var root=groom.roots[sample];BoneWeight w=root.weights;
                Vector3 p=Point(w,root.position),n=Direction(w,root.normal).normalized;
                float length=root.length*scale;
                if(!awake)springs[sample]=default;
                else springs[sample].Step(p-previousRoots[sample],wind,n,length,Time.deltaTime);
                previousRoots[sample]=p;
                Vector3 bend=springs[sample].displacement;
                MaximumBend=Mathf.Max(MaximumBend,bend.magnitude);
                Vector3 tangent=Vector3.ProjectOnPlane(-transform.up,n).normalized;
                if(tangent.sqrMagnitude<.1f)tangent=Vector3.ProjectOnPlane(-transform.forward,n).normalized;
                Vector3 growth=(length>.06f?n*.25f+tangent*.97f:n*.72f+tangent*.69f).normalized*length;
                // Preserve root-to-tip length while allowing a curved intermediate segment.
                Vector3 tip=(growth+bend).normalized*length;
                Vector3 axis=Vector3.Cross(n,Mathf.Abs(n.y)>.9f?Vector3.forward:Vector3.up).normalized;
                axis=Quaternion.AngleAxis(root.roll*Mathf.Rad2Deg,n)*axis;
                p-=n*.001f*scale;
                for(int plane=0;plane<2;plane++)
                {
                    Vector3 side=plane==0?axis:Vector3.Cross(n,axis);
                    int b=i*12+plane*6;
                    for(int row=0;row<3;row++)
                    {
                        float t=row*.5f;
                        Vector3 center=p+growth*t+(tip-growth)*t*t;
                        float width=length*.30f*(1-t*.7f);
                        vertices[b+row*2]=local.MultiplyPoint3x4(center-side*width);
                        vertices[b+row*2+1]=local.MultiplyPoint3x4(center+side*width);
                        normals[b+row*2]=normals[b+row*2+1]=local.MultiplyVector(n).normalized;
                    }
                }
            }
            mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();awake=true;
        }
        Vector3 Point(BoneWeight w,Vector3 p)=>palette[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+palette[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+palette[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+palette[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
        Vector3 Direction(BoneWeight w,Vector3 p)=>palette[w.boneIndex0].MultiplyVector(p)*w.weight0+palette[w.boneIndex1].MultiplyVector(p)*w.weight1+palette[w.boneIndex2].MultiplyVector(p)*w.weight2+palette[w.boneIndex3].MultiplyVector(p)*w.weight3;
        void OnDisable(){if(furRenderer!=null)furRenderer.enabled=false;awake=false;}
        void OnDestroy()
        {
            if(mesh!=null)Destroy(mesh);
            if(materials!=null)foreach(var material in materials)if(material!=null)Destroy(material);
        }
    }
}
