using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExplorersByNature
{
    // Shared by ranch and scenery. Imported material slots and LODs stay intact.
    public static class ModelArt
    {
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static GameObject Instantiate(string path, Transform parent, bool cull = true)
        {
            GameObject asset = Resources.Load<GameObject>(path);
            if (asset == null) return null;
            GameObject instance = UnityEngine.Object.Instantiate(asset, parent, false);
            instance.name = asset.name;
            Remap(instance, Folder(path),path+"Detail");
            ConfigureLods(instance, cull);
            AnimalMotion.Attach(instance, path);
            return instance;
        }

        public static GameObject Tree(string path, Transform parent, bool cull = true)
        {
            if (Resources.Load<GameObject>(path + "_LOD0") == null) return Instantiate(path, parent);
            var root = new GameObject(path.Substring(path.LastIndexOf('/') + 1));
            root.transform.SetParent(parent, false);
            var levels = new List<LOD>();
            float[] transitions = { .2f, .065f, cull ? .006f : .00001f };
            for (int i = 0; i < 3; i++)
            {
                var source = Resources.Load<GameObject>(path + "_LOD" + i);
                if (source == null) continue;
                var model = UnityEngine.Object.Instantiate(source, root.transform, false);
                Remap(model, Folder(path));
                levels.Add(new LOD(transitions[i], model.GetComponentsInChildren<Renderer>()));
            }
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(levels.ToArray());
            group.RecalculateBounds();
            return root;
        }

        static void ConfigureLods(GameObject root, bool cull)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var groups = new List<LOD>();
            for (int level = 0; level < 3; level++)
            {
                string suffix = "_LOD" + level;
                Renderer[] selected = renderers.Where(r => HasAncestor(r.transform, root.transform, suffix)).ToArray();
                if (selected.Length > 0) groups.Add(new LOD(level == 0 ? .18f : .025f, selected));
            }
            if (groups.Count < 2) return;
            LOD last = groups[groups.Count - 1];
            last.screenRelativeTransitionHeight = cull ? .004f : .00001f;
            groups[groups.Count - 1] = last;
            var lod = root.GetComponent<LODGroup>() ?? root.AddComponent<LODGroup>();
            lod.SetLODs(groups.ToArray());
            lod.RecalculateBounds();
        }

        static bool HasAncestor(Transform node, Transform stop, string token)
        {
            while (node != null)
            {
                if (node.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (node == stop) break;
                node = node.parent;
            }
            return false;
        }

        static string Folder(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash < 0 ? "" : path.Substring(0, slash + 1);
        }

        public static void Remap(GameObject root, string folder, string detail = null)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = Convert(slots[i], folder,detail);
                renderer.sharedMaterials = slots;
            }
        }

        static Material Convert(Material source, string folder,string detail)
        {
            string name = source == null ? "Unpainted" : source.name.Replace(" (Instance)", "");
            string key = (detail ?? folder) + ":" + name;
            if (Materials.TryGetValue(key, out Material result) && result != null) return result;
            Color color = source != null && source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source != null && source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            Texture texture = source != null ? source.mainTexture : null;
            texture = texture ?? Resources.Load<Texture2D>(folder + name + "_BaseColor") ?? Resources.Load<Texture2D>(folder + name) ?? Resources.Load<Texture2D>("Animals/" + name + "_BaseColor");
            Texture2D detailColor=detail==null?null:Resources.Load<Texture2D>(detail+"_BaseColor");
            Texture2D detailNormal=detail==null?null:Resources.Load<Texture2D>(detail+"_Normal");
            if(detailColor!=null)texture=detailColor;
            result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            result.SetColor("_BaseColor", color);
            result.SetFloat("_Smoothness", name.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0 ? .65f : detailColor!=null ? .09f : .18f);
            if (texture != null) { result.SetTexture("_BaseMap", texture); result.SetColor("_BaseColor", Color.white); }
            if(detailNormal!=null){result.SetTexture("_BumpMap",detailNormal);result.SetFloat("_BumpScale",.55f);result.EnableKeyword("_NORMALMAP");}
            bool foliage = name.IndexOf("Leaves", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Needles", StringComparison.OrdinalIgnoreCase) >= 0;
            if (name.StartsWith("Wildflower", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Hen", StringComparison.OrdinalIgnoreCase)) result.SetFloat("_Cull", 0);
            if (foliage)
            {
                result.shader = Shader.Find("Explorers/Foliage");
                result.SetFloat("_Cull", 0);
                result.SetFloat("_AlphaClip", 1);
                result.SetFloat("_Cutoff", .35f);
                result.EnableKeyword("_ALPHATEST_ON");
                result.SetOverrideTag("RenderType", "TransparentCutout");
                result.renderQueue = (int)RenderQueue.AlphaTest;
            }
            Materials[key] = result;
            return result;
        }
    }
}
