using System.Collections;
using System.Linq;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ReferenceGroveTests
{
    static IEnumerator LoadGrove()
    {
        yield return SceneManager.LoadSceneAsync("Pinewatch");
        float deadline = Time.realtimeSinceStartup + 20;
        while ((ReferenceGrove.Current == null || !ReferenceGrove.Current.Ready) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(ReferenceGrove.Current, Is.Not.Null, "Pinewatch must create the authored grove");
        Assert.That(ReferenceGrove.Current.Ready, Is.True, "Grove initialization must finish");
    }

    [UnityTest]
    public IEnumerator ImportedGroveKeepsCompleteLodsAndMaterialsAcrossQualityChanges()
    {
        yield return LoadGrove();
        var grove = ReferenceGrove.Current;
        var roots = grove.transform.Cast<Transform>().Where(t => t.name != "Fern Hollow doe").ToArray();
        foreach (string kind in new[] { "Pine", "Fir", "Fern", "FernA", "FernC", "FernD", "Stump", "RockA", "RockB", "RockC", "RockD", "RockE", "RockF" })
        {
            var root = roots.FirstOrDefault(t => t.name == kind);
            Assert.That(root, Is.Not.Null, kind + " must instantiate from its imported asset");
            bool tree = kind == "Pine" || kind == "Fir";
            var group = root.GetComponent<LODGroup>();
            Assert.That(group, Is.Not.Null, kind);
            var lods = group.GetLODs();
            Assert.That(lods, Has.Length.EqualTo(3), kind + " needs all exported LODs");
            float previous = 1;
            for (int level = 0; level < lods.Length; level++)
            {
                Assert.That(lods[level].screenRelativeTransitionHeight, Is.InRange(.0001f, previous - .0001f));
                previous = lods[level].screenRelativeTransitionHeight;
                Assert.That(lods[level].renderers, Is.Not.Empty, kind + " LOD " + level);
                var source = Resources.Load<GameObject>((tree ? "ReferenceTrees/" : "ReferenceGround/") + kind + "_LOD" + level);
                Assert.That(source, Is.Not.Null);
                var sourceMeshes = source.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).ToArray();
                foreach (var renderer in lods[level].renderers)
                {
                    Assert.That(renderer, Is.Not.Null);
                    Assert.That(sourceMeshes, Does.Contain(renderer.GetComponent<MeshFilter>().sharedMesh), "LOD must use the matching native imported mesh");
                    foreach (var material in renderer.sharedMaterials)
                    {
                        Assert.That(material.shader.name, Is.EqualTo(tree || kind.StartsWith("Fern") ? "Explorers/VegetationLit" : "Universal Render Pipeline/Lit"));
                        if (tree || kind.StartsWith("Fern"))
                        {
                            Assert.That(material.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0));
                            Assert.That(material.FindPass("DepthNormals"), Is.GreaterThanOrEqualTo(0));
                            var originalBounds = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
                            Assert.That(renderer.localBounds.size.x, Is.GreaterThan(originalBounds.size.x), "Wind needs expanded culling bounds");
                        }
                        Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null, material.name + " diffuse map");
                        Assert.That(material.GetTexture("_BumpMap"), Is.Not.Null, material.name + " normal map");
                        Assert.That(material.IsKeywordEnabled("_NORMALMAP"), Is.True);
                        if (!tree)
                        {
                            Assert.That(material.GetTexture("_MetallicGlossMap"), Is.Not.Null, material.name + " packed roughness conversion");
                            Assert.That(material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"), Is.True);
                        }
                        if (kind.StartsWith("Fern") || material.name.EndsWith("_twig"))
                        {
                            Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.True, material.name);
                            Assert.That(material.GetFloat("_Cull"), Is.Zero, "Leaf backs must remain visible");
                            Assert.That(material.GetFloat("_Cutoff"), Is.InRange(.2f, .5f));
                            Assert.That(material.renderQueue, Is.EqualTo(2450));
                        }
                    }
                }
            }
            // Catch FBX centimeter/meter and up-axis regressions in imported model space.
            float height = ValleyWorld.ModelHeight(root.gameObject);
            Assert.That(height, tree ? Is.InRange(12f, 30f) : Is.InRange(.1f, 4f), kind + " imported height");
        }
        Assert.That(grove.TransitionTreeCount, Is.GreaterThan(0), "The grove edge must replace existing prototype trees");
        var forest=Object.FindFirstObjectByType<ValleyWorld>().transform.Find("Pine forest");
        foreach(Transform trunk in forest)
            if(trunk.name=="Transition trunk collision")
            {
                Assert.That(trunk.GetComponent<Collider>().enabled, Is.True, "Replacing art must retain the existing trunk collision");
                Assert.That(trunk.GetComponentsInChildren<Renderer>(), Is.Empty, "The old canopy must not overlap the replacement");
            }
        var session = Object.FindFirstObjectByType<WalkSession>();
        foreach (bool high in new[] { false, true })
        {
            session.ApplyQuality(high);
            yield return null;
            foreach (var root in roots)
            {
                Assert.That(root != null && root.gameObject.activeInHierarchy, Is.True, "Quality must preserve authored trees and props");
                Assert.That(root.GetComponent<LODGroup>().enabled, Is.True);
            }
        }
    }

    [UnityTest]
    public IEnumerator PaintedTerrainStaysNormalizedAndVisitReturnsAnUnobstructedGroundedWalker()
    {
        yield return LoadGrove();
        var grove = ReferenceGrove.Current;
        var world = Object.FindFirstObjectByType<ValleyWorld>();
        var terrain = world.Ground;
        var data = terrain.terrainData;
        var layers = data.terrainLayers;
        Assert.That(layers[layers.Length - 2].diffuseTexture, Is.SameAs(Resources.Load<Texture2D>("ReferenceGround/ForestFloor_BaseColor")));
        Assert.That(layers[layers.Length - 1].normalMapTexture, Is.SameAs(Resources.Load<Texture2D>("ReferenceGround/TrailGround_Normal")));
        foreach (var layer in layers.Skip(layers.Length - 2))
        {
            Assert.That(layer.diffuseTexture, Is.Not.Null);
            Assert.That(layer.normalMapTexture, Is.Not.Null);
        }
        // Sample the tread, shoulder, blend border, and untouched world, including heightmap vertices.
        foreach (var point in new[] { ValleyShape.Trail(-150), ValleyShape.Trail(-150) + Vector3.right * 8, ValleyShape.Trail(-150) + Vector3.right * 39, ValleyShape.Spawn })
        {
            Vector3 local = point - terrain.transform.position;
            int x = Mathf.Clamp(Mathf.RoundToInt(local.x / data.size.x * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(local.z / data.size.z * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);
            var map = data.GetAlphamaps(x, z, 1, 1);
            float forestCoverage = map[0, 0, layers.Length - 2] + map[0, 0, layers.Length - 1];
            if (ReferenceGrove.Contains(point) && Mathf.Abs(point.x - ValleyShape.TrailX(point.z)) < 10)
                Assert.That(forestCoverage, Is.GreaterThan(.9f), "Interior must receive scanned ground layers");
            if (!ReferenceGrove.Contains(point, 10))
                Assert.That(forestCoverage, Is.LessThan(.01f), "Dressing must stay local to the grove");
            float sum = 0;
            for (int layer = 0; layer < layers.Length; layer++) { Assert.That(map[0, 0, layer], Is.InRange(0f, 1f)); sum += map[0, 0, layer]; }
            Assert.That(sum, Is.EqualTo(1f).Within(.01f), "Painting must not create dark holes or overbright seams");
            int hx = Mathf.RoundToInt(local.x / data.size.x * (data.heightmapResolution - 1));
            int hz = Mathf.RoundToInt(local.z / data.size.z * (data.heightmapResolution - 1));
            float wx = terrain.transform.position.x + hx * data.size.x / (data.heightmapResolution - 1);
            float wz = terrain.transform.position.z + hz * data.size.z / (data.heightmapResolution - 1);
            Assert.That(data.GetHeight(hx, hz) + terrain.transform.position.y, Is.EqualTo(ValleyShape.Height(wx, wz)).Within(.02f), "Grove dressing must preserve authoritative terrain heights");
        }
        var walker = world.walker;
        var controller = walker.GetComponent<CharacterController>();
        walker.Automated = true;
        try
        {
            walker.SetMenu(true);
            walker.Teleport(ValleyShape.Spawn);
            walker.view.transform.localRotation = Quaternion.Euler(35, 0, 0);
            grove.Visit(); // The same public action invoked by F8.
            Assert.That(walker.MenuOpen, Is.False);
            Assert.That(controller.enabled, Is.True);
            Assert.That(Vector3.Distance(walker.transform.position, ReferenceGrove.Entrance), Is.LessThan(.01f));
            Assert.That(Vector3.Angle(walker.view.transform.forward, Vector3.forward), Is.LessThan(.1f));
            Physics.SyncTransforms();
            for (int step = 0; step < 30; step++) { controller.Move(Vector3.down * .04f); yield return null; }
            Assert.That(controller.isGrounded, Is.True, "Visit must land above the terrain collider");
            var cameraObstacles = Physics.OverlapSphere(walker.view.transform.position, .15f, ~0, QueryTriggerInteraction.Ignore)
                .Where(c => c != controller && !c.transform.IsChildOf(walker.transform)).ToArray();
            Assert.That(cameraObstacles, Is.Empty, "Camera must not spawn inside a trunk or terrain");
            Vector3 start = walker.transform.position;
            for (int step = 0; step < 90; step++)
            {
                float z = walker.transform.position.z + .05f;
                float x = ValleyShape.TrailX(z);
                controller.Move(new Vector3(x - walker.transform.position.x, -.08f, .05f));
                yield return null;
            }
            Assert.That(walker.transform.position.z - start.z, Is.GreaterThan(4f), "The entrance trail must allow forward walking");
            Assert.That(controller.isGrounded, Is.True);
        }
        finally { walker.Automated = false; walker.SetMenu(true); }
    }
}
