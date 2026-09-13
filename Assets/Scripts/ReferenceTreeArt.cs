using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    // Native scale: pine 20.4m, fir 19m. CC0 authored trees, see docs/reference-trees.json.
    public static class ReferenceTreeArt
    {
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static GameObject Tree(bool fir, Transform parent)
        {
            string name = fir ? "Fir" : "Pine";
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var levels = new List<LOD>();
            float[] transitions = { .32f, .13f, .012f };
            for (int i = 0; i < 3; i++)
            {
                GameObject source = Resources.Load<GameObject>("ReferenceTrees/" + name + "_LOD" + i);
                if (source == null) continue;
                GameObject instance = Object.Instantiate(source, root.transform, false);
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    Material[] slots = renderer.sharedMaterials;
                    for (int j = 0; j < slots.Length; j++)
                    {
                        string materialName = slots[j] == null ? name + "_bark" : slots[j].name.Replace(" (Instance)", "");
                        slots[j] = MaterialFor(materialName.Replace("_dead_branches", "_bark"));
                    }
                    renderer.sharedMaterials = slots;
                }
                levels.Add(new LOD(transitions[i], renderers));
            }
            if (levels.Count == 0) { Object.Destroy(root); return null; }
            LODGroup group = root.AddComponent<LODGroup>();
            group.SetLODs(levels.ToArray());
            group.RecalculateBounds();
            return root;
        }

        static Material MaterialFor(string name)
        {
            if (Materials.TryGetValue(name, out Material material)) return material;
            bool needles = name.EndsWith("_twig");
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            material.SetTexture("_BaseMap", Resources.Load<Texture2D>("ReferenceTrees/" + name + "_BaseColor"));
            material.SetTexture("_BumpMap", Resources.Load<Texture2D>("ReferenceTrees/" + name + "_Normal"));
            // Pine source needles are pale under direct Lit shading. Grade only foliage; bark stays neutral.
            material.SetColor("_BaseColor", name == "Pine_twig" ? new Color(.55f, .72f, .44f) : Color.white);
            material.SetFloat("_BumpScale", needles ? .55f : 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", needles ? .16f : .08f);
            if (needles)
            {
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_AlphaToMask", 1f);
                material.SetFloat("_Cutoff", .35f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }
            Materials[name] = material;
            return material;
        }
    }
}
