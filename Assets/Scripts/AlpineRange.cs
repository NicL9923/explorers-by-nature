using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    // Visual geology only. The terrain collider and the shared height function remain authoritative.
    public sealed class AlpineRange : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        void OnDestroy() { foreach (var value in owned) if (value != null) Destroy(value); }

        public void Build(Terrain terrain)
        {
            var source = Resources.Load<Material>("AlpineRange/Mountain");
            if (source == null) { Debug.LogError("Missing alpine rock material"); return; }
            BuildRange(source, terrain);
            BuildFaces(source, terrain);
        }

        // A transverse watershed, with unequal saddles and broken, angular summits. Each
        // point is x, crest height, z. Broad buttresses descend from this connected spine.
        static readonly Vector3[] Crest = {
            new Vector3(-1400,130,1000), new Vector3(-1180,325,860),
            new Vector3(-1010,255,890), new Vector3(-845,470,910),
            new Vector3(-720,370,940), new Vector3(-565,535,1010),
            new Vector3(-460,420,975), new Vector3(-325,560,1030),
            new Vector3(-195,455,1080), new Vector3(-55,645,1110),
            new Vector3(65,475,1030), new Vector3(225,540,1070),
            new Vector3(345,385,970), new Vector3(520,605,1110),
            new Vector3(690,435,1030), new Vector3(855,510,1120),
            new Vector3(1030,345,1060), new Vector3(1210,405,1140),
            new Vector3(1400,130,1120)
        };

        static float RangeHeight(float x, float z)
        {
            int segment=0;
            while (segment<Crest.Length-2 && x>Crest[segment+1].x) segment++;
            Vector3 a=Crest[segment], b=Crest[segment+1];
            float t=Mathf.InverseLerp(a.x,b.x,x);
            float crestZ=Mathf.Lerp(a.z,b.z,t);
            float height=Mathf.Lerp(a.y,b.y,t);
            float dz=z-crestZ;
            // Unequal front and back escarpments keep the skyline out of a cone profile.
            float shoulder=height-Mathf.Abs(dz)*(dz<0?.77f:1.24f);
            float flank=Mathf.Clamp01(Mathf.Abs(dz)/210);
            float warp=(Mathf.PerlinNoise(x*.006f+19,z*.007f+71)-.5f)*45;
            float drainage=Mathf.Abs(Mathf.Sin((x+dz*.34f+warp)*.032f));
            float fine=Mathf.PerlinNoise(x*.027f+128,z*.023f+92)-.5f;
            shoulder-=drainage*drainage*flank*42;
            shoulder+=fine*13*Mathf.Clamp01(shoulder/120);
            // A low intervening ridge gives the farther crest a second visible horizon.
            float nearCrest=152+Mathf.Abs(Mathf.Sin(x*.0071f+.4f))*112;
            float near=nearCrest-Mathf.Abs(z-(615+Mathf.Sin(x*.0038f)*42))*.95f;
            return Mathf.Max(22,Mathf.Max(shoulder,near));
        }

        void BuildRange(Material source, Terrain terrain)
        {
            const int columns=321, rows=145;
            var vertices=new Vector3[columns*rows];
            var colors=new Color[vertices.Length];
            var indices=new int[(columns-1)*(rows-1)*6];
            int triangle=0;
            for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
            {
                float x=Mathf.Lerp(-1400,1400,col/(float)(columns-1));
                float z=Mathf.Lerp(450,1470,row/(float)(rows-1));
                float blend=Mathf.SmoothStep(0,1,(z-450)/110);
                float edge=Mathf.Abs(x)<=450 ? terrain.SampleHeight(new Vector3(x,0,449.9f)) : 0;
                int index=row*columns+col;
                vertices[index]=new Vector3(x,Mathf.Lerp(edge,RangeHeight(x,z),blend),z);
                colors[index]=new Color(1,1,1,1);
                if(row==rows-1 || col==columns-1) continue;
                indices[triangle++]=index; indices[triangle++]=index+columns; indices[triangle++]=index+1;
                indices[triangle++]=index+1; indices[triangle++]=index+columns; indices[triangle++]=index+columns+1;
            }
            Render("Connected alpine escarpments",vertices,colors,indices,source,385);
        }

        void BuildFaces(Material source, Terrain terrain)
        {
            // Sample the actual rendered heightfield, not a second analytic approximation.
            // The 5 cm normal lift prevents z fighting and leaves gameplay geometry untouched.
            const int columns=513, rows=153;
            var vertices=new Vector3[columns*rows];
            var colors=new Color[vertices.Length];
            var indices=new List<int>((columns-1)*(rows-1)*6);
            TerrainData data=terrain.terrainData;
            for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
            {
                float x=Mathf.Lerp(-450,450,col/(float)(columns-1));
                float z=Mathf.Lerp(182.8125f,450,row/(float)(rows-1));
                float u=(x+450)/900,v=(z+450)/900;
                float h=data.GetInterpolatedHeight(u,v);
                Vector3 normal=data.GetInterpolatedNormal(u,v);
                float north=Mathf.SmoothStep(0,1,Mathf.InverseLerp(195,235,z));
                float high=Mathf.SmoothStep(0,1,Mathf.InverseLerp(98,145,h));
                int index=row*columns+col;
                vertices[index]=new Vector3(x,h,z)+normal*.05f;
                colors[index]=new Color(1,1,1,north*high);
            }
            for(int row=0;row<rows-1;row++) for(int col=0;col<columns-1;col++)
            {
                int i=row*columns+col;
                if(Mathf.Max(colors[i].a,Mathf.Max(colors[i+1].a,Mathf.Max(colors[i+columns].a,colors[i+columns+1].a)))<.02f)continue;
                indices.Add(i);indices.Add(i+columns);indices.Add(i+1);
                indices.Add(i+1);indices.Add(i+columns);indices.Add(i+columns+1);
            }
            Render("Northern granite faces",vertices,colors,indices.ToArray(),source,228);
        }

        void Render(string name,Vector3[] vertices,Color[] colors,int[] triangles,Material source,float snowLine)
        {
            var mesh=Own(new Mesh { name=name,indexFormat=IndexFormat.UInt32 });
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(true);
            Debug.Log("ALPINE_MESH "+name+" vertices="+vertices.Length+" triangles="+triangles.Length/3);
            var material=Own(new Material(source));material.SetFloat("_SnowLine",snowLine);
            var piece=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            piece.transform.SetParent(transform,false);
            piece.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=piece.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;
            renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
    }
}
