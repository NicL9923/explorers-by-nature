using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    // One authored visual target. Existing ranch authority and terrain heights are unchanged.
    [DefaultExecutionOrder(200)]
    public sealed class ReferenceGrove : MonoBehaviour
    {
        public static ReferenceGrove Current { get; private set; }
        public static bool PhotoMode;
        public bool Ready { get; private set; }
        public int TreeCount { get; private set; }
        readonly List<Object> owned=new List<Object>();
        FirstPersonWalker walker;
        public static bool Contains(Vector3 p,float margin=0) => Mathf.Abs(p.x-ValleyShape.TrailX(p.z))<43+margin && p.z>-193-margin && p.z<-88+margin;
        public static Vector3 Entrance => ValleyShape.Trail(-178,.15f);
        void Start()
        {
            Current=this;walker=FindFirstObjectByType<FirstPersonWalker>();
            var world=FindFirstObjectByType<ValleyWorld>();
            ClearOldScenery(world);DressGround(world.Ground);
            // Offset from the trail, z, scale. Open pockets alternate with close framing trunks.
            Vector3[] trees={
                new Vector3(-6,-177,.92f),new Vector3(8,-182,1.03f),new Vector3(-17,-184,1.15f),new Vector3(22,-185,.86f),
                new Vector3(-9,-160,1.08f),new Vector3(13,-166,.94f),new Vector3(-24,-165,.87f),new Vector3(29,-169,1.12f),
                new Vector3(-5,-145,.82f),new Vector3(10,-148,1.06f),new Vector3(-18,-151,.94f),new Vector3(23,-146,1.15f),
                new Vector3(-12,-131,1.17f),new Vector3(7,-126,.95f),new Vector3(-27,-138,1.1f),new Vector3(28,-133,.91f),
                new Vector3(-6,-114,1.03f),new Vector3(16,-113,1.16f),new Vector3(-21,-115,.91f),new Vector3(31,-115,1.08f),
                new Vector3(-12,-95,1.05f),new Vector3(6,-98,.97f),new Vector3(-30,-99,1.14f),new Vector3(28,-94,1.13f),
                new Vector3(-37,-173,1.1f),new Vector3(-36,-145,1.08f),new Vector3(-39,-118,.97f),new Vector3(39,-159,1.11f),
                new Vector3(42,-127,1.18f),new Vector3(-18,-207,.86f),new Vector3(13,-204,.94f),new Vector3(-2,-83,1.12f)
            };
            for(int i=0;i<trees.Length;i++)
            {
                Vector3 t=trees[i];t.x*=.7f;t.y=-140+(t.y+140)*.8f;var tree=ReferenceTreeArt.Tree(i%3!=0,transform);if(tree==null)continue;
                tree.transform.position=ValleyShape.Ground(ValleyShape.TrailX(t.y)+t.x,t.y,-.09f);
                tree.transform.localScale=Vector3.one*t.z;tree.transform.rotation=Quaternion.Euler((i%3-1)*1.4f,i*137.5f,(i%5-2)*.6f);
                var trunk=tree.AddComponent<CapsuleCollider>();trunk.center=Vector3.up*5;trunk.height=10;trunk.radius=.24f;TreeCount++;
            }
            // Close saplings and irregular background groups enclose a walkable trail.
            var groveRandom=new System.Random(9261);
            for(int i=0;i<76;i++)
            {
                float z=-184+(float)groveRandom.NextDouble()*84;
                bool sapling=i<28;
                float offset=(i%2==0?-1:1)*(sapling?3.2f+(float)groveRandom.NextDouble()*5:8+(float)groveRandom.NextDouble()*20);
                var tree=ReferenceTreeArt.Tree(i%4!=0,transform);if(tree==null)continue;
                tree.transform.position=ValleyShape.Ground(ValleyShape.TrailX(z)+offset,z,-.08f);
                float scale=sapling?.18f+(float)groveRandom.NextDouble()*.18f:1.1f+(float)groveRandom.NextDouble()*.35f;
                tree.transform.localScale=Vector3.one*scale;tree.transform.rotation=Quaternion.Euler(0,i*137.5f,0);
                var trunk=tree.AddComponent<CapsuleCollider>();trunk.center=Vector3.up*5;trunk.height=10;trunk.radius=.24f;TreeCount++;
            }
            var random=new System.Random(8192);
            for(int clump=0;clump<160;clump++)
            {
                float z=Mathf.Lerp(-188,-94,(float)random.NextDouble());
                float side=clump%2==0?-1:1;
                float x=ValleyShape.TrailX(z)+side*Mathf.Lerp(1.9f,clump<120?8:18,(float)random.NextDouble());
                for(int plant=0;plant<8;plant++)
                {
                    float px=x+(float)(random.NextDouble()-.5)*3.2f,pz=z+(float)(random.NextDouble()-.5)*3.2f;
                    if(Mathf.Abs(px-ValleyShape.TrailX(pz))<1.25f)continue;
                    string kind=new[]{"Fern","FernA","FernC","FernD"}[(clump+plant)%4];
                    Place(kind,px,pz,Mathf.Lerp(.85f,1.8f,(float)random.NextDouble()),(float)random.NextDouble()*360,-.025f);
                }
            }
            Place("Stump",ValleyShape.TrailX(-159)-3.2f,-159,1.22f,36,-.08f);
            Place("Stump",ValleyShape.TrailX(-120)+5.2f,-120,.84f,173,-.07f);
            for(int i=0;i<35;i++)
            {
                float z=-186+(float)random.NextDouble()*94,x=ValleyShape.TrailX(z)+(i%2==0?-1:1)*(2.5f+(float)random.NextDouble()*16);
                Place("Rock"+(char)('A'+i%6),x,z,.20f+(float)random.NextDouble()*.46f,i*119,-.08f);
            }
            AddDoe();Ready=true;
        }
        void Place(string kind,float x,float z,float scale,float yaw,float bury)
        {
            var prop=ReferenceGroundArt.Instantiate(kind,transform);if(prop==null)return;
            prop.transform.position=ValleyShape.Ground(x,z,bury);prop.transform.localScale=Vector3.one*scale;
            Vector3 up=new Vector3(ValleyShape.Height(x-.2f,z)-ValleyShape.Height(x+.2f,z),.4f,ValleyShape.Height(x,z-.2f)-ValleyShape.Height(x,z+.2f)).normalized;
            prop.transform.rotation=Quaternion.FromToRotation(Vector3.up,up)*Quaternion.Euler(0,yaw,0);
        }
        void AddDoe()
        {
            var source=Resources.Load<GameObject>("ReferenceDeer/Deer");if(source==null)return;
            var doe=Instantiate(source,transform);doe.name="Fern Hollow doe";
            doe.transform.position=ValleyShape.Ground(ValleyShape.TrailX(-144)-6.2f,-144,.02f);doe.transform.rotation=Quaternion.Euler(0,125,0);
            ModelArt.Remap(doe,"ReferenceDeer/");
            var renderers=doe.GetComponentsInChildren<SkinnedMeshRenderer>();
            System.Array.Sort(renderers,(a,b)=>string.CompareOrdinal(a.name,b.name));
            var levels=new List<LOD>();foreach(var renderer in renderers)levels.Add(new LOD(levels.Count==0?.12f:.00001f,new Renderer[]{renderer}));
            if(levels.Count>0){var lod=doe.GetComponent<LODGroup>()??doe.AddComponent<LODGroup>();lod.SetLODs(levels.ToArray());lod.RecalculateBounds();}
            AnimalMotion.Attach(doe,"Wildlife/Deer");
            var motion=doe.GetComponent<AnimalMotion>();if(motion!=null)motion.allowGrazing=false;
        }
        void ClearOldScenery(ValleyWorld world)
        {
            foreach(Transform group in world.transform)
            {
                if(group.name=="Pine forest" || group.name=="Trailside daisies")
                    foreach(Transform child in group)if(Contains(child.position,10))child.gameObject.SetActive(false);
            }
            var details=FindFirstObjectByType<NatureDetails>();
            foreach(Transform child in details.transform)if((child.name=="Aspen"||child.name=="Cottontail rabbit")&&Contains(child.position,5))child.gameObject.SetActive(false);
            // Hide decorative batches whose centres overlap this authored ground-cover area.
            foreach(var filter in world.GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh!=null && (filter.sharedMesh.name=="Original forest understory" || filter.sharedMesh.name.Contains("Grass")))
                    if(Contains(filter.GetComponent<Renderer>().bounds.center,5))filter.gameObject.SetActive(false);
            int deerIndex=0;
            foreach(var deer in FindObjectsByType<WildlifeRoamer>(FindObjectsSortMode.None))
                if(Contains(deer.transform.position,10)){deer.Relocate(ValleyShape.Ground(-160-deerIndex*8,-200+deerIndex*12));deerIndex++;}
        }
        void DressGround(Terrain terrain)
        {
            var data=terrain.terrainData;int oldCount=data.terrainLayers.Length;
            var layers=new TerrainLayer[oldCount+2];System.Array.Copy(data.terrainLayers,layers,oldCount);
            string[] names={"ForestFloor","TrailGround"};float[] scales={1.5f,3.15f};
            for(int i=0;i<2;i++)
            {
                // Diffuse alpha stores scanned smoothness for Terrain Lit.
                // Keep the dry ground independent of runtime-only mask-map shader variants.
                var layer=new TerrainLayer
                {
                    diffuseTexture=Resources.Load<Texture2D>("ReferenceGround/"+names[i]+"_BaseColor"),
                    normalMapTexture=Resources.Load<Texture2D>("ReferenceGround/"+names[i]+"_Normal"),
                    normalScale=.7f,tileSize=Vector2.one*scales[i],smoothness=.06f
                };owned.Add(layer);layers[oldCount+i]=layer;
            }
            var old=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);data.terrainLayers=layers;
            var map=new float[data.alphamapHeight,data.alphamapWidth,oldCount+2];
            for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++)
            {
                float wx=terrain.transform.position.x+x/(float)(data.alphamapWidth-1)*data.size.x,wz=terrain.transform.position.z+z/(float)(data.alphamapHeight-1)*data.size.z;
                float across=Mathf.Abs(wx-ValleyShape.TrailX(wz));float border=Mathf.Min(32-across,Mathf.Min(wz+193,-88-wz));float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(border/12));
                float tread=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,2.4f,across+(Mathf.PerlinNoise(wx*.8f,wz*.3f)-.5f)*.6f));
                for(int n=0;n<oldCount;n++)map[z,x,n]=old[z,x,n]*(1-blend);
                map[z,x,oldCount]=blend*(1-tread);map[z,x,oldCount+1]=blend*tread;
            }
            data.SetAlphamaps(0,0,map);
        }
        public void Visit()
        {
            FindFirstObjectByType<RanchComfort>()?.Stand();
            walker.Teleport(Entrance);walker.transform.rotation=Quaternion.Euler(0,0,0);walker.view.transform.localRotation=Quaternion.identity;walker.SyncLookPitch();walker.SetMenu(false);
        }
        void Update(){if(!walker.Automated&&Input.GetKeyDown(KeyCode.F8))Visit();if(!walker.Automated&&Input.GetKeyDown(KeyCode.F9))PhotoMode=!PhotoMode;}
        void OnDestroy(){if(Current==this)Current=null;PhotoMode=false;foreach(var asset in owned)Destroy(asset);}
    }
}
