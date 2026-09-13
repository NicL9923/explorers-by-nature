using System;
using UnityEngine;

namespace ExplorersByNature
{
    // Authored deterministically during FBX import. Rest positions and skin weights
    // pin each clump to the same animated surface as the coat beneath it.
    public sealed class AnimalFurGroom : MonoBehaviour
    {
        [Serializable] public struct Root
        {
            public Vector3 position, normal;
            public Vector2 uv;
            public BoneWeight weights;
            public int material;
            public float length, roll;
        }
        public SkinnedMeshRenderer skin;
        public Matrix4x4[] bindposes;
        public Root[] roots;
    }
}
