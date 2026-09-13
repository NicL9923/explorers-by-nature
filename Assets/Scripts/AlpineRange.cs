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
            // Nested drainage roughness gives buttresses irregular edges instead of broad triangles.
            float ridge = 1 - Mathf.Abs(Mathf.PerlinNoise(x*.012f+42,z*.016f+17)*2-1);
            float scree = Mathf.PerlinNoise(x*.071f+63,z*.066f+39)-.5f;
            shoulder+=(fine*19 + (ridge-.65f)*24*flank + scree*4)*Mathf.Clamp01(shoulder/120);
            // A low intervening ridge gives the farther crest a second visible horizon.
            float nearCrest=152+Mathf.Abs(Mathf.Sin(x*.0071f+.4f))*112;
            float near=nearCrest-Mathf.Abs(z-(615+Mathf.Sin(x*.0038f)*42))*.95f;
            return Mathf.Max(22,Mathf.Max(shoulder,near));
        }

        void BuildRange(Material source, Terrain terrain)
        {
            const int columns=561, rows=205;
            var vertices=new Vector3[columns*rows];
            var colors=new Color[vertices.Length];
            var indices=new int[(columns-1)*(rows-1)*6];
            int triangle=0;
            TerrainData data=terrain.terrainData;
            Vector3 summitHeights=new Vector3(data.GetInterpolatedHeight(245f/900,805f/900),
                data.GetInterpolatedHeight(605f/900,835f/900),data.GetInterpolatedHeight(805f/900,750f/900));
            for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
            {
                float x=Mathf.Lerp(-1400,1400,col/(float)(columns-1));
                float z=Mathf.Lerp(450,1470,row/(float)(rows-1));
                float blend=Mathf.SmoothStep(0,1,(z-450)/110);
                float edge=Mathf.Abs(x)<=450
                    ? RockEnvelope(x,449.9f,terrain.SampleHeight(new Vector3(x,0,449.9f)),summitHeights) : 0;
                int index=row*columns+col;
                vertices[index]=new Vector3(x,Mathf.Lerp(edge,RangeHeight(x,z),blend),z);
                colors[index]=new Color(1,1,1,1);
                if(row==rows-1 || col==columns-1) continue;
                indices[triangle++]=index; indices[triangle++]=index+columns; indices[triangle++]=index+1;
                indices[triangle++]=index+1; indices[triangle++]=index+columns; indices[triangle++]=index+columns+1;
            }
            Render("Connected alpine escarpments",vertices,colors,indices,source,385);
        }

        // Linked summits and asymmetric rock walls replace the rounded upper envelope.
        // Heights are relative to each massif's actual summit. These are complete watershed
        // sections, not separate rocks pasted onto a smooth cone.
        static readonly Vector3[] SummitProfile = {
            new Vector3(-1,-76,-.06f), new Vector3(-.77f,-18,.07f),
            new Vector3(-.57f,27,.03f), new Vector3(-.36f,4,-.09f),
            new Vector3(-.08f,53,.04f), new Vector3(.12f,24,.11f),
            new Vector3(.34f,39,.02f), new Vector3(.55f,-4,-.12f),
            new Vector3(.76f,17,-.04f), new Vector3(1,-88,.10f)
        };

        static float RockEnvelope(float x,float z,float ground,Vector3 summitHeights)
        {
            float envelope=ground;
            envelope=Mathf.Max(envelope,Escarpment(x,z,-205,355,118,1.13f,1.78f,summitHeights.x));
            envelope=Mathf.Max(envelope,Escarpment(x,z,155,385,127,1.48f,2.1f,summitHeights.y));
            envelope=Mathf.Max(envelope,Escarpment(x,z,355,300,103,1.24f,1.9f,summitHeights.z));
            // Keep all ranch/river/overlook ground exactly where the authority places it.
            // Upper rock faces remain scenery; no new routes run through this envelope.
            float north=Mathf.SmoothStep(0,1,Mathf.InverseLerp(245,290,z));
            float altitude=Mathf.SmoothStep(0,1,Mathf.InverseLerp(112,162,ground));
            return Mathf.Lerp(ground,envelope,north*altitude);
        }

        static float Escarpment(float x,float z,float cx,float cz,float width,float frontSlope,float backSlope,float summit)
        {
            float u=(x-cx)/width;
            if(u<=-1 || u>=1)return 0;
            int section=0;
            while(section<SummitProfile.Length-2 && u>SummitProfile[section+1].x)section++;
            Vector3 a=SummitProfile[section],b=SummitProfile[section+1];
            float t=Mathf.InverseLerp(a.x,b.x,u);
            float crest=summit+Mathf.Lerp(a.y,b.y,t);
            float ridgeZ=cz+Mathf.Lerp(a.z,b.z,t)*width;
            float distance=z-ridgeZ;
            float slope=distance<0?frontSlope:backSlope;
            // A steep headwall breaks onto a shallow talus apron. Unequal facets produce
            // readable planes and saddles at skyline scale, rather than smooth radial peaks.
            float wall=crest-Mathf.Abs(distance)*slope;
            float apron=crest-48-Mathf.Abs(distance)*slope*.66f;
            float face=Mathf.Max(wall,apron);
            // Continuous weathering breaks up each plane without a repeated sawtooth in
            // height. Periodic bedding made the silhouette look like stacked terraces.
            float weathering=(Mathf.PerlinNoise(x*.055f+31,z*.041f+73)-.5f)*2.8f;
            weathering+=(Mathf.PerlinNoise(x*.117f+17,z*.093f+29)-.5f)*.65f;
            return (face+weathering*Mathf.Clamp01(Mathf.Abs(distance)/30))
                *Mathf.SmoothStep(0,1,Mathf.Clamp01((1-Mathf.Abs(u))*6));
        }

        void BuildFaces(Material source, Terrain terrain)
        {
            // Sample the actual rendered heightfield, not a second analytic approximation.
            // Connected upper escarpments change the skyline; their feet meet the sampled terrain.
            // The 5 cm normal lift prevents z fighting where the surfaces coincide.
            const int columns=513, rows=153;
            var vertices=new Vector3[columns*rows];
            var colors=new Color[vertices.Length];
            var raised=new bool[vertices.Length];
            var indices=new List<int>((columns-1)*(rows-1)*6);
            TerrainData data=terrain.terrainData;
            Vector3 summitHeights=new Vector3(data.GetInterpolatedHeight(245f/900,805f/900),
                data.GetInterpolatedHeight(605f/900,835f/900),data.GetInterpolatedHeight(805f/900,750f/900));
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
                float rockHeight=RockEnvelope(x,z,h,summitHeights);
                raised[index]=rockHeight>h+.001f;
                vertices[index]=new Vector3(x,rockHeight,z)+normal*.05f;
                colors[index]=new Color(1,1,1,north*high);
            }
            // Dithered coverage is safe only where the rock is a terrain-conforming coat.
            // Escarpments have empty space below them: clipping their foot exposes that
            // space as a hollow arch. Make every vertex of a raised cell opaque, including
            // its ground-contact vertices, so interpolated alpha cannot reopen the seam.
            for(int row=0;row<rows-1;row++) for(int col=0;col<columns-1;col++)
            {
                int i=row*columns+col;
                if(!raised[i] && !raised[i+1] && !raised[i+columns] && !raised[i+columns+1])continue;
                colors[i].a=colors[i+1].a=colors[i+columns].a=colors[i+columns+1].a=1;
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
            renderer.shadowCastingMode=ShadowCastingMode.On;
            renderer.receiveShadows=true;
            renderer.lightProbeUsage=LightProbeUsage.Off;
            renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
    }
}
