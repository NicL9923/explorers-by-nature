using System;
using System.Collections.Generic;
using System.Linq;
using ExplorersByNature;
using UnityEditor;
using UnityEngine;

public sealed class AnimalFurImport : AssetPostprocessor
{
    public override uint GetVersion() => 2;
    public override int GetPostprocessOrder() => 200;
    void OnPostprocessModel(GameObject model)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (!assetPath.StartsWith("Assets/Resources/") || (name != "Deer" && name != "Clover" && name != "Fox" && name != "Rabbit")) return;
        var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(s => s.sharedMesh.vertexCount).FirstOrDefault();
        if (skin == null) return;
        Mesh mesh = skin.sharedMesh;
        Vector3[] positions = mesh.vertices, normals = mesh.normals;
        Vector2[] uv = mesh.uv;
        BoneWeight[] weights = mesh.boneWeights;
        if (weights.Length != positions.Length || uv.Length != positions.Length) return;
        var triangles = new List<(int a, int b, int c, int material, float area)>();
        float total = 0;
        for (int m = 0; m < mesh.subMeshCount; m++)
        {
            string material = skin.sharedMaterials[m].name;
            bool tailMaterial=name=="Clover" && material.Contains("Sable");
            if (!material.Contains("Coat") && !tailMaterial) continue;
            int[] indices = mesh.GetTriangles(m);
            for (int t = 0; t < indices.Length; t += 3)
            {
                int a = indices[t], b = indices[t + 1], c = indices[t + 2];
                if (!Covered(weights[a], skin.bones) || !Covered(weights[b], skin.bones) || !Covered(weights[c], skin.bones)) continue;
                float area = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]).magnitude * .5f;
                if (area <= 1e-10f) continue;
                if(tailMaterial)
                {
                    if(TailWeight(weights[a],skin.bones)<.8f || TailWeight(weights[b],skin.bones)<.8f || TailWeight(weights[c],skin.bones)<.8f)continue;
                    area*=10; // Reserve enough coverage for the small, long-haired tail switch.
                }
                total += area; triangles.Add((a,b,c,m,total));
            }
        }
        if (triangles.Count == 0) return;
        var random = new System.Random(7341);
        float Next() => (float)random.NextDouble();
        int count = name == "Deer" || name == "Clover" ? 1800 : 1200;
        float length = name == "Fox" ? .040f : name == "Rabbit" ? .023f : .026f;
        var roots = new AnimalFurGroom.Root[count];
        for (int i = 0; i < count; i++)
        {
            float pick = Next() * total;
            int lo = 0, hi = triangles.Count - 1;
            while (lo < hi) { int mid = (lo + hi) / 2; if (triangles[mid].area < pick) lo = mid + 1; else hi = mid; }
            var tri = triangles[lo];
            float u = Mathf.Sqrt(Next()), v = Next(); Vector3 bary = new Vector3(1-u,u*(1-v),u*v);
            var combined = new Dictionary<int,float>();
            Add(combined,weights[tri.a],bary.x); Add(combined,weights[tri.b],bary.y); Add(combined,weights[tri.c],bary.z);
            var best = combined.OrderByDescending(pair => pair.Value).Take(4).ToArray();
            float sum = best.Sum(pair=>pair.Value);
            var w = new BoneWeight();
            for(int j=0;j<best.Length;j++)
            {
                int bone=best[j].Key;float value=best[j].Value/sum;
                if(j==0){w.boneIndex0=bone;w.weight0=value;} else if(j==1){w.boneIndex1=bone;w.weight1=value;}
                else if(j==2){w.boneIndex2=bone;w.weight2=value;} else {w.boneIndex3=bone;w.weight3=value;}
            }
            roots[i] = new AnimalFurGroom.Root { position=positions[tri.a]*bary.x+positions[tri.b]*bary.y+positions[tri.c]*bary.z,
                normal=(normals[tri.a]*bary.x+normals[tri.b]*bary.y+normals[tri.c]*bary.z).normalized,
                uv=uv[tri.a]*bary.x+uv[tri.b]*bary.y+uv[tri.c]*bary.z,weights=w,material=tri.material,
                length=(name=="Clover" && skin.sharedMaterials[tri.material].name.Contains("Sable")?.10f:length)*Mathf.Lerp(.65f,1.3f,Next()),roll=Next()*Mathf.PI*2 };
        }
        var groom=model.AddComponent<AnimalFurGroom>();groom.skin=skin;groom.bindposes=mesh.bindposes;groom.roots=roots;
    }
    static float TailWeight(BoneWeight w,Transform[] bones)
    {
        float Part(int bone,float weight)=>bones[bone].name=="Tail"?weight:0;
        return Part(w.boneIndex0,w.weight0)+Part(w.boneIndex1,w.weight1)+Part(w.boneIndex2,w.weight2)+Part(w.boneIndex3,w.weight3);
    }
    static bool Covered(BoneWeight weight,Transform[] bones)
    {
        float excluded=0;
        void Check(int index,float value)
        {
            string name=bones[index].name;
            if(name=="Head" || name.StartsWith("Ear") || name.EndsWith("Lower") || name.EndsWith("Foot"))excluded+=value;
        }
        Check(weight.boneIndex0,weight.weight0);Check(weight.boneIndex1,weight.weight1);Check(weight.boneIndex2,weight.weight2);Check(weight.boneIndex3,weight.weight3);
        return excluded < .15f;
    }
    static void Add(Dictionary<int,float> result,BoneWeight weight,float fraction)
    {
        void Part(int index,float value){if(value<=0)return;result.TryGetValue(index,out float old);result[index]=old+value*fraction;}
        Part(weight.boneIndex0,weight.weight0);Part(weight.boneIndex1,weight.weight1);Part(weight.boneIndex2,weight.weight2);Part(weight.boneIndex3,weight.weight3);
    }
}
