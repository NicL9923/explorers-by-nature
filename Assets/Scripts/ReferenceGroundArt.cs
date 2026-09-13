using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    // CC0 scan props, in meters, with their lowest point at the local origin.
    public static class ReferenceGroundArt
    {
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static GameObject Instantiate(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var levels = new List<LOD>();
            float[] transitions = { .16f, .045f, .008f };
            string material = name.StartsWith("Fern") ? "Fern" : name.StartsWith("Rock") ? "Rock" : "Stump";
            for (int i = 0; i < 3; i++)
            {
                var source = Resources.Load<GameObject>("ReferenceGround/" + name + "_LOD" + i);
                if (source == null) continue;
                var model = Object.Instantiate(source, root.transform, false);
                var renderers = model.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    var slots = renderer.sharedMaterials;
                    for (int s = 0; s < slots.Length; s++) slots[s] = Surface(material);
                    renderer.sharedMaterials = slots;
                    if (material == "Fern")
                    {
                        Bounds windBounds = renderer.localBounds;
                        windBounds.Expand(.6f);
                        renderer.localBounds = windBounds;
                    }
                    renderer.shadowCastingMode = material == "Fern" ? ShadowCastingMode.TwoSided : ShadowCastingMode.On;
                }
                levels.Add(new LOD(transitions[i], renderers));
            }
            if (levels.Count == 0) { Object.Destroy(root); return null; }
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(levels.ToArray());
            group.RecalculateBounds();
            return root;
        }

        public static Material Surface(string name)
        {
            if (Materials.TryGetValue(name, out var material) && material != null) return material;
            material = new Material(Shader.Find(name == "Fern" ? "Explorers/VegetationLit" : "Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", Resources.Load<Texture2D>("ReferenceGround/" + name + "_BaseColor"));
            material.SetTexture("_BumpMap", Resources.Load<Texture2D>("ReferenceGround/" + name + "_Normal"));
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", Resources.Load<Texture2D>("ReferenceGround/" + name + "_MetallicSmoothness"));
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            if (name == "Fern")
            {
                material.EnableKeyword("_NATURE_FERN");
                material.SetFloat("_Cull", 0);
                material.SetFloat("_AlphaClip", 1);
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
