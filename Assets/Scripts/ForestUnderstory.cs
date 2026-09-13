using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    // Original, deterministically grown plant meshes. One renderer per spatial cell and detail layer.
    // The base layer remains on low quality; additional growth follows ValleyWorld.DenseGrass.
    public sealed class ForestUnderstory : MonoBehaviour
    {
        readonly List<Mesh> meshes = new List<Mesh>();
        public int PlantCount { get; private set; }
        public void Grow(Transform highDetail, Material material)
        {
            var random = new System.Random(63021);
            for (int layer = 0; layer < 2; layer++)
                for (int z = -266; z < 154; z += 28)
                    for (int x = -182; x < (layer==0?42:182); x += 28)
                    {
                        var patch = new Geometry();
                        Vector3 origin = ValleyShape.Ground(x + 14, z + 14);
                        for (int i = 0; i < (layer == 0 ? 22 : 76); i++)
                        {
                            float wx = x + R(random, 0, 28), wz = z + R(random, 0, 28);
                            // Concentrate half of the extra layer at the walking route's edges.
                            // Keep cell bounds valid so each garden still culls independently.
                            if (layer == 1 && i < 21)
                            {
                                float edge = ValleyShape.TrailX(wz) + (i % 2 == 0 ? -1 : 1) * R(random, 4.2f, 16);
                                if (edge >= x && edge < x + 28) wx = edge;
                            }
                            float trail = Mathf.Abs(wx - ValleyShape.TrailX(wz));
                            if (trail < 3.8f || (layer==0?Mathf.Abs(wx-ValleyShape.RiverX(wz))<24:RiverDynamics.DepthAt(wx,wz)>.01f)) continue;
                            if (ValleyShape.Height(wx, wz) > 108 || Mathf.Abs(ValleyShape.Height(wx + 1, wz) - ValleyShape.Height(wx - 1, wz)) > 2.1f) continue;
                            // Leave the actual homestead and expedition interactions open.
                            if ((new Vector2(wx + 98, wz + 232)).sqrMagnitude < 18 * 18) continue;
                            float patchiness = Mathf.PerlinNoise((wx + 640) * .047f, (wz + 410) * .047f);
                            if (patchiness < .25f || (trail > 35 && Mathf.Abs(wx-ValleyShape.RiverX(wz))>55 && R(random, 0, 1) > .38f)) continue;
                            Vector3 p = ValleyShape.Ground(wx, wz, .015f) - origin;
                            float angle = R(random, 0, Mathf.PI * 2), scale = R(random, .8f, 1.65f);
                            int kind = random.Next(10);
                            if (kind < 4 && wz > -185) Fern(patch, p, angle, scale, random);
                            else if (kind < 7) Shrub(patch, p, angle, scale, random, kind == 6);
                            else Flowers(patch, p, angle, scale, random);
                            PlantCount++;
                        }
                        if (layer == 0 && z > -182 && random.NextDouble() < .19)
                        {
                            float wx = x + 14, wz = z + 14;
                            if (Mathf.Abs(wx - ValleyShape.TrailX(wz)) > 7 && Mathf.Abs(wx - ValleyShape.RiverX(wz)) > 28)
                                FallenLog(patch, ValleyShape.Ground(wx, wz, .15f) - origin, R(random, 0, 6.28f), random);
                        }
                        if (patch.vertices.Count == 0) continue;
                        var mesh = patch.Mesh(); meshes.Add(mesh);
                        var cell = new GameObject(layer == 0 ? "Forest floor garden" : "Lush forest floor", typeof(MeshFilter), typeof(MeshRenderer), typeof(LODGroup));
                        cell.transform.SetParent(layer == 0 ? transform : highDetail, false); cell.transform.position = origin;
                        cell.GetComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = cell.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                        renderer.shadowCastingMode = ShadowCastingMode.On;
                        var lod = cell.GetComponent<LODGroup>();
                        lod.SetLODs(new[] { new LOD(layer == 0 ? .095f : .14f, new Renderer[] { renderer }) }); lod.RecalculateBounds();
                    }
        }

        static float R(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());
        static Vector3 Direction(float a) => new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
        static void Fern(Geometry g, Vector3 p, float angle, float scale, System.Random r)
        {
            Color green = Color.Lerp(new Color(.20f, .38f, .12f), new Color(.39f, .56f, .20f), R(r, 0, 1));
            for (int frond = 0; frond < 7; frond++)
            {
                Vector3 direction = Direction(angle + frond * 6.283f / 7 + R(r, -.15f, .15f));
                Vector3 side = Vector3.Cross(Vector3.up, direction);
                float length = scale * R(r, .55f, .95f);
                Vector3 previous = p;
                for (int segment = 1; segment <= 10; segment++)
                {
                    float t = segment / 10f;
                    Vector3 center = p + direction * (length * t) + Vector3.up * (Mathf.Sin(t * 2.3f) * length * .7f + .04f);
                    g.Ribbon(previous, center, .006f * scale, green * .72f, t);
                    float width = Mathf.Sin(t * Mathf.PI) * length * .27f;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 tip = center + side * (width * s) + direction * (length * .10f) + Vector3.up * .015f;
                        g.Leaf(center, tip, direction * (.026f * scale * (1 - t * .65f)), green * Mathf.Lerp(.68f, 1.13f, t), t);
                    }
                    previous = center;
                }
            }
        }
        static void Shrub(Geometry g, Vector3 p, float angle, float scale, System.Random r, bool flowering)
        {
            Color green = Color.Lerp(new Color(.20f, .34f, .10f), new Color(.42f, .49f, .18f), R(r, 0, 1));
            for (int branch = 0; branch < 5; branch++)
            {
                Vector3 d = Direction(angle + branch * 2.399f), side = Vector3.Cross(Vector3.up, d);
                Vector3 end = p + d * scale * R(r, .18f, .46f) + Vector3.up * scale * R(r, .45f, .9f);
                g.Ribbon(p, end, .009f * scale, new Color(.30f, .24f, .12f), .5f);
                for (int n = 1; n <= 6; n++)
                {
                    float t = n / 7f; Vector3 center = Vector3.Lerp(p, end, t);
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Vector3 tip = center + (side * sign * .14f + d * .07f + Vector3.up * .055f) * scale;
                        g.Leaf(center, tip, (d + Vector3.up * .4f).normalized * scale * .065f, green * R(r, .77f, 1.18f), t);
                    }
                }
                if (flowering)
                    for (int flower = 0; flower < 3; flower++) Blossom(g, end + Direction(flower * 2.09f) * .05f * scale, .037f * scale, new Color(.93f, .82f, .70f));
            }
        }
        static void Flowers(Geometry g, Vector3 p, float angle, float scale, System.Random r)
        {
            Color petals = r.Next(3) == 0 ? new Color(.48f, .39f, .70f) : new Color(.91f, .84f, .55f);
            for (int stem = 0; stem < 6; stem++)
            {
                Vector3 d = Direction(angle + stem * 2.399f), start = p + d * R(r, .03f, .25f) * scale;
                Vector3 tip = start + (Vector3.up * R(r, .28f, .68f) + d * .1f) * scale;
                g.Ribbon(start, tip, .007f * scale, new Color(.27f, .41f, .11f), .8f);
                for (int n = 1; n < 4; n++)
                {
                    Vector3 center = Vector3.Lerp(start, tip, n / 4f);
                    g.Leaf(center, center + (Direction(angle + n * 2.4f) * .10f + Vector3.up * .045f) * scale, d * .025f * scale, new Color(.35f, .47f, .14f), .5f);
                }
                Blossom(g, tip, .048f * scale, petals);
            }
        }
        static void Blossom(Geometry g, Vector3 p, float radius, Color color)
        {
            for (int petal = 0; petal < 5; petal++)
            {
                Vector3 d = Direction(petal * 1.2566f), side = Vector3.Cross(Vector3.up, d);
                g.Leaf(p, p + d * radius + Vector3.up * radius * .25f, side * radius * .43f, color, 1);
            }
            g.Leaf(p + Vector3.up * .008f - Vector3.right * radius * .2f, p + Vector3.up * .008f + Vector3.right * radius * .2f, Vector3.forward * radius * .2f, new Color(.80f, .52f, .12f), 1);
        }
        static void FallenLog(Geometry g, Vector3 p, float angle, System.Random r)
        {
            Vector3 along = Direction(angle), across = Vector3.Cross(Vector3.up, along);
            float length = R(r, 2.1f, 3.6f), radius = R(r, .15f, .23f);
            for (int segment = 0; segment < 5; segment++)
                for (int side = 0; side < 10; side++)
                {
                    float a = side * .6283f, b = (side + 1) * .6283f;
                    Vector3 va = (across * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * radius;
                    Vector3 vb = (across * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b)) * radius;
                    Vector3 start = p + along * (segment / 5f * length), end = p + along * ((segment + 1) / 5f * length);
                    Color bark = Color.Lerp(new Color(.19f, .16f, .10f), new Color(.37f, .31f, .19f), R(r, 0, 1));
                    if (Mathf.Sin(a) > .4f) bark = Color.Lerp(new Color(.20f, .30f, .07f), new Color(.36f, .42f, .13f), R(r, 0, 1));
                    g.Quad(start + va, end + va, end + vb, start + vb, bark, 0);
                    if (segment == 4) g.Triangle(end, end + vb, end + va, new Color(.50f, .39f, .23f), 0);
                    if (segment == 0) g.Triangle(start, start + va, start + vb, new Color(.34f, .27f, .15f), 0);
                }
            Fern(g, p + across * .27f, angle + 1, .72f, r);
        }
        void OnDestroy() { foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh); }

        sealed class Geometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<int> indices = new List<int>();
            readonly List<Color> colors = new List<Color>();
            readonly List<Vector2> uv = new List<Vector2>();
            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color, float wind)
            {
                int i = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                indices.Add(i); indices.Add(i + 1); indices.Add(i + 2);
                colors.Add(color.linear); colors.Add(color.linear); colors.Add(color.linear);
                uv.Add(new Vector2(0, wind)); uv.Add(new Vector2(.5f, wind)); uv.Add(new Vector2(1, wind));
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, float wind)
            { Triangle(a, b, c, color, wind); Triangle(a, c, d, color, wind); }
            public void Leaf(Vector3 root, Vector3 tip, Vector3 width, Color color, float wind)
            {
                Vector3 mid = Vector3.Lerp(root, tip, .48f), ridge = mid + Vector3.up * width.magnitude * .23f;
                Triangle(root, mid - width, ridge, color * .86f, wind);
                Triangle(root, ridge, mid + width, color, wind);
                Triangle(mid - width, tip, ridge, color * .92f, wind);
                Triangle(ridge, tip, mid + width, color * 1.08f, wind);
            }
            public void Ribbon(Vector3 a, Vector3 b, float width, Color color, float wind)
            { Vector3 side = Vector3.Cross(b - a, Vector3.forward).normalized * width; Quad(a - side, b - side, b + side, a + side, color, wind); }
            public Mesh Mesh()
            {
                var mesh = new Mesh { name = "Original forest understory", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.SetColors(colors); mesh.SetUVs(0, uv);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
    }
}
