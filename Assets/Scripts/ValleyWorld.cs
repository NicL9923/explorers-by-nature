using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    [DefaultExecutionOrder(-50)]
    public sealed class ValleyWorld : MonoBehaviour
    {
        public Texture2D grassTexture;
        public Texture2D earthTexture;
        public Texture2D rockTexture;
        public Material terrainMaterial;
        public Material grassMaterial;
        public Material waterMaterial;
        public Material barkMaterial;
        public Material leavesMaterial;
        public Material rockMaterial;
        public Material deerMaterial;
        public Mesh[] stoneMeshes;
        public FirstPersonWalker walker;
        public Terrain Ground { get; private set; }
        public GameObject DenseGrass { get; private set; }
        public bool Ready { get; private set; }
        readonly List<Object> owned = new List<Object>();

        void Awake()
        {
            BuildTerrain();
            BuildBackdrop();
            BuildRiver();
            BuildForest();
            BuildGrass();
            BuildFlowerDrifts();
            BuildWildlife();
            Physics.SyncTransforms();
            walker.Teleport(ValleyShape.Spawn);
            Ready = true;
        }

        T Own<T>(T asset) where T : Object { owned.Add(asset); return asset; }
        void OnDestroy() { foreach (Object asset in owned) if (asset != null) Destroy(asset); }

        void BuildTerrain()
        {
            const int resolution = 513;
            TerrainData data = Own(new TerrainData { heightmapResolution = resolution, size = new Vector3(900, 360, 900), alphamapResolution = 256 });
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                    heights[z, x] = ValleyShape.Height(x * 900f / (resolution - 1) - 450, z * 900f / (resolution - 1) - 450) / 360;
            data.SetHeights(0, 0, heights);
            data.terrainLayers = new[] { Layer(MeadowTexture(), 14), Layer(earthTexture, 5), Layer(rockTexture, 12), Layer(SnowTexture(), 20) };
            var splat = new float[256, 256, 4];
            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                {
                    float wx = x / 255f * 900 - 450, wz = z / 255f * 900 - 450;
                    float slope = data.GetSteepness(x / 255f, z / 255f);
                    float stone = Mathf.Clamp01((slope - 23) / 22 + Mathf.Max(0, ValleyShape.Height(wx, wz) - 110) / 90);
                    float path = (1 - SmoothRange(1.5f, 5f, Mathf.Abs(wx - ValleyShape.TrailX(wz))))
                        * (1 - SmoothRange(140, 160, wz)) * SmoothRange(-280, -250, wz);
                    float shore = 1 - SmoothRange(12, 23, Mathf.Abs(wx - ValleyShape.RiverX(wz)));
                    float earth = Mathf.Max(path, shore) * (1 - stone);
                    float snow = SmoothRange(170, 220, ValleyShape.Height(wx,wz) + Mathf.PerlinNoise(wx*.035f,wz*.035f)*30) * (1-SmoothRange(38,60,slope));
                    splat[z, x, 0] = (1 - stone - earth) * (1-snow);
                    splat[z, x, 1] = earth * (1-snow);
                    splat[z, x, 2] = stone * (1-snow);
                    splat[z, x, 3] = snow;
                }
            data.SetAlphamaps(0, 0, splat);
            GameObject terrain = Terrain.CreateTerrainGameObject(data);
            terrain.name = "Meadow, forest and mountain terrain";
            terrain.transform.SetParent(transform);
            terrain.transform.position = new Vector3(-450, 0, -450);
            Ground = terrain.GetComponent<Terrain>();
            Ground.materialTemplate = terrainMaterial;
            Ground.drawInstanced = true;
            Ground.heightmapPixelError = 6;
            Ground.basemapDistance = 500;
        }

        TerrainLayer Layer(Texture2D texture, float scale) => Own(new TerrainLayer { diffuseTexture = texture, tileSize = Vector2.one * scale, metallic = 0, smoothness = .05f });

        Texture2D SnowTexture()
        {
            var texture=Own(new Texture2D(32,32,TextureFormat.RGB24,true){name="Alpine snow"});
            var pixels=new Color[1024];
            for(int i=0;i<pixels.Length;i++) pixels[i]=Color.Lerp(new Color(.76f,.82f,.84f),new Color(.93f,.94f,.90f),Mathf.PerlinNoise(i%32*.4f,i/32*.4f));
            texture.SetPixels(pixels);texture.Apply(true,true);return texture;
        }

        Texture2D MeadowTexture()
        {
            var texture = Own(new Texture2D(256, 256, TextureFormat.RGB24, true) { name = "Living meadow", wrapMode = TextureWrapMode.Repeat });
            var pixels = new Color[256 * 256];
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            {
                // Periodic waves keep the broad meadow variation seamless at tile boundaries.
                float broad = .5f + .22f * Mathf.Sin(x * Mathf.PI / 128) * Mathf.Cos(y * Mathf.PI / 64);
                float grain = Mathf.PerlinNoise(x * .38f, y * .38f);
                pixels[y * 256 + x] = Color.Lerp(new Color(.34f,.43f,.16f), new Color(.53f,.60f,.29f), broad * .55f + grain * .45f);
            }
            texture.SetPixels(pixels); texture.Apply(true, true); return texture;
        }

        void BuildBackdrop()
        {
            // A sculpted mesh beyond the playable heightfield leaves shared ground heights unchanged.
            const int columns=121, rows=65;
            var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            var summits=new[]{new Vector3(-530,330,780),new Vector3(-160,405,940),new Vector3(230,375,810),new Vector3(650,420,1020)};
            for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
            {
                float x=-1100+col*2200f/(columns-1), z=450+row*950f/(rows-1);
                float distance=z-450;
                float peak=0;
                foreach(Vector3 summit in summits)
                {
                    float dx=(x-summit.x)/150, dz=(z-summit.z)/180;
                    peak=Mathf.Max(peak,summit.y*Mathf.Exp(-.5f*(dx*dx+dz*dz)));
                }
                float detail=(1-Mathf.Abs(Mathf.PerlinNoise((x+1900)*.012f,z*.014f)*2-1))*36;
                float height=(peak+detail)*SmoothRange(0,110,distance);
                if(distance<110 && Mathf.Abs(x)<450) height+=ValleyShape.Height(x,450)*(1-SmoothRange(0,110,distance));
                vertices.Add(new Vector3(x,height,z));
                float snow=SmoothRange(285,345,height+Mathf.PerlinNoise(x*.03f,z*.03f)*30);
                colors.Add(Color.Lerp(new Color(.32f,.37f,.36f),new Color(.91f,.92f,.89f),snow).linear);
                if(row==rows-1||col==columns-1)continue;
                int n=row*columns+col;indices.AddRange(new[]{n,n+columns,n+1,n+1,n+columns,n+columns+1});
            }
            var mesh=Own(new Mesh{name="Sculpted alpine horizon"});mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var material=Own(new Material(grassMaterial));material.SetFloat("_WindStrength",0);material.SetFloat("_SurfaceLighting",1);
            MeshObject("Distant alpine range",mesh,material,transform).shadowCastingMode=ShadowCastingMode.Off;
        }

        void BuildRiver()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();
            for (int i = 0; i <= 180; i++)
            {
                float z = -450 + i * 5;
                float center = ValleyShape.RiverX(z);
                vertices.Add(new Vector3(center - 12, ValleyShape.WaterHeight, z));
                vertices.Add(new Vector3(center + 12, ValleyShape.WaterHeight, z));
                uv.Add(new Vector2(0, i * .25f)); uv.Add(new Vector2(1, i * .25f));
                if (i == 180) continue;
                int n = i * 2;
                triangles.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 });
            }
            Mesh river = Own(new Mesh { name = "Winding river" });
            river.SetVertices(vertices); river.SetTriangles(triangles, 0); river.SetUVs(0, uv); river.RecalculateNormals();
            MeshObject("River", river, waterMaterial, transform).shadowCastingMode = ShadowCastingMode.Off;
        }

        void BuildForest()
        {
            var rng = new System.Random(1848);
            Mesh pine = Own(PineMesh(9, 7));
            Mesh distantPine = Own(PineMesh(5, 4));
            var forest = new GameObject("Pine forest").transform;
            forest.SetParent(transform);
            for (int i = 0; i < 1000; i++)
            {
                float x = Range(rng, -340, 340), z = Range(rng, -320, 330);
                if (Mathf.Abs(x - ValleyShape.RiverX(z)) < 28 || Mathf.Abs(x - ValleyShape.TrailX(z)) < 8) continue;
                if (z < -180 && x > -130 && x < 0) continue; // Arrival meadow.
                if (ValleyShape.Height(x, z) > 140) continue;
                if (Mathf.PerlinNoise((x+600)*.015f,(z+400)*.015f) < .38f) continue;
                float slope = Mathf.Abs(ValleyShape.Height(x + 2, z) - ValleyShape.Height(x - 2, z));
                if (slope > 5) continue;
                var tree = new GameObject("Pine").transform;
                tree.SetParent(forest); tree.position = ValleyShape.Ground(x, z, -.2f);
                float height = Range(rng, 11, 23);
                tree.localScale = new Vector3(height * Range(rng, .85f, 1.15f), height, height);
                tree.Rotate(0, Range(rng, 0, 360), 0);
                GameObject importedTree = ModelArt.Tree("Woodland/Pine", tree);
                if (importedTree != null)
                {
                    // Source trees use metre units; the parent is normalized to a one-metre tree.
                    float sourceHeight = ModelHeight(importedTree);
                    importedTree.transform.localScale = Vector3.one / Mathf.Max(1, sourceHeight);
                    var trunkShape = tree.gameObject.AddComponent<CapsuleCollider>();
                    trunkShape.center = new Vector3(0, .3f, 0); trunkShape.height = .6f; trunkShape.radius = .018f;
                    continue;
                }
                MeshRenderer near = MeshObject("Branches", pine, leavesMaterial, tree);
                MeshRenderer far = MeshObject("Distant branches", distantPine, leavesMaterial, tree);
                GameObject trunk = Primitive("Trunk", PrimitiveType.Cylinder, tree, new Vector3(0, .3f, 0), new Vector3(.045f, .3f, .045f), barkMaterial, false);
                CapsuleCollider collider = trunk.AddComponent<CapsuleCollider>();
                collider.radius = .5f; collider.height = 2;
                LODGroup lod = tree.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(.09f, new Renderer[] { near, trunk.GetComponent<Renderer>() }), new LOD(.008f, new Renderer[] { far }) });
                lod.RecalculateBounds();
            }
            for (int i = 0; i < 130; i++)
            {
                float z = Range(rng, -310, 300), x = ValleyShape.RiverX(z) + (i % 2 == 0 ? -1 : 1) * Range(rng, 16, 24);
                MeshRenderer rock = MeshObject("River stone", stoneMeshes[i % stoneMeshes.Length], rockMaterial, transform);
                rock.transform.localPosition = ValleyShape.Ground(x, z);
                rock.transform.localScale = new Vector3(Range(rng, 1, 3), Range(rng, 1, 2), Range(rng, 1, 3));
                rock.transform.Rotate(Range(rng, 0, 20), Range(rng, 0, 360), 0);
            }
        }

        public static float ModelHeight(GameObject model)
        {
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
            {
                Bounds bounds = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    float y = model.transform.InverseTransformPoint(filter.transform.TransformPoint(point)).y;
                    min = Mathf.Min(min, y); max = Mathf.Max(max, y);
                }
            }
            return max - min;
        }

        static Mesh PineMesh(int sides, int tiers)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int tier = 0; tier < tiers; tier++)
            {
                float t = tier / (float)tiers;
                float bottom = .2f + t * .65f, radius = .19f * (1 - t * .85f);
                for (int s = 0; s < sides; s++)
                {
                    float a = s * Mathf.PI * 2 / sides, b = (s + 1) * Mathf.PI * 2 / sides;
                    int n = vertices.Count;
                    vertices.Add(new Vector3(Mathf.Cos(a) * radius, bottom + Mathf.Sin(s * 7 + tier) * .025f, Mathf.Sin(a) * radius));
                    vertices.Add(new Vector3(0, Mathf.Min(1, bottom + .3f), 0));
                    vertices.Add(new Vector3(Mathf.Cos(b) * radius, bottom + Mathf.Sin((s + 1) * 7 + tier) * .025f, Mathf.Sin(b) * radius));
                    triangles.AddRange(new[] { n, n + 1, n + 2 });
                }
            }
            var mesh = new Mesh { name = "Original pine placeholder" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        void BuildGrass()
        {
            DenseGrass = new GameObject("High quality ground cover");
            DenseGrass.transform.SetParent(transform);
            var meadowGrass=new GameObject("Meadow ground cover");meadowGrass.transform.SetParent(transform);
            var rng = new System.Random(371);
            for(int layer=0;layer<2;layer++)
            for (int z = -260; z < 165; z += 24)
                for (int x = -160; x < 10; x += 24)
                {
                    var verts = new List<Vector3>(); var colors = new List<Color>(); var indices = new List<int>(); var bladeUV = new List<Vector2>();
                    for (int b = 0; b < (layer==0?950:2200); b++)
                    {
                        float wx = x + Range(rng, 0, 24), wz = z + Range(rng, 0, 24);
                        if (Mathf.Abs(wx - ValleyShape.TrailX(wz)) < 3 || Mathf.Abs(wx - ValleyShape.RiverX(wz)) < 23) continue;
                        Vector3 p = ValleyShape.Ground(wx, wz);
                        float h = Range(rng, .13f, .36f), angle = Range(rng, 0, Mathf.PI * 2);
                        Vector3 side = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * .012f;
                        Color color = Color.Lerp(new Color(.24f, .39f, .105f), new Color(.46f, .57f, .21f), Range(rng, 0, 1));
                        int n = verts.Count;
                        Vector3 bend = new Vector3(-side.z, 0, side.x) * 6;
                        verts.Add(p - side); verts.Add(p + side);
                        verts.Add(p + Vector3.up * h * .55f + bend * .25f - side * .55f);
                        verts.Add(p + Vector3.up * h * .55f + bend * .25f + side * .55f);
                        verts.Add(p + Vector3.up * h + bend);
                        colors.Add((color * .6f).linear); colors.Add((color * .6f).linear);
                        colors.Add(color.linear); colors.Add(color.linear); colors.Add((color * 1.04f).linear);
                        bladeUV.Add(Vector2.zero); bladeUV.Add(Vector2.right);
                        bladeUV.Add(new Vector2(0, .55f)); bladeUV.Add(new Vector2(1, .55f)); bladeUV.Add(new Vector2(.5f, 1));
                        indices.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3, n + 2, n + 4, n + 3 });
                    }
                    if (verts.Count == 0) continue;
                    Mesh mesh = Own(new Mesh { name = "Grass patch" });
                    mesh.SetVertices(verts); mesh.SetTriangles(indices, 0); mesh.SetColors(colors); mesh.SetUVs(0, bladeUV); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    MeshRenderer renderer = MeshObject("Grass", mesh, grassMaterial, layer==0?meadowGrass.transform:DenseGrass.transform);
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    LODGroup lod = renderer.gameObject.AddComponent<LODGroup>();
                    lod.SetLODs(new[] { new LOD(.07f, new Renderer[] { renderer }) }); lod.RecalculateBounds();
                }
        }

        void BuildFlowerDrifts()
        {
            var root = new GameObject("Trailside daisies").transform; root.SetParent(transform);
            var random = new System.Random(714);
            for (int i=0; i<85; i++)
            {
                float z=Range(random,-260,115), x=ValleyShape.TrailX(z)+(i%2==0?-1:1)*Range(random,5,21);
                if(i<25) { x=Range(random,-135,-75);z=Range(random,-245,-175); }
                if (Mathf.Abs(x-ValleyShape.RiverX(z))<25) continue;
                var flowers=ModelArt.Tree("Woodland/Wildflower",root);
                if (flowers==null) break;
                flowers.transform.position=ValleyShape.Ground(x,z,.02f);
                flowers.transform.localScale=Vector3.one*Range(random,.8f,1.45f);
                flowers.transform.Rotate(0,Range(random,0,360),0);
            }
        }

        void BuildWildlife()
        {
            for (int i = 0; i < 4; i++)
            {
                var deer = new GameObject("Deer placeholder").transform;
                deer.SetParent(transform); deer.position = ValleyShape.Ground(-100 + i * 9, -196 + i * 13);
                GameObject detailedDeer = ModelArt.Instantiate("Wildlife/Deer", deer, false);
                if (detailedDeer != null)
                {
                    var roaming = deer.gameObject.AddComponent<WildlifeRoamer>();
                    roaming.legs = new Transform[0]; roaming.observer = walker.transform;
                    continue;
                }
                Primitive("Body", PrimitiveType.Capsule, deer, new Vector3(0, 1, 0), new Vector3(.55f, .65f, .6f), deerMaterial, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
                Primitive("Neck", PrimitiveType.Capsule, deer, new Vector3(0, 1.35f, .48f), new Vector3(.26f, .45f, .3f), deerMaterial, false).transform.localRotation = Quaternion.Euler(25, 0, 0);
                Primitive("Head", PrimitiveType.Capsule, deer, new Vector3(0, 1.8f, .68f), new Vector3(.25f, .26f, .28f), deerMaterial, false).transform.localRotation = Quaternion.Euler(65, 0, 0);
                var legs = new Transform[4];
                for (int leg = 0; leg < 4; leg++)
                    legs[leg] = Primitive("Leg", PrimitiveType.Capsule, deer, new Vector3(leg % 2 == 0 ? -.18f : .18f, .46f, leg < 2 ? -.4f : .4f), new Vector3(.09f, .43f, .09f), deerMaterial, false).transform;
                for (int ear = 0; ear < 2; ear++)
                    Primitive("Ear", PrimitiveType.Sphere, deer, new Vector3(ear == 0 ? -.18f : .18f, 2, .6f), new Vector3(.1f, .3f, .13f), deerMaterial, false);
                WildlifeRoamer roamer = deer.gameObject.AddComponent<WildlifeRoamer>();
                roamer.legs = legs; roamer.observer = walker.transform;
            }
        }

        static float SmoothRange(float min, float max, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(min, max, value));

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        static MeshRenderer MeshObject(string name, Mesh mesh, Material material, Transform parent)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false); go.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            return renderer;
        }

        static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collision)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) { Collider collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider); }
            return go;
        }
    }
}
