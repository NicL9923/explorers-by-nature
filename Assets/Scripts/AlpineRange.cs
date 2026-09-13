using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    // Visual geology only. The terrain collider and shared height function remain authoritative.
    public sealed class AlpineRange : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        void OnDestroy() { foreach (var value in owned) if (value != null) Destroy(value); }

        // Unequal crown widths and glacial wall positions are authored at landscape scale.
        // Rounded roofs, near-vertical valley faces and sloping backs give each mass volume.
        struct Mass
        {
            public float x, z, width, depth, height, cut, seed, wall;
            public Mass(float x, float z, float width, float depth, float height, float cut, float seed, float wall)
            { this.x=x; this.z=z; this.width=width; this.depth=depth; this.height=height; this.cut=cut; this.seed=seed; this.wall=wall; }
        }
        static readonly Mass[] Masses = {
            new Mass(-315,560,228,276,357,390,19,44),
            new Mass(342,632,238,298,395,453,61,53),
            new Mass(-209,385,184,231,310,238,43,70),
            new Mass(167,423,193,244,325,265,89,85),
            new Mass(-611,705,258,317,334,449,93,82),
            new Mass(693,757,260,321,351,485,137,91)
        };

        static float GraniteHeight(float x, float z, Mass mass)
        {
            float u=(x-mass.x)/mass.width;
            float v=(z-mass.z)/mass.depth;
            if(Mathf.Abs(u)>1.4f || v>1.4f || z<mass.cut-115) return 0;
            float broad=Mathf.PerlinNoise(u*2.7f+mass.seed,v*2.3f+23)-.5f;
            float medium=Mathf.PerlinNoise(u*9.3f+mass.seed,v*7.1f+83)-.5f;
            // A broken, asymmetric granite crown: an offset highest shoulder and an
            // oblique descending roof give the massif a different silhouette on each side.
            float offsetU=u+.10f*v-.06f;
            float radius=Mathf.Pow(Mathf.Abs(offsetU),2.8f)+Mathf.Pow(Mathf.Abs(v*.8f),2.5f);
            radius+=broad*.14f+medium*.032f;
            float shoulder=Mathf.Exp(-Mathf.Pow((u+.36f)/.44f,2))*16;
            float crown=mass.height-43*u*u-36*v*v-17*u+shoulder+broad*28+medium*9;
            float side=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.20f,.73f,radius));
            float back=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.28f,.51f,v));
            // Glacial walls are cut through unequal buttresses, with broad diagonal
            // recesses. There are no full-height evenly spaced vertical slots.
            float front=mass.cut+19*u*u+broad*27+medium*7;
            float recessU=u-.21f-v*.19f;
            front+=Mathf.Exp(-recessU*recessU/.034f)*12;
            float wallWidth=mass.wall+13*Mathf.PerlinNoise(u*3.7f+mass.seed,53);
            float wall=Mathf.SmoothStep(0,1,Mathf.InverseLerp(front,front+wallWidth,z));
            float upper=crown*side*back*wall;
            float apronFront=front-92-19*broad;
            float apron=Mathf.SmoothStep(0,1,Mathf.InverseLerp(apronFront,front+12,z));
            float talus=(94+19*broad)*apron*Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.4f,.78f,Mathf.Abs(u)))*back;
            return Mathf.Max(upper,talus);
        }

        // The distant Sierra-like watershed is lower in angular size than the valley walls.
        // Broad saddles alternate with small broken summits, without repeating triangular peaks.
        static readonly Vector3[] Crest = {
            new Vector3(-1600,65,1160), new Vector3(-1390,260,1200),
            new Vector3(-1190,335,1160), new Vector3(-1025,295,1190),
            new Vector3(-850,433,1260), new Vector3(-745,451,1280),
            new Vector3(-612,368,1190), new Vector3(-430,391,1240),
            new Vector3(-295,478,1320), new Vector3(-214,460,1280),
            new Vector3(-104,517,1360), new Vector3(-12,441,1330),
            new Vector3(132,421,1310), new Vector3(250,489,1400),
            new Vector3(334,507,1410), new Vector3(487,390,1340),
            new Vector3(645,415,1330), new Vector3(791,356,1360),
            new Vector3(935,422,1460), new Vector3(1100,330,1400),
            new Vector3(1290,297,1430), new Vector3(1600,65,1480)
        };

        static float RangeHeight(float x,float z)
        {
            int segment=0;
            while(segment<Crest.Length-2 && x>Crest[segment+1].x) segment++;
            Vector3 a=Crest[segment],b=Crest[segment+1];
            float t=Mathf.InverseLerp(a.x,b.x,x);
            float distance=z-Mathf.Lerp(a.z,b.z,t);
            float crown=Mathf.Lerp(a.y,b.y,t);
            float broad=(Mathf.PerlinNoise(x*.0053f+42,z*.0061f+19)-.5f)*43;
            float drainage=1-Mathf.Abs(Mathf.PerlinNoise(x*.014f+71,z*.009f+93)*2-1);
            float flank=Mathf.Clamp01(Mathf.Abs(distance)/140);
            float ridge=crown-Mathf.Abs(distance)*(distance<0?.64f:.91f);
            ridge+=broad*flank-drainage*drainage*26*flank;
            return Mathf.Max(18,ridge);
        }

        static List<float> Axis(float min,float max,float detailedMin,float detailedMax,float nearStep,float farStep)
        {
            var values=new List<float>();
            float value=min;
            while(value<max)
            {
                values.Add(value);
                float step=value>=detailedMin && value<detailedMax?nearStep:farStep;
                float next=Mathf.Min(max,value+step);
                if(value<detailedMin && next>detailedMin)next=detailedMin;
                if(value<detailedMax && next>detailedMax)next=detailedMax;
                value=next;
            }
            values.Add(max);
            return values;
        }

        public void Build(Terrain terrain)
        {
            var source=Resources.Load<Material>("AlpineRange/Mountain");
            if(source==null) { Debug.LogError("Missing alpine rock material"); return; }
            // One connected surface prevents an abrupt terrain/backdrop boundary. Sampling
            // concentrates detail on the nearby joints, while distant slopes need fewer vertices.
            var xs=Axis(-1600,1600,-480,480,2,6);
            var zs=Axis(182.8125f,1860,182.8125f,800,1.5f,6);
            int columns=xs.Count,rows=zs.Count;
            var vertices=new Vector3[columns*rows];
            var colors=new Color[vertices.Length];
            var raised=new bool[vertices.Length];
            var clearance=new float[vertices.Length];
            var indices=new List<int>((columns-1)*(rows-1)*6);
            TerrainData data=terrain.terrainData;
            for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
            {
                float x=xs[col],z=zs[row];
                bool onTerrain=x>=-450 && x<=450 && z<=450;
                float ground=onTerrain?data.GetInterpolatedHeight((x+450)/900,(z+450)/900):18;
                float mountain=18;
                for(int m=0;m<Masses.Length;m++)mountain=Mathf.Max(mountain,GraniteHeight(x,z,Masses[m]));
                mountain=Mathf.Max(mountain,RangeHeight(x,z));
                // No alteration at the playable overlook or along the river approach.
                // A preserved river corridor opens a sightline into the far watershed.
                float north=Mathf.SmoothStep(0,1,Mathf.InverseLerp(210,248,z));
                float height=Mathf.Max(ground,Mathf.Lerp(ground,mountain,north));
                int i=row*columns+col;
                clearance[i]=Mathf.Max(0,height-ground);
                raised[i]=clearance[i]>.01f;
                vertices[i]=new Vector3(x,height+.06f,z);
                float rock=Mathf.SmoothStep(0,1,Mathf.InverseLerp(80,126,ground));
                colors[i]=new Color(1,1,1,onTerrain?north*rock:1);
            }
            // Relief is sampled in three dimensions, including actual elevation. Sampling
            // only x/z stretched every detail into a full-height flute on near-vertical walls.
            // Read the undisplaced surface for normals so this pass cannot accumulate drift.
            var surface=(Vector3[])vertices.Clone();
            for(int row=1;row<rows-1;row++) for(int col=1;col<columns-1;col++)
            {
                int i=row*columns+col;
                if(!raised[i])continue;
                Vector3 alongX=surface[i+1]-surface[i-1];
                Vector3 alongZ=surface[i+columns]-surface[i-columns];
                Vector3 normal=Vector3.Cross(alongZ,alongX).normalized;
                float steep=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,.18f,normal.y));
                Vector3 p=surface[i];
                float n1=Mathf.PerlinNoise(p.x*.043f+p.z*.016f+91,p.y*.039f+37)-.5f;
                float n2=Mathf.PerlinNoise(p.x*.117f+p.z*.031f+17,p.y*.103f+59)-.5f;
                float fracture=Mathf.Abs(Mathf.PerlinNoise(p.x*.069f-p.y*.013f+51,p.y*.059f+p.z*.021f+29)*2-1);
                float relief=n1*7+n2*2.5f-fracture*fracture*3;
                // Keep the entire ground-contact band fixed; moving its edge sideways
                // can expose a gap even when the raised triangles remain opaque.
                float contact=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,16,clearance[i]));
                vertices[i]+=normal*(relief*steep*contact);
            }
            // Raised cells are opaque all the way to their contact vertices. Alpha fades
            // are confined to the rock coating, where actual terrain supports every fragment.
            for(int row=0;row<rows-1;row++) for(int col=0;col<columns-1;col++)
            {
                int i=row*columns+col;
                if(raised[i]||raised[i+1]||raised[i+columns]||raised[i+columns+1])
                    colors[i].a=colors[i+1].a=colors[i+columns].a=colors[i+columns+1].a=1;
            }
            for(int row=0;row<rows-1;row++) for(int col=0;col<columns-1;col++)
            {
                int i=row*columns+col;
                if(Mathf.Max(colors[i].a,Mathf.Max(colors[i+1].a,Mathf.Max(colors[i+columns].a,colors[i+columns+1].a)))<.02f)continue;
                indices.Add(i);indices.Add(i+columns);indices.Add(i+1);
                indices.Add(i+1);indices.Add(i+columns);indices.Add(i+columns+1);
            }
            var mesh=Own(new Mesh { name="Glacial granite valley",indexFormat=IndexFormat.UInt32 });
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=indices.ToArray();
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(true);
            Debug.Log("ALPINE_MESH Glacial granite valley vertices="+vertices.Length+" triangles="+indices.Count/3);
            var material=Own(new Material(source));material.SetFloat("_SnowLine",650);
            var piece=new GameObject("Glacial granite valley",typeof(MeshFilter),typeof(MeshRenderer));
            piece.transform.SetParent(transform,false);
            piece.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=piece.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.On;
            renderer.receiveShadows=true;
            renderer.lightProbeUsage=LightProbeUsage.Off;
            renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
    }
}
